namespace SplitApi.Models;

public class Group
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public List<Member> Members { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
}
