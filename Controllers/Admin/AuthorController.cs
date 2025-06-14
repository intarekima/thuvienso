using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuvienso.Data;
using thuvienso.Models;

namespace thuvienso.Controllers.Admin
{
    [Route("admin/author")]
    public class AuthorController : Controller
    {
        private readonly AppDbContext _context;

        public AuthorController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Authors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var authors = await query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View("~/Views/Admin/Author/Index.cshtml", authors);
        }


        [HttpGet("create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Author/Create.cshtml");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, ".*[A-Za-zÀ-ỹ0-9].*"))
            {
                ModelState.AddModelError("Name", "Tên tác giả không hợp lệ.");
            }
            else if (_context.Authors.Any(a => a.Name == name))
            {
                ModelState.AddModelError("Name", "Tên tác giả đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Name"] = name;
                ViewData["Description"] = description;
                return View("~/Views/Admin/Author/Create.cshtml");
            }

            _context.Authors.Add(new Author { Name = name, Description = description });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo tác giả thành công.";
            return RedirectToAction("Index");
        }


        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
                return NotFound();

            return View("~/Views/Admin/Author/Edit.cshtml", author);
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(int id, string name, string? description)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                TempData["Error"] = "Không tìm thấy tác giả.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, ".*[A-Za-zÀ-ỹ0-9].*"))
            {
                ModelState.AddModelError("Name", "Tên tác giả không hợp lệ.");
            }
            else if (_context.Authors.Any(a => a.Name == name && a.Id != id))
            {
                ModelState.AddModelError("Name", "Tên tác giả đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                author.Name = name;
                author.Description = description;
                return View("~/Views/Admin/Author/Edit.cshtml", author);
            }

            author.Name = name;
            author.Description = description;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật tác giả thành công.";
            return RedirectToAction("Index");
        }


        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                TempData["Error"] = "Không tìm thấy tác giả để xoá.";
                return RedirectToAction("Index");
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xoá tác giả thành công.";
            return RedirectToAction("Index");
        }
    }
}
