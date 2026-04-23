using BookStore.Data;
using BookStore.Models;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

public class BookController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
        ViewBag.SelectedCategoryId = categoryId;

        var query = _db.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .OrderBy(b => b.Title)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        var books = await query.ToListAsync();

        // Views are stored under Views/Books (plural) in this project.
        return View("~/Views/Books/Index.cshtml", books);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book is null)
        {
            return NotFound();
        }

        // Views are stored under Views/Books (plural) in this project.
        return View("~/Views/Books/Details.cshtml", book);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TopBooks()
    {
        var topBooks = await _db.OrderItems
            .AsNoTracking()
            .GroupBy(oi => new { oi.BookId, Title = oi.Book.Title })
            .Select(g => new TopBookViewModel
            {
                Title = g.Key.Title,
                TotalSoldQuantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalSoldQuantity)
            .ThenBy(x => x.Title)
            .Take(5)
            .ToListAsync();

        return View("~/Views/Books/TopBooks.cshtml", topBooks);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View("~/Views/Books/Create.cshtml");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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