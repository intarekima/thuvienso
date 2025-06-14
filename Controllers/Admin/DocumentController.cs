using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdfiumViewer;
using PdfSharpCore.Pdf.IO;
using QRCoder;
using System.Diagnostics;
using System.Drawing.Imaging;
using thuvienso.Data;
using thuvienso.Models;
// Đặt alias cho PdfSharpCore
using SharpPdfDocument = PdfSharpCore.Pdf.PdfDocument;
using PdfReader = PdfSharpCore.Pdf.IO.PdfReader;

namespace thuvienso.Controllers.Admin
{
    [Route("admin/document")]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;
        public DocumentController(AppDbContext context)
        {
            _context = context;
        }



        // Hàm hỗ trợ tạo thumbnail từ trang đầu tiên của file
        public async Task RenderPdfThumbnail(string inputPdf, string outputJpg)
        {
            using var document = PdfDocument.Load(inputPdf);
            using var image = document.Render(0, 300, 400, true); // Trang đầu tiên
            image.Save(outputJpg, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        //Hàm hỗ trợ tạo preview pdf 3 trang đầu
        private bool GeneratePreviewPdf(string sourcePath, string previewPath, int maxPages = 3)
        {
            try
            {
                using var input = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                using var output = new SharpPdfDocument();

                int count = Math.Min(maxPages, input.PageCount);
                for (int i = 0; i < count; i++)
                {
                    output.AddPage(input.Pages[i]);
                }

                output.Save(previewPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Hàm hỗ trợ tạo mã QR và lưu thành file PNG
        private async Task GenerateAndSaveQr(string url, string savePath)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);
            await System.IO.File.WriteAllBytesAsync(savePath, qrBytes);
        }



        // Hiển thị danh sách tài liệu, có hỗ trợ tìm kiếm theo tiêu đề
        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? search,
            bool? isFree,
            int? categoryId,
            int? publisherId,
            [FromQuery(Name = "authorIds")] List<int>? authorIds,
            int page = 1)
        {
            int pageSize = 10;
            var query = _context.Documents
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.DocumentAuthors).ThenInclude(da => da.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.Title.Contains(search));
            }

            if (isFree.HasValue)
            {
                query = query.Where(d => d.IsFree == isFree.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(d => d.CategoryId == categoryId.Value);
            }

            if (publisherId.HasValue && publisherId.Value > 0)
            {
                query = query.Where(d => d.PublisherId == publisherId.Value);
            }

            if (authorIds != null && authorIds.Count > 0)
            {
                query = query.Where(d => d.DocumentAuthors.Any(da => authorIds.Contains(da.AuthorId)));
            }

            var totalItems = await query.CountAsync();
            var documents = await query
                .OrderByDescending(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu lên ViewBag để giữ lại form
            ViewBag.Search = search;
            ViewBag.IsFree = isFree;
            ViewBag.CategoryId = categoryId;
            ViewBag.PublisherId = publisherId;
            ViewBag.AuthorIds = authorIds ?? new List<int>();
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name");
            ViewBag.Authors = new MultiSelectList(await _context.Authors.ToListAsync(), "Id", "Name");

            return View("~/Views/Admin/Document/Index.cshtml", documents);
        }

        // Hiển thị form tạo tài liệu mới
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name");
            ViewBag.Authors = new MultiSelectList(await _context.Authors.ToListAsync(), "Id", "Name");
            return View("~/Views/Admin/Document/Create.cshtml");
        }

        // Xử lý POST tạo tài liệu mới
        [HttpPost("create")]
        public async Task<IActionResult> Create(string title, string description, int categoryId, int publisherId, List<int> authorIds, IFormFile file, IFormFile? thumb, bool isFree = true, decimal price = 0)
        {
            if (string.IsNullOrWhiteSpace(title) || categoryId == 0 || publisherId == 0 || file == null || authorIds == null || !authorIds.Any())
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("Create");
            }

            var document = new Document
            {
                Title = title,
                Description = description,
                CategoryId = categoryId,
                PublisherId = publisherId,
                IsFree = isFree,
                Price = price,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Tạo thư mục riêng
            var documentFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/documents", $"document-{document.Id}");
            if (!Directory.Exists(documentFolder))
            {
                Directory.CreateDirectory(documentFolder);
            }

            // Lưu file tài liệu
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(documentFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            document.FileUrl = $"/documents/document-{document.Id}/{fileName}";

            // Tạo file preview PDF (3 trang đầu)
            var previewName = "preview_" + fileName;
            var previewPath = Path.Combine(documentFolder, previewName);
            if (GeneratePreviewPdf(filePath, previewPath))
            {
                document.PreviewFileUrl = $"/documents/document-{document.Id}/{previewName}";
            }

            // Thumbnail
            string thumbFileName;
            if (thumb != null)
            {
                thumbFileName = "thumb_" + Guid.NewGuid() + Path.GetExtension(thumb.FileName);
                var thumbPath = Path.Combine(documentFolder, thumbFileName);
                using (var stream = new FileStream(thumbPath, FileMode.Create))
                {
                    await thumb.CopyToAsync(stream);
                }
                document.Thumb = $"/documents/document-{document.Id}/{thumbFileName}";
            }
            else
            {
                // Tự render thumbnail từ file PDF
                thumbFileName = "thumb.jpg";
                var thumbPath = Path.Combine(documentFolder, thumbFileName);
                await RenderPdfThumbnail(filePath, thumbPath);
                document.Thumb = $"/documents/document-{document.Id}/{thumbFileName}";
            }

            // QR code: loại view
            string viewUrl = $"{Request.Scheme}://{Request.Host}/document/detail/{document.Id}";
            string viewPath = Path.Combine(documentFolder, QRCodeFileName.View);
            string viewQrUrl = $"/documents/document-{document.Id}/{QRCodeFileName.View}";
            await GenerateAndSaveQr(viewUrl, viewPath);
            _context.QRCodes.Add(new QRCode
            {
                DocumentId = document.Id,
                Type = QRCodeType.view,
                QrUrl = viewQrUrl
            });

            // QR code: loại download
            string downloadUrl = $"{Request.Scheme}://{Request.Host}/document/download/{document.Id}";
            string downloadPath = Path.Combine(documentFolder, QRCodeFileName.Download);
            string downloadQrUrl = $"/documents/document-{document.Id}/{QRCodeFileName.Download}";
            await GenerateAndSaveQr(downloadUrl, downloadPath);
            _context.QRCodes.Add(new QRCode
            {
                DocumentId = document.Id,
                Type = QRCodeType.download,
                QrUrl = downloadQrUrl
            });

            // Gán tác giả
            foreach (var authorId in authorIds)
            {
                _context.DocumentAuthors.Add(new DocumentAuthor
                {
                    DocumentId = document.Id,
                    AuthorId = authorId
                });
            }

            _context.Documents.Update(document);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo tài liệu thành công.";
            return RedirectToAction("Index");
        }

        // Hiển thị form sửa tài liệu
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentAuthors)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return NotFound();

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", doc.CategoryId);
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name", doc.PublisherId);
            ViewBag.Authors = new MultiSelectList(await _context.Authors.ToListAsync(), "Id", "Name",
                doc.DocumentAuthors.Select(da => da.AuthorId));

            return View("~/Views/Admin/Document/Edit.cshtml", doc);
        }

