namespace BookStore.Models;

public class OrderItem
{
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
}