using BookStore.Data;
using BookStore.Models;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

public class OrderController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // Temporary default user for anonymous orders (keeps existing behavior working).
    private const string DefaultUserId = "demo-user";
    private const string DefaultUserName = "demo@bookstore.local";

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Book)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            var userId = _userManager.GetUserId(User);
            query = query.Where(o => o.UserId == userId);
        }

        var orders = await query.ToListAsync();

        return View("~/Views/Orders/Index.cshtml", orders);
    }

    [HttpGet]
    [Authorize]
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

        if (!User.IsInRole("Admin"))
        {
            var userId = _userManager.GetUserId(User);
            if (!string.Equals(order.UserId, userId, StringComparison.Ordinal))
            {
                return NotFound();
            }
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

        var userId = _userManager.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                await EnsureUserRoleAssignedAsync(user);
            }
        }
        else
        {
            await EnsureDefaultUserExistsAsync();
            userId = DefaultUserId;
        }

        var order = new Order
        {
            UserId = userId,
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

    private async Task EnsureUserRoleAssignedAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "Admin")) return;
        if (await _userManager.IsInRoleAsync(user, "User")) return;

        await _userManager.AddToRoleAsync(user, "User");
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
