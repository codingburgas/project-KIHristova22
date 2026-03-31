namespace BookStore.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<OrderItem> OrderItem { get; set; }
}