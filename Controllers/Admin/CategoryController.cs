using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuvienso.Data;
using thuvienso.Models;
using thuvienso.Helpers;
using static thuvienso.Models.Category;

namespace thuvienso.Controllers.Admin
{
    [Route("admin/category")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search)
        {
            // Luôn load toàn bộ cây
            var allCategories = await _context.Categories
                .Include(c => c.Parent)
                .ToListAsync();

            // Dựng cây
            var tree = CategoryHelper.BuildTree(allCategories);

            // Đánh dấu những thằng khớp kết quả tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                foreach (var item in tree)
                {
                    if (item.Category.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Matched = true;
                    }
                }
            }

            ViewBag.Search = search;
            return View("~/Views/Admin/Category/Index.cshtml", tree);
        }



        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.ParentCategories = new SelectList(categories, "Id", "Name");
            return View("~/Views/Admin/Category/Create.cshtml");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(string name, int? parentId, string? description)
        {
            name = name?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.All(char.IsDigit))
            {
                TempData["Error"] = "Tên danh mục không hợp lệ.";

                ViewBag.ParentCategories = new SelectList(
                    await _context.Categories.ToListAsync(), "Id", "Name", parentId);

                return View("~/Views/Admin/Category/Create.cshtml");
            }

            var exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());

            if (exists)
            {
                TempData["Error"] = "Tên danh mục đã tồn tại.";

                ViewBag.ParentCategories = new SelectList(
                    await _context.Categories.ToListAsync(), "Id", "Name", parentId);

                return View("~/Views/Admin/Category/Create.cshtml");
            }

            var category = new Category
            {
                Name = name,
                ParentId = parentId,
                Description = description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công.";
            return RedirectToAction("Index");
        }


        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            // B1: Lấy toàn bộ danh mục
            var allCategories = await _context.Categories.ToListAsync();

            // B2: Tìm danh sách ID con cháu của chính nó (đệ quy nội bộ)
            List<int> GetDescendantIds(int parentId)
            {
                var result = new List<int> { parentId };

                var children = allCategories
                    .Where(c => c.ParentId == parentId)
                    .Select(c => c.Id)
                    .ToList();

                foreach (var childId in children)
                {
                    result.AddRange(GetDescendantIds(childId));
                }

                return result;
            }

            var excludeIds = GetDescendantIds(id);

            // B3: Tạo dropdown danh mục cha, loại trừ chính nó và con cháu
            var parentOptions = allCategories
                .Where(c => !excludeIds.Contains(c.Id))
                .ToList();

            ViewBag.ParentCategories = new SelectList(
                parentOptions,
                "Id", "Name", category.ParentId
            );

            return View("~/Views/Admin/Category/Edit.cshtml", category);
        }


        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(int id, string name, int? parentId, string? description)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return RedirectToAction("Index");
            }

            name = name?.Trim();

            // Kiểm tra rỗng hoặc toàn số
            if (string.IsNullOrWhiteSpace(name) || name.All(char.IsDigit))
            {
                TempData["Error"] = "Tên không hợp lệ.";
                goto Invalid;
            }

            // Kiểm tra trùng tên (trừ chính nó)
            bool isDuplicate = await _context.Categories
                .AnyAsync(c => c.Id != id && c.Name == name);
            if (isDuplicate)
            {
                TempData["Error"] = "Tên danh mục đã tồn tại.";
                goto Invalid;
            }

            // OK -> cập nhật
            category.Name = name;
            category.ParentId = parentId;
            category.Description = description;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thành công.";
            return RedirectToAction("Index");

        Invalid:
            // Load lại dropdown
            ViewBag.ParentCategories = new SelectList(
                await _context.Categories.Where(c => c.Id != id).ToListAsync(),
                "Id", "Name", parentId
            );
            category.Name = name;
            category.ParentId = parentId;
            category.Description = description;
            return View("~/Views/Admin/Category/Edit.cshtml", category);
        }



        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục để xoá.";
                return RedirectToAction("Index");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xoá danh mục thành công.";
            return RedirectToAction("Index");
        }



    }
}
