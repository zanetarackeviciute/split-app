using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using SplitApi.Data;
using SplitApi.Models;
using SplitApi.Services;
using System.Text.Json.Serialization;

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

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opts.SerializerOptions.WriteIndented    = true;
});


var app = builder.Build();

// ------------ Swagger middleware -----------
app.UseSwagger();      // JSON  → /swagger/v1/swagger.json
app.UseSwaggerUI();    // HTML  → /swagger (+ /swagger/index.html)
// -------------------------------------------


// ------------ GET /groups----------- visos grupes su visa info: nariais ir transakcijos
app.MapGet("/groups", async (AppDbContext db) =>
    await db.Groups
            .Include(g => g.Members)
            .Include(g => g.Transactions)
            .ToListAsync());

// ------------ POST /groups----------- nauja grupe is JSON ("title" : "....")
app.MapPost("/groups", async (AppDbContext db, GroupDto dto) =>
{
    var group = new Group {Title = dto.Title};
    db.Groups.Add(group);
    await db.SaveChangesAsync();
    return Results.Created($"/groups/{group.Id}", group);
});



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
app.MapPost("/groups/{id}/transactions", async (
    int id,
    TransactionDto dto,      // is public record - payerID, amount
    AppDbContext db,
    SplitService splitter) =>
    {
        // grupe su nariais + senomis transakciomis
        var group = await db.Groups
                            .Include(g => g.Members)
                            .Include(g => g.Transactions)
                            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null)
            return Results.NotFound("Group not found");

        var tx = new Transaction      //naujas transaction objektas
        {
            Amount = dto.Amount,
            PayerId = dto.PayerId,
            GroupId = id,
            Date = DateTime.UtcNow
        };
        group.Transactions.Add(tx);
        await db.SaveChangesAsync();    // issaugau i In-memory DB

        var balances = splitter.CalculateBalances(group);   // balanso skaiciuokle

        return Results.Ok(new
        {
            transactionId = tx.Id,
            balances
        });
    });

// ------------ GET /groups/{id}/balances ----------- irasom kas sumokejo
app.MapGet("/groups/{id}/balances", async (
    int id,
    AppDbContext db,
    SplitService splitter) =>
    {
        var group = await db.Groups
                            .Include(g => g.Members)
                            .Include(g => g.Transactions)
                            .FirstOrDefaultAsync(g => g.Id == id);
        
        if (group is null)
            return Results.NotFound("Group not found");

        var bal = splitter.CalculateBalances(group);

        var result = group.Members.Select(m => new
        {
            m.Id,
            m.Name,
            Balance = bal.GetValueOrDefault(m.Id, 0)
        });

        return Results.Ok(result);
    });


// ------------ POST /groups/{id}/settle ----------- vienas atsiskaito su kitu
app.MapPost("/groups/{id}/settle", async (
    int id,
    SettleDto dto,
    AppDbContext db,
    SplitService splitter) =>
    {
        var group = await db.Groups    // suranda grupe su nariais ir transakcijom
                            .Include(g => g.Members)
                            .Include(g => g.Transactions)
                            .FirstOrDefaultAsync(g => g.Id == id);
        if (group is null)
            return Results.NotFound("Group not found");

        // ar abu yra grupeje
        if (!group.Members.Any(m => m.Id == dto.FromId) || !group.Members.Any(m => m.Id == dto.ToId))
            return Results.BadRequest("Member id not in this group");

        // dirbtina transakcija
        var tx = new Transaction
        {
            Amount = dto.Amount,
            PayerId = dto.FromId,
            GroupId = id,
            Date = DateTime.UtcNow
        };
        group.Transactions.Add(tx);
        await db.SaveChangesAsync();

        var balances = splitter.CalculateBalances(group);

        return Results.Ok(new
        {
            settleTransactionId = tx.Id,
            balances
        });
    });


// paprastas testas
app.MapGet("/weatherforecast", () => "ok");

app.Run();

// ------------DTO-------------
public record MemberDto(string Name);
public record TransactionDto(int PayerId, decimal Amount);
public record GroupDto(string Title);
public record SettleDto(int FromId, int ToId, decimal Amount);
