using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using SplitApi.Data;
using SplitApi.Models;
using SplitApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------ Swagger paslaugos ------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(cfg =>
{
    cfg.SwaggerDoc("v1",
        new OpenApiInfo { Title = "Split API", Version = "v1" });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("split"));

builder.Services.AddScoped<SplitService>();
// -------------------------------------------

var app = builder.Build();

// ------------ Swagger middleware -----------
app.UseSwagger();      // JSON  → /swagger/v1/swagger.json
app.UseSwaggerUI();    // HTML  → /swagger (+ /swagger/index.html)
// -------------------------------------------

// ------------ POST /groups/{id}/members ----------- irasom nauja nari
app.MapPost("/groups/{id}/members", async (
    int id,
    MemberDto dto,
    AppDbContext db) =>
{
    var group = await db.Groups
                        .Include(g => g.Members)  // ikelia narius
                        .FirstOrDefaultAsync(g => g.Id == id);

    if (group is null)
        return Results.NotFound("Group not found");

    var member = new Member  // naujas narys
    {
        Name = dto.Name,
        GroupId = id
    };

    group.Members.Add(member);
    await db.SaveChangesAsync();  // issaugo bazeje

    return Results.Created(
        $"/groups/{id}/members/{member.Id}",
        member
    );
}
);


// ------------ POST /groups/{id}/members ----------- irasom kas sumokejo
app.MapPost("/gropus/{id}/transactions", async (
    int id,
    TransactionDto dto,      // is public record - payerID, amount
    AddDbContext db,
    SplitService splitter) =>
    {
        // grupe su nariais + senomis transakciomis
        var group = await db.Groups
                            .Include(g =>Members)
                            .Include(g =>Transactions)
                            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null)
            return Results.NotFound("Group not found");

        var tx = new Transaction      //naujas transaction objektas
        {
            Amount = dto.Amount,
            PayerId = dto.PayerId,
            GroupId = id,
            Date = DateTimeUtcNow
        };
        group.Transactions.Add(tx);
        await db.SaveChangesAsync();    // issaugau i In-memory DB

        var balance = splitter.CalculateBalances(group);   // balanso skaiciuokle

        return Results.Ok(new
        {
            transactionId = tx.Id,
            balance
        });
    });


// paprastas testas
app.MapGet("/weatherforecast", () => "ok");

app.Run();

// ------------DTO-------------
public record MemberDto(string Name);
public record TransactionDto(int PayerId, decimal Amount);
