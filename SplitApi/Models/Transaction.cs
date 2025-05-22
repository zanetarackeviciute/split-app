namespace SplitApi.Models;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }

    public int PayerId { get; set; }
    public Member? Payer { get; set; }

    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
}
