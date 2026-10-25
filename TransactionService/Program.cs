
using System.Net.Http.Json;

// TransactionService - Dillan Banking System
// This service handles deposits and withdrawals.
// It communicates with AccountService and HistoryService.

var builder = WebApplication.CreateBuilder(args);

// Connect to AccountService to update account balances.
builder.Services.AddHttpClient("accounts", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ACCOUNT_SERVICE_URL"]
        ?? "http://accountservice:8080");
});

// Connect to HistoryService to save transaction records.
builder.Services.AddHttpClient("history", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["HISTORY_SERVICE_URL"]
        ?? "http://historyservice:8080");
});

var app = builder.Build();

// Welcome message
app.MapGet("/", () => "Welcome to Dillan Banking System - Transaction Service!");

// Deposit money into an account.
app.MapPost("/transactions/deposit",
    (MoneyRequest request, IHttpClientFactory clients) =>
{
    return Process(request, "Deposit", clients);
});

// Withdraw money from an account.
app.MapPost("/transactions/withdraw",
    (MoneyRequest request, IHttpClientFactory clients) =>
{
    return Process(request, "Withdrawal", clients);
});

app.Run();

// Process a deposit or withdrawal.
static async Task<IResult> Process(
    MoneyRequest request,
    string type,
    IHttpClientFactory clients)
{
    // Check that the account ID and amount are valid.
    if (request.AccountId <= 0 ||
        request.Amount <= 0 ||
        decimal.Round(request.Amount, 2) != request.Amount)
    {
        return Results.BadRequest(
            "Enter a valid account ID and positive amount (max 2 decimal places).");
    }

    try
    {
        // Connect to AccountService.
        var accounts = clients.CreateClient("accounts");

        // Deposits add money, while withdrawals subtract money.
        var change = type == "Deposit"
            ? request.Amount
            : -request.Amount;

        // Send the balance change to AccountService.
        using var response = await accounts.PostAsJsonAsync(
            $"/accounts/{request.AccountId}/adjust",
            new { Amount = change });

        // Return an error if the transaction cannot be completed.
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            return Results.Text(
                error,
                "application/json",
                statusCode: (int)response.StatusCode);
        }

        // Get the updated account information.
        var account = await response.Content.ReadFromJsonAsync<AccountResult>();

        if (account is null)
        {
            return Results.Problem(
                "Account service returned an empty response.");
        }

        // Connect to HistoryService.
        var history = clients.CreateClient("history");

        // Save the deposit or withdrawal in transaction history.
        using var logResponse = await history.PostAsJsonAsync(
            "/history",
            new
            {
                request.AccountId,
                Type = type,
                request.Amount,
                BalanceAfter = account.Balance
            });

        // Show an error if the balance changed but history was not saved.
        if (!logResponse.IsSuccessStatusCode)
        {
            return Results.Problem(
                "Balance updated, but history recording failed. Do not retry without checking the balance.",
                statusCode: 502);
        }

        // Return the completed transaction.
        return Results.Ok(new
        {
            request.AccountId,
            Type = type,
            request.Amount,
            account.Balance
        });
    }
    catch (HttpRequestException)
    {
        return Results.Problem(
            "A required service is unavailable. Check balances before retrying.",
            statusCode: 503);
    }
}

// Information needed for a deposit or withdrawal.
record MoneyRequest(int AccountId, decimal Amount);

// Account information returned by AccountService.
record AccountResult(int Id, string AccountName, decimal Balance);
