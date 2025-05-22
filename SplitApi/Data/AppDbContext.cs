using Microsoft.EntityFrameworkCore;
using SplitApi.Models;

namespace SplitApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) {}

    public DbSet<Group>       Groups       => Set<Group>();
    public DbSet<Member>      Members      => Set<Member>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
}
