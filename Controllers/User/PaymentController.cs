using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using thuvienso.Data;
using thuvienso.Models;

[Route("payment")]
public class PaymentController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public PaymentController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    /// <summary>
    /// Tạo hoặc cập nhật yêu cầu thanh toán cho tài liệu.
    /// Quy tắc:
    /// - Mỗi người với mỗi tài liệu chỉ có tối đa 1 bản ghi `paid` + 1 bản ghi `pending`.
    /// - Nếu đã `paid`, được phép tạo `pending` mới (cập nhật nếu đã có).
    /// - Nếu chưa `paid` mà có `pending` → cập nhật dòng đó.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromForm] int documentId, [FromForm] int percent)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Unauthorized();

        // 👉 Lấy tài liệu
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsFree || document.Price == null)
            return BadRequest("Tài liệu không hợp lệ");

        var price = document.Price.Value;
        var percentValue = Math.Clamp(percent, 1, 100);

        // 👉 Lấy bản ghi thanh toán đã thanh toán
        var paymentPaid = await _context.Payments
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentId == documentId && p.PaymentStatus == "paid");

        // 👉 Lấy bản ghi thanh toán đang chờ (QR chưa quét)
        var paymentPending = await _context.Payments
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentId == documentId && p.PaymentStatus == "pending");

        var percentOld = paymentPaid?.PercentPaid ?? 0;
        if (percentValue <= percentOld)
            return BadRequest("Bạn đã thanh toán phần trăm này hoặc cao hơn.");

        // Tính phần cần thanh toán thêm
        var addedPercent = percentValue - percentOld;
        var amount = (int)Math.Round(price * addedPercent / 100);// PayOS dùng đơn vị VND


        // 👉 Tạo orderCode mới
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var baseUrl = _config["PublicUrl"];
        var returnUrl = $"{baseUrl}/user/profile";
        var cancelUrl = $"{baseUrl}/document/{document.Id}";
        var description = $"TL#{document.Id} - +{addedPercent}%";

        var rawSignature = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        var signature = ComputeHmacSha256(rawSignature, _config["PayOS:ChecksumKey"]);

        // 👉 Payload gửi PayOS
        var payload = new
        {
            orderCode = orderCode,
            amount = amount,
            description = description,
            cancelUrl = cancelUrl,
            returnUrl = returnUrl,
            webhookUrl = _config["PayOS:WebhookUrl"], // 👈 BẮT BUỘC PHẢI CÓ
            signature = signature
        };

        // 👉 Log để xác minh URL webhook đang dùng
        Console.WriteLine("🌐 Webhook URL đang dùng:");
        Console.WriteLine(payload.webhookUrl);

        //Console.WriteLine("📦 Payload gửi lên:");
        //Console.WriteLine(JsonConvert.SerializeObject(payload));
        //Console.WriteLine("🔐 Raw Signature:");
        //Console.WriteLine(rawSignature);
        //Console.WriteLine("✅ Signature:");
        //Console.WriteLine(signature);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
        client.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);
        client.DefaultRequestHeaders.Add("x-checksum", signature);

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content);
        var resBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine("📥 Response từ PayOS:");
        Console.WriteLine(resBody);

        if (!response.IsSuccessStatusCode)
            return BadRequest("PayOS lỗi: " + resBody);

        var json = JObject.Parse(resBody);
        var data = json["data"];
        if (data == null)
            return BadRequest("Không lấy được dữ liệu từ PayOS");

        var checkoutUrl = data["checkoutUrl"]?.ToString();
        var qrCode = data["qrCode"]?.ToString();

        // 👉 Tạo ảnh QR VietQR (từ chuỗi QR trả về)
        var qrImage = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(qrCode)}";

        // 👉 Nếu đã có payment `pending` → cập nhật
        if (paymentPending != null)
        {
            paymentPending.PercentPaid = percentValue;
            paymentPending.PricePaid = amount;
            paymentPending.OrderCode = orderCode.ToString();
            paymentPending.QrCodeUrl = qrImage;
            paymentPending.CheckoutUrl = checkoutUrl;
            paymentPending.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // 👉 Chưa có → thêm mới dòng `pending`
            var newPayment = new Payment
            {
                UserId = userId.Value,
                DocumentId = document.Id,
                PercentPaid = percentValue,
                TotalPrice = price,
                PricePaid = amount,
                OrderCode = orderCode.ToString(),
                PaymentStatus = "pending",
                QrCodeUrl = qrImage,
                CheckoutUrl = checkoutUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(newPayment);
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            qrCodeUrl = qrImage,
            checkoutUrl = checkoutUrl
        });
    }

    [HttpGet("confirm-webhook-test")]
    public async Task<IActionResult> ConfirmWebhookTest()
    {
        return await ConfirmWebhook(); // Gọi lại hàm đã có sẵn
    }

    [HttpPost("confirm-webhook")]
    public async Task<IActionResult> ConfirmWebhook()
    {
        var webhookUrl = _config["PayOS:WebhookUrl"];

        var payload = new
        {
            webhookUrl = webhookUrl
        };

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
        client.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api-merchant.payos.vn/confirm-webhook", content);
        var resBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine("📡 Kết quả xác minh webhook:");
        Console.WriteLine(resBody);

        return Content(resBody, "application/json");
    }


    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        Request.EnableBuffering(); // Cho phép đọc nhiều lần body

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0; // Reset lại để middleware khác dùng

        Console.WriteLine("📥 Webhook nhận được:");
        Console.WriteLine(body);

        try
        {
            var payload = JObject.Parse(body);
            var eventData = payload["data"] as JObject;
            var receivedSignature = payload["signature"]?.ToString();

            // ✅ Trường hợp test webhook
            if (eventData == null || eventData["orderCode"]?.ToString() == "123")
            {
                Console.WriteLine("✅ Xác minh webhook test từ PayOS");
                return Ok(new { code = "00", message = "Confirm webhook successfully" });
            }

            // ✅ Tạo chuỗi signature theo định dạng key=value&key2=value2...
            var sortedKeys = eventData.Properties()
                .Select(p => p.Name)
                .OrderBy(name => name)
                .ToList();

            var signatureBase = new StringBuilder();
            foreach (var key in sortedKeys)
            {
                var value = eventData[key]?.ToString() ?? "";
                signatureBase.Append($"{key}={value}");
                if (key != sortedKeys.Last())
                    signatureBase.Append("&");
            }

            var expectedSignature = ComputeHmacSha256(signatureBase.ToString(), _config["PayOS:ChecksumKey"]);

            Console.WriteLine("🔐 Chuỗi để ký: " + signatureBase.ToString());
            Console.WriteLine("🔐 Signature nhận được: " + receivedSignature);
            Console.WriteLine("🔐 Signature tính lại : " + expectedSignature);

            if (receivedSignature != expectedSignature)
            {
                Console.WriteLine("❌ Signature không hợp lệ.");
                return Unauthorized();
            }

            var orderCode = eventData["orderCode"]?.ToString();
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderCode == orderCode);

            if (payment == null)
            {
                Console.WriteLine($"⚠️ Không tìm thấy đơn: {orderCode}");
                return NotFound();
            }

            if (payment.PaymentStatus == "paid")
            {
                Console.WriteLine($"✅ Đơn hàng {orderCode} đã xử lý.");
                return Ok();
            }

            payment.PaymentStatus = "paid";
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Cập nhật thành công đơn hàng {orderCode}.");
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Lỗi xử lý webhook:");
            Console.WriteLine(ex.Message);

            return Ok(new { code = "00", message = "Fallback for malformed webhook" });
        }
    }





    private static string ComputeHmacSha256(string rawData, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var rawBytes = Encoding.UTF8.GetBytes(rawData);

        using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(rawBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
