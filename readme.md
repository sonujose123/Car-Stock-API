# Car Stock API

A C# API for dealers to manage car stock. Built with .NET 10, FastEndpoints,
Dapper, and SQLite. All database queries are written in SQL.

## Run

Install the .NET 10 SDK. The test scripts need **PowerShell 7** (`pwsh`), not
Windows PowerShell 5.1. The first build needs internet access to restore packages.

From the project folder, run:

```powershell
cd CarStock.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile --urls http://127.0.0.1:5187
```

Leave this terminal open. The API has no homepage; use the requests below.

## Sample data

`CarStock.Api/Data/carstock.db` is included, so no database server is needed.
Both demo accounts use password `DemoPassword123!`.

| Email | Cars and starting stock |
| --- | --- |
| dealer1@example.com | Audi A4 (2018): 3; Toyota Corolla (2022): 5; Ford Focus (2020): 0 |
| dealer2@example.com | Audi A4 (2018): 8; Mazda CX-5 (2021): 2; Hyundai i30 (2023): 4 |

Passwords are stored as salted hashes. Startup creates missing tables and, in
Development, missing demo accounts. Existing stock is not reset. To add missing
sample cars, run `./scripts/add-sample-data.ps1` from the project root while the
API is running. The script leaves existing stock unchanged.

## Try it

Open another PowerShell 7 terminal:

```powershell
$baseUrl = 'http://127.0.0.1:5187'
$login = Invoke-RestMethod "$baseUrl/api/auth/login" -Method Post `
    -ContentType application/json `
    -Body '{"email":"dealer1@example.com","password":"DemoPassword123!"}'
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
Invoke-RestMethod "$baseUrl/api/cars" -Headers $headers
Invoke-RestMethod "$baseUrl/api/cars?make=Audi&model=A4" -Headers $headers
```

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/auth/login` | Get a JWT |
| GET | `/api/auth/me` | Get the authenticated dealer ID |
| POST | `/api/cars` | Add a car; returns 201 and its URL |
| GET | `/api/cars/{id}` | Get a car |
| GET | `/api/cars` | List cars and stock |
| GET | `/api/cars?make=Audi&model=A4` | Search by either or both filters |
| PUT | `/api/cars/{id}/stock` | Set the stock quantity |
| DELETE | `/api/cars/{id}` | Delete a car; returns 200 and a JSON message |

All endpoints except login require a Bearer token. Send bodies as JSON.
Use the IDs returned by the API rather than assuming fixed IDs.

Add example:

```json
{"make":"Honda","model":"Civic","year":2022,"stockLevel":4}
```

Stock update example:

```json
{"stockLevel":10}
```

A stock update sets the quantity; it does not add to it. Zero stock keeps the car
listed. The same dealer cannot add a duplicate make/model/year combination.
Search supports partial matches, ignores ASCII case, and requires both filters to
match when both are supplied. Blank filters are ignored. No matches returns `[]`.

## Validation and access control

Make/model must be nonblank and at most 100 characters. Year must be an integer
between 1886 and next year. Stock must be an integer from 0 to 2,147,483,647.
Car IDs must be positive 64-bit integers. Search filters are limited to 100
characters after trimming. Login requires a valid email (up to 254 characters)
and a nonempty password (up to 256 characters).

Login issues a JWT valid for 30 minutes. The API validates its signature, issuer,
audience, and expiry. Every car SQL statement uses the dealer ID from that token.
A dealer ID supplied in a request cannot change ownership. Attempts to access
another dealer's car return 404, just like a missing car.

Errors have a shared JSON format:

```json
{
  "status": 400,
  "message": "One or more validation errors occurred.",
  "errors": {"id": ["Car ID must be a positive integer."]},
  "traceId": "request-specific-id"
}
```

Status codes: 400 invalid input, 401 invalid/missing credentials, 403 forbidden,
404 not found, 405 wrong method, 409 duplicate car, 415 wrong content type,
500 unexpected failure. Errors without field messages have an empty `errors`
object. Unexpected failures are logged; exception details are not sent to clients.

## Tests

With the API running, run these from the project root in PowerShell 7:

```powershell
./tests/auth-smoke.ps1
./tests/cars-smoke.ps1
./tests/errors-smoke.ps1
dotnet run --project tests/ErrorHandlingChecks
```

The tests throw on failure and print PASS on success. They cover login, validation,
CRUD, errors, and both dealers trying to read or change the other's stock. The car
tests use temporary entries and remove them afterward. HTTP scripts accept
`-BaseUrl` for a different address. Exception checks do not require a running API.

## Code and configuration

- `Auth`: login, token settings, and demo accounts.
- `Cars`: one file per endpoint, validation, and the SQL repository.
- `Data`: database connection, schema, and SQLite file.
- `Errors`: shared error responses and exception handling.

Development uses a random in-memory signing key, so log in again after restarting.
For a persistent key, set `Jwt__SigningKey` to a cryptographically random secret of
at least 32 bytes. It is required outside Development. `Database__Path` can point
to a separate database file. Never commit real keys or use demo accounts in production.
Use HTTPS when deploying; the HTTP command above is for local testing.

This project has no registration, refresh tokens, password reset, login throttling,
pagination, or schema migrations. Concurrent stock updates use the last successful
write. SQLite's case-insensitive search handles ASCII characters.

For submission, include the source, this README, tests/scripts, and the SQLite file.
Exclude `.vs`, `bin`, and `obj` from a ZIP. These folders are already in `.gitignore`.
Stop the API before copying the database.
