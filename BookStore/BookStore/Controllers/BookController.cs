using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

public class BookController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var books = await _db.Books
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Include(b => b.Category)
            .OrderBy(b => b.Title)
            .ToListAsync();

        // Views are stored under Views/Books (plural) in this project.
        return View("~/Views/Books/Index.cshtml", books);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View("~/Views/Books/Create.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Price,StockQuantity,CategoryId")] Book book)
    {
        if (book.CategoryId <= 0)
        {
            ModelState.AddModelError(nameof(Book.CategoryId), "Please select a category.");
        }
        else
        {
            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == book.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(Book.CategoryId), "Please select a valid category.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(book.CategoryId);
            return View("~/Views/Books/Create.cshtml", book);
        }

        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(int? selectedCategoryId = null)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
    }
}
