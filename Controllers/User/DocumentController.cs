using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuvienso.Data;

namespace thuvienso.Controllers.User
{
    [Route("document")]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;

        public DocumentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.DocumentAuthors)
                    .ThenInclude(da => da.Author)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return NotFound();

            // +1 lượt xem
            doc.View = (doc.View ?? 0) + 1;
            await _context.SaveChangesAsync();

            // Lấy 12 tài liệu mới nhất (trừ tài liệu hiện tại)
            var newestDocs = await _context.Documents
                .Where(d => d.Id != id)
                .Include(d => d.DocumentAuthors).ThenInclude(da => da.Author)
                .OrderByDescending(d => d.Id)
                .Take(12)
                .ToListAsync();

            // Lấy 12 tài liệu có lượt xem cao nhất
            var popularDocs = await _context.Documents
                .Where(d => d.Id != id)
                .Include(d => d.DocumentAuthors).ThenInclude(da => da.Author)
                .OrderByDescending(d => d.View)
                .Take(12)
                .ToListAsync();

            ViewBag.NewestDocs = newestDocs;
            ViewBag.PopularDocs = popularDocs;

            var allCategories = await _context.Categories.ToListAsync();
            ViewBag.AllCategories = allCategories;

            return View("~/Views/User/Document/Detail.cshtml", doc);
        }


        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null || string.IsNullOrEmpty(doc.FileUrl))
                return NotFound();
            if (doc.IsFree)
            {
                // +1 lượt tải
                doc.Download = (doc.Download ?? 0) + 1;
                await _context.SaveChangesAsync();

                // Trả về file
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FileUrl.TrimStart('/'));
                var contentType = "application/octet-stream";
                return PhysicalFile(filePath, contentType, Path.GetFileName(filePath));
            }
            return Unauthorized("Tài liệu có phí. Bạn cần thanh toán trước khi tải.");
        }
    }
}