        // Xử lý POST sửa tài liệu
        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(
        int id,
        string title,
        string description,
        int categoryId,
        int publisherId,
        List<int> authorIds,
        IFormFile? file,
        bool isFree = true,
        decimal price = 0)
        {
            var document = await _context.Documents
                .Include(d => d.DocumentAuthors)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return NotFound();

            document.Title = title;
            document.Description = description;
            document.CategoryId = categoryId;
            document.PublisherId = publisherId;
            document.IsFree = isFree;
            document.Price = price;
            document.UpdatedAt = DateTime.Now;

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/documents", $"document-{document.Id}");
            Directory.CreateDirectory(folder);

            if (file != null)
            {
                // Nếu có file mới → lưu lại
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                document.FileUrl = $"/documents/document-{document.Id}/{fileName}";

                // Tạo lại preview PDF
                var previewName = "preview_" + fileName;
                var previewPath = Path.Combine(folder, previewName);
                if (GeneratePreviewPdf(filePath, previewPath))
                {
                    document.PreviewFileUrl = $"/documents/document-{document.Id}/{previewName}";
                }

                // Tạo lại thumbnail từ file mới
                var thumbPath = Path.Combine(folder, "thumb.jpg");
                await RenderPdfThumbnail(filePath, thumbPath);
                document.Thumb = $"/documents/document-{document.Id}/thumb.jpg";
            }
            else if (string.IsNullOrEmpty(document.PreviewFileUrl) && !string.IsNullOrEmpty(document.FileUrl))
            {
                // Không có file mới nhưng chưa có preview → tạo từ file cũ
                var sourcePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    document.FileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(sourcePath))
                {
                    var previewName = "preview_" + Path.GetFileName(document.FileUrl);
                    var previewPath = Path.Combine(folder, previewName);
                    if (GeneratePreviewPdf(sourcePath, previewPath))
                    {
                        document.PreviewFileUrl = $"/documents/document-{document.Id}/{previewName}";
                    }
                }
            }

            // Cập nhật danh sách tác giả
            _context.DocumentAuthors.RemoveRange(document.DocumentAuthors);
            foreach (var authorId in authorIds)
            {
                _context.DocumentAuthors.Add(new DocumentAuthor
                {
                    DocumentId = document.Id,
                    AuthorId = authorId
                });
            }

            _context.Documents.Update(document);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật tài liệu thành công.";
            return RedirectToAction("Index");
        }

        // Xóa tài liệu và các dữ liệu liên quan
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentAuthors)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return NotFound();

            // Xóa thư mục chứa file tài liệu và QR code
            var docFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/documents", $"document-{doc.Id}");
            if (Directory.Exists(docFolder))
            {
                Directory.Delete(docFolder, recursive: true);
            }

            // Xóa dữ liệu liên quan trong DB
            _context.DocumentAuthors.RemoveRange(doc.DocumentAuthors);
            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xoá tài liệu thành công.";
            return RedirectToAction("Index");
        }
    }
}
