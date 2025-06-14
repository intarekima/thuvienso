using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuvienso.Data;
using thuvienso.Models;

namespace thuvienso.Controllers.Admin
{
    [Route("admin/publisher")]
    public class PublisherController : Controller
    {
        private readonly AppDbContext _context;

        public PublisherController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Publishers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var publishers = await query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View("~/Views/Admin/Publisher/Index.cshtml", publishers);
        }


        [HttpGet("create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Publisher/Create.cshtml");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, ".*[A-Za-zÀ-ỹ0-9].*"))
            {
                ModelState.AddModelError("Name", "Tên nhà xuất bản không hợp lệ.");
            }
            else if (_context.Publishers.Any(p => p.Name == name))
            {
                ModelState.AddModelError("Name", "Tên nhà xuất bản đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Name"] = name;
                ViewData["Description"] = description;
                return View("~/Views/Admin/Publisher/Create.cshtml");
            }

            _context.Publishers.Add(new Publisher { Name = name, Description = description });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo nhà xuất bản thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
                return NotFound();

            return View("~/Views/Admin/Publisher/Edit.cshtml", publisher);
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(int id, string name, string? description)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                TempData["Error"] = "Không tìm thấy nhà xuất bản.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, ".*[A-Za-zÀ-ỹ0-9].*"))
            {
                ModelState.AddModelError("Name", "Tên nhà xuất bản không hợp lệ.");
            }
            else if (_context.Publishers.Any(p => p.Name == name && p.Id != id))
            {
                ModelState.AddModelError("Name", "Tên nhà xuất bản đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                publisher.Name = name;
                publisher.Description = description;
                return View("~/Views/Admin/Publisher/Edit.cshtml", publisher);
            }

            publisher.Name = name;
            publisher.Description = description;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật nhà xuất bản thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                TempData["Error"] = "Không tìm thấy nhà xuất bản để xoá.";
                return RedirectToAction("Index");
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xoá nhà xuất bản thành công.";
            return RedirectToAction("Index");
        }
    }
}
