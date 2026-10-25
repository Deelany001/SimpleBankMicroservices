# SimpleBankMicroservices — .NET 10, SQLite and Docker

A small **coursework demonstration**, not production banking software. Three independent ASP.NET Core minimal API services communicate over HTTP:

- **AccountService** (localhost:5001): creates accounts, retrieves accounts, adjusts balances; SQLite volume.
- **TransactionService** (localhost:5002): accepts deposits and withdrawals, calls AccountService, then calls HistoryService.
- **HistoryService** (localhost:5003): records transactions in its own SQLite volume.

## Quick start (PowerShell in the folder containing compose.yaml)

```powershell
docker compose up --build -d
docker compose ps
```

If your old `simplebank-account-container` is still using port 5001, stop/remove it first:

```powershell
docker rm -f simplebank-account-container
```

**Do not remove `dazzling_newton` or the other course's banking-app**; it uses port 8080 and does not conflict with this project.

## Demo commands (PowerShell)

```powershell
# Create an account
Invoke-RestMethod -Uri http://localhost:5001/accounts -Method Post -ContentType application/json -Body '{"accountName":"John Smith"}'

# List accounts; note the ID of the account you just created
Invoke-RestMethod http://localhost:5001/accounts

# Deposit $100 to account ID 1 (change the ID if needed)
Invoke-RestMethod -Uri http://localhost:5002/transactions/deposit -Method Post -ContentType application/json -Body '{"accountId":1,"amount":100}'

# Withdraw $25
Invoke-RestMethod -Uri http://localhost:5002/transactions/withdraw -Method Post -ContentType application/json -Body '{"accountId":1,"amount":25}'

# View balance and history
Invoke-RestMethod http://localhost:5001/accounts/1
Invoke-RestMethod http://localhost:5003/history/1
```

Expected balance after a $100 deposit and $25 withdrawal is **$75**.

To stop without deleting saved data: `docker compose down`.
To restart: `docker compose up -d`.
**Do not use `docker compose down -v`** unless you intend to delete both databases.

## Architecture

```text
User / PowerShell
    | create/list accounts           | deposit/withdraw           | view history
    v                                v                            v
AccountService <---------------- TransactionService ----------> HistoryService
 SQLite DB                              HTTP                       SQLite DB
```

## Limitations

- This is **not** suitable for real money: no login/authentication, no rate limiting, and internal balance endpoint is exposed on port 5001.
- A balance update and history write occur in different services and are **not atomic**. If history fails, balance may already have changed. Do not blindly retry failed requests.
- `EnsureCreated()` is for a simple assignment, not production schema migrations.
- Amounts are decimal and validated to two decimal places. The database uses SQLite and should be treated as a demo, not a regulated ledger.

## Sample AI architecture prompt for assignment

> Design a beginner-friendly microservices banking demonstration in ASP.NET Core .NET 10 using three independent services: AccountService, TransactionService, and HistoryService. Each persistent service should use SQLite. Explain how HTTP calls connect the services and how Docker Compose runs them.

## Sample staged coding prompt for assignment

> Stage 1: Create the .NET 10 projects and SQLite account database. Stage 2: Implement account creation, lookup, and balance adjustments. Stage 3: Implement deposit/withdrawal orchestration in TransactionService. Stage 4: Implement transaction history storage and retrieval. Stage 5: Add Dockerfiles, Docker Compose, and API smoke tests.

## Example reflection (edit to match what you personally did)

> I learned that microservices split an application into smaller programs that each have a job. AccountService stores bank accounts, TransactionService handles deposits and withdrawals, and HistoryService saves transaction records. Docker Compose helped me run all three services together. One challenge was getting the services to communicate correctly. I also learned that saving a balance and saving its history are two separate actions, so a real banking app would need extra protection against errors.

## GitHub submission

In the project root:

```powershell
git init
git add .
git commit -m "Complete banking microservices assignment"
```

Create an empty repository on GitHub, then run the remote/push commands GitHub shows. Submit the repository URL and your assignment answers. Do not claim any test passed unless you ran it.
