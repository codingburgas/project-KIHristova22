namespace BookStore.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public double Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}