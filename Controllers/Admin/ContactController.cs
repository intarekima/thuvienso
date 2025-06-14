using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuvienso.Data;
using thuvienso.Models;

namespace thuvienso.Controllers.Admin
{
    [Route("admin/contact")]
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;

        public ContactController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Contacts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var contacts = await query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View("~/Views/Admin/Contact/Index.cshtml", contacts);
        }
    }
}
