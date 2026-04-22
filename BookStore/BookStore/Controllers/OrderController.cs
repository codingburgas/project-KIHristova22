using BookStore.Data;
using BookStore.Models;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

public class OrderController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    // Temporary default user (no auth yet).
    private const string DefaultUserId = "demo-user";
    private const string DefaultUserName = "demo@bookstore.local";

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Book)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View("~/Views/Orders/Index.cshtml", orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Book)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return View("~/Views/Orders/Details.cshtml", order);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int bookId)
    {
        var book = await _db.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book is null)
        {
            return NotFound();
        }

        var vm = new CreateOrderViewModel
        {
            BookId = book.Id,
            BookTitle = book.Title,
            UnitPrice = book.Price,
            Quantity = 1
        };

        return View("~/Views/Orders/Create.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        if (model.Quantity < 1)
        {
            ModelState.AddModelError(nameof(CreateOrderViewModel.Quantity), "Quantity must be at least 1.");
        }

        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == model.BookId);
        if (book is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            // Re-hydrate display-only fields.
            model.BookTitle = book.Title;
            model.UnitPrice = book.Price;
            return View("~/Views/Orders/Create.cshtml", model);
        }

        await EnsureDefaultUserExistsAsync();

        var order = new Order
        {
            UserId = DefaultUserId,
            Items = new List<OrderItem>
            {
                new()
                {
                    BookId = book.Id,
                    Quantity = model.Quantity,
                    UnitPrice = book.Price
                }
            }
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task EnsureDefaultUserExistsAsync()
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == DefaultUserId);
        if (userExists) return;

        _db.Users.Add(new ApplicationUser
        {
            Id = DefaultUserId,
            UserName = DefaultUserName,
            NormalizedUserName = DefaultUserName.ToUpperInvariant(),
            Email = DefaultUserName,
            NormalizedEmail = DefaultUserName.ToUpperInvariant(),
            EmailConfirmed = true
        });

        await _db.SaveChangesAsync();
    }
}
