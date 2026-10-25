
using Microsoft.EntityFrameworkCore;

// HistoryService - Dillan Banking System
// This service saves and displays bank transaction history.

var builder = WebApplication.CreateBuilder(args);

// Connect to the SQLite database.
// Docker stores the transaction history in the /data folder.
var dbPath = builder.Configuration["DB_PATH"] ?? "/data/history.db";

builder.Services.AddDbContext<HistoryDb>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// Create the database if it does not exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HistoryDb>();
    db.Database.EnsureCreated();
}

// Welcome message
app.MapGet("/", () => "Welcome to Dillan Banking System - History Service!");

// Display all transactions from all bank accounts.
app.MapGet("/history", async (HistoryDb db) =>
{
    return await db.Transactions
        .AsNoTracking()
        .OrderByDescending(t => t.Id)
        .ToListAsync();
});

// Display transaction history for one bank account.
app.MapGet("/history/{accountId:int}",
    async (int accountId, HistoryDb db) =>
{
    return await db.Transactions
        .AsNoTracking()
        .Where(t => t.AccountId == accountId)
        .OrderByDescending(t => t.Id)
        .ToListAsync();
});

// Save a new transaction record.
app.MapPost("/history", async (NewHistory request, HistoryDb db) =>
{
    // Make sure the transaction information is valid.
    if (request.AccountId <= 0 ||
        request.Amount <= 0 ||
        (request.Type != "Deposit" && request.Type != "Withdrawal"))
    {
        return Results.BadRequest("Invalid transaction history.");
    }

    // Create a record for the deposit or withdrawal.
    var record = new TransactionRecord
    {
        AccountId = request.AccountId,
        Type = request.Type,
        Amount = request.Amount,
        BalanceAfter = request.BalanceAfter,
        CreatedAtUtc = DateTime.UtcNow
    };

    // Save the transaction to SQLite.
    db.Transactions.Add(record);
    await db.SaveChangesAsync();

    // Return the saved transaction.
    return Results.Created($"/history/{record.AccountId}", record);
});

app.Run();

// Information needed to save a transaction.
record NewHistory(
    int AccountId,
    string Type,
    decimal Amount,
    decimal BalanceAfter
);

// Model used to store transaction history.
class TransactionRecord
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string Type { get; set; } = "";

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

// SQLite database connection for transaction history.
class HistoryDb(DbContextOptions<HistoryDb> options) : DbContext(options)
{
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
}
