using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuvienso.Data;
using thuvienso.Helpers;

namespace thuvienso.Controllers.User
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Newest = await _context.Documents
                .Include(d => d.DocumentAuthors)
                .ThenInclude(da => da.Author)
                .OrderByDescending(d => d.CreatedAt)
                .Take(12)
                .ToListAsync();

            ViewBag.MostViewed = await _context.Documents
                .Include(d => d.DocumentAuthors)
                .ThenInclude(da => da.Author)
                .OrderByDescending(d => d.View)
                .Take(12)
                .ToListAsync();

            ViewBag.Free = await _context.Documents
                .Where(d => d.IsFree == true)
                .Include(d => d.DocumentAuthors)
                .ThenInclude(da => da.Author)
                .OrderByDescending(d => d.CreatedAt)
                .Take(12)
                .ToListAsync();

            ViewBag.Paid = await _context.Documents
                .Where(d => d.IsFree == false)
                .Include(d => d.DocumentAuthors)
                .ThenInclude(da => da.Author)
                .OrderByDescending(d => d.CreatedAt)
                .Take(12)
                .ToListAsync();

            // ✅ THÊM MỚI: Random 12 tài liệu
            ViewBag.Random = await _context.Documents
                .OrderBy(d => Guid.NewGuid()) // cách phổ biến để random trong EF
                .Take(12)
                .ToListAsync();

            // Danh mục phẳng
            var allCategories = await _context.Categories.ToListAsync();
            var flatList = CategoryHelper.BuildTree(allCategories);
            ViewBag.FlatCategories = flatList;


            // Lấy 3 danh mục có nhiều tài liệu nhất, mỗi cái kèm theo 6 document mới nhất
            var topCategories = await _context.Categories
                .Where(c => c.Documents.Any())
                .OrderByDescending(c => c.Documents.Count)
                .Take(3)
                .ToListAsync();

            // Lấy luôn tài liệu theo từng danh mục
            if (topCategories.Count > 0)
            {
                topCategories[0].Documents = topCategories[0].Documents
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(6)
                    .ToList();
                ViewBag.CategoryTop1View = topCategories[0];
            }

            if (topCategories.Count > 1)
            {
                topCategories[1].Documents = topCategories[1].Documents
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(6)
                    .ToList();
                ViewBag.CategoryTop2View = topCategories[1];
            }

            if (topCategories.Count > 2)
            {
                topCategories[2].Documents = topCategories[2].Documents
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(6)
                    .ToList();
                ViewBag.CategoryTop3View = topCategories[2];
            }


            return View("~/Views/User/Home.cshtml");
        }



    }
}
