
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

// AccountService - Dillan Banking System
// This service creates bank accounts and manages account balances.

var builder = WebApplication.CreateBuilder(args);

// Connect to the SQLite database.
// Docker stores the database in the /data folder.
var dbPath = builder.Configuration["DB_PATH"] ?? "/data/accounts.db";

builder.Services.AddDbContext<AccountDb>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// Create the database if it does not exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountDb>();
    db.Database.EnsureCreated();
}

// Welcome message
app.MapGet("/", () => "Welcome to Dillan Banking System - Account Service!");

// Display all bank accounts.
app.MapGet("/accounts", async (AccountDb db) =>
{
    var accounts = await db.Accounts.AsNoTracking().ToListAsync();
    return accounts.Select(ToResponse);
});

// Find a bank account using its ID.
app.MapGet("/accounts/{id:int}", async (int id, AccountDb db) =>
{
    var account = await db.Accounts.AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == id);

    if (account == null)
    {
        return Results.NotFound("Account not found.");
    }

    return Results.Ok(ToResponse(account));
});

// Create a new bank account.
app.MapPost("/accounts", async (CreateAccount request, AccountDb db) =>
{
    // Make sure the account has a name.
    if (string.IsNullOrWhiteSpace(request.AccountName))
    {
        return Results.BadRequest("Account name is required.");
    }

    // New accounts start with a balance of zero.
    var account = new BankAccount
    {
        AccountName = request.AccountName.Trim(),
        BalanceCents = 0
    };

    // Save the new account to SQLite.
    db.Accounts.Add(account);
    await db.SaveChangesAsync();

    return Results.Created($"/accounts/{account.Id}", ToResponse(account));
});

// Update the account balance when money is deposited or withdrawn.
// TransactionService uses this endpoint.
app.MapPost("/accounts/{id:int}/adjust",
    async (int id, BalanceAdjustment request, AccountDb db) =>
{
    // Check that the amount is valid.
    if (request.Amount == 0 ||
        decimal.Round(request.Amount, 2) != request.Amount)
    {
        return Results.BadRequest(
            "Amount must be a nonzero value with at most two decimal places.");
    }

    // Convert dollars into cents to avoid rounding problems.
    long cents;

    try
    {
        cents = checked((long)(request.Amount * 100m));
    }
    catch (OverflowException)
    {
        return Results.BadRequest("Amount is too large.");
    }

    // Update the balance only if enough money is available.
    var affected = await db.Accounts
        .Where(a => a.Id == id && a.BalanceCents + cents >= 0)
        .ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.BalanceCents, a => a.BalanceCents + cents));

    // Show an error if the account is missing or has insufficient funds.
    if (affected == 0)
    {
        return await db.Accounts.AnyAsync(a => a.Id == id)
            ? Results.BadRequest("Insufficient funds.")
            : Results.NotFound("Account not found.");
    }

    // Return the updated account balance.
    var account = await db.Accounts.AsNoTracking()
        .FirstAsync(a => a.Id == id);

    return Results.Ok(ToResponse(account));
});

app.Run();

// Format the account information returned by the API.
static object ToResponse(BankAccount account) => new
{
    account.Id,
    account.AccountName,
    Balance = account.BalanceCents / 100m
};

// Information needed to create an account.
record CreateAccount(string AccountName);

// Information needed to update an account balance.
record BalanceAdjustment(decimal Amount);

// Bank account model.
class BankAccount
{
    public int Id { get; set; }

    [Required]
    public string AccountName { get; set; } = "";

    public long BalanceCents { get; set; }
}

// SQLite database connection.
class AccountDb(DbContextOptions<AccountDb> options) : DbContext(options)
{
    public DbSet<BankAccount> Accounts => Set<BankAccount>();
}
