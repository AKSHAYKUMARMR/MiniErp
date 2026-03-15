# Mini ERP Backend System – Project Presentation

## 1. Project Summary

**Mini ERP** is a backend API system built with **.NET 8**, **SQL Server**, and **Clean Architecture**. It provides:

- **Authentication** – JWT with refresh tokens
- **Product Management** – CRUD for products
- **Customer Management** – CRUD for customers
- **Sales Management** – Create sales with multiple items
- **Stock Management** – Ledger-based stock tracking
- **Insight Reporting** – Business analytics APIs
- **User & Role Administration** – CRUD for users, roles, and role assignments

The system is designed for maintainability, testability, and clear separation of concerns.

---

## 2. Architecture Overview

### 2.1 Clean Architecture (Four Layers)

The solution follows **Clean Architecture** with four projects:

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Domain** | MiniErp.Domain | Core entities (User, Role, Product, Customer, Sale, SaleItem, StockLedgerEntry, etc.) |
| **Application** | MiniErp.Application | DTOs, service interfaces, use-case contracts |
| **Infrastructure** | MiniErp.Infrastructure | EF Core DbContext, repository-style services, SQL Server |
| **API** | MiniErp | Controllers, DI, middleware, JWT configuration |

### 2.2 Project Dependencies

```
MiniErp (API)
    ├── MiniErp.Application
    │       └── MiniErp.Domain
    └── MiniErp.Infrastructure
            ├── MiniErp.Domain
            └── MiniErp.Application
```

- **Domain** has no dependencies.
- **Application** depends only on Domain.
- **Infrastructure** depends on Domain and Application.
- **API** depends on Application and Infrastructure.

### 2.3 Separation of Concerns

- **Domain**: Entities, no business logic, no framework references.
- **Application**: Interfaces and DTOs; no infrastructure details.
- **Infrastructure**: Database access, external services, concrete implementations.
- **API**: HTTP handling, routing, middleware, configuration.

---

## 3. Dependency Injection

All services are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddDbContext<MiniErpDbContext>(options =>
    options.UseSqlServer(connectionString));
```

- **Scoped** services: one instance per HTTP request.
- **DbContext**: Scoped, shared across requests.

---

## 4. Entity Framework Core & Repository Pattern

- **EF Core** is used for database access via `MiniErpDbContext`.
- **DbSets** act as repositories: `Users`, `Roles`, `Products`, `Customers`, `Sales`, `SalesItems`, `StockLedgerEntries`, `RefreshTokens`, `UserRoles`.
- **Fluent API** configures entities in `OnModelCreating` (table names, keys, indexes, decimal types).
- **Stored procedure** `Sp_CreateSale` is used for sales creation to enforce business rules and transactions in SQL.

---

## 5. Database Connection

### 5.1 Connection String

Configured in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=LAPTOP-E0OPQ035;Initial Catalog=MiniErp;User ID=sa;Password=admin@123;TrustServerCertificate=True"
}
```

### 5.2 Connection Flow

1. `Program.cs` reads `ConnectionStrings:DefaultConnection`.
2. `AddDbContext<MiniErpDbContext>` configures EF Core with SQL Server.
3. `MiniErpDbContext` uses `UseSqlServer(connectionString)` for all queries.

---

## 6. Security Implementation

### 6.1 JWT Access Token – 15 Minutes Expiry

- Configured in `appsettings.json`:

```json
"Jwt": {
  "AccessTokenMinutes": 15,
  "Key": "<secure-32-char-key>",
  "Issuer": "MiniErp",
  "Audience": "MiniErp"
}
```

- `AuthService` uses `JwtSecurityTokenHandler` with `Expires = DateTime.UtcNow.AddMinutes(15)`.
- `ClockSkew = TimeSpan.Zero` prevents extra time after expiry.

### 6.2 Refresh Token – 7 Days Expiry

- Configured in `appsettings.json`:

```json
"RefreshTokenDays": 7
```

- Refresh tokens are stored in `RefreshTokens` with `ExpiresAt = DateTime.UtcNow.AddDays(7)`.
- Each token is validated on use and revoked when replaced.

### 6.3 Refresh Token Rotation

- On refresh:
  1. Old refresh token is validated and marked as revoked.
  2. New access token and new refresh token are issued.
  3. Old token’s `ReplacedByToken` is set to the new refresh token.
  4. New token is stored in `RefreshTokens`.

### 6.4 Secure Password Hashing

- **PBKDF2** with SHA-256, 100,000 iterations.
- 16-byte random salt per password.
- 32-byte hash stored in `Users.PasswordHash` as Base64.
- `AuthService.HashPassword()` and `VerifyPassword()` use `Rfc2898DeriveBytes` and `CryptographicOperations.FixedTimeEquals`.

### 6.5 Role-Based Authorization (Admin / User)

- **Admin**: Full access to Products, Customers, Users, Roles.
- **User**: Access to Sales, Reports, and own profile.
- Controllers use `[Authorize(Roles = "Admin")]` or `[Authorize]` as needed.
- `AuthService` adds role claims to the JWT from `UserRoles`.

### 6.6 Global Exception Handling Middleware

- `ExceptionHandlingMiddleware` wraps all requests.
- `UnauthorizedAccessException` → HTTP 401 with message.
- Other exceptions → HTTP 500 with `traceId`, `statusCode`, `message`.
- Logs errors before returning.

---

## 7. API Endpoints Summary

| Module | Endpoint | Method | Auth |
|--------|----------|--------|------|
| Auth | /api/auth/login | POST | Anonymous |
| Auth | /api/auth/refresh | POST | Anonymous |
| Auth | /api/auth/hash | POST | Anonymous (dev) |
| Products | /api/products | GET, POST | Admin |
| Products | /api/products/{id} | GET, PUT, DELETE | Admin |
| Customers | /api/customers | GET, POST | Admin |
| Customers | /api/customers/{id} | GET, PUT, DELETE | Admin |
| Users | /api/users | GET, POST | Admin |
| Users | /api/users/{id} | GET, PUT, DELETE | Admin |
| Users | /api/users/{id}/roles | GET | Admin |
| Users | /api/users/{id}/roles/{roleId} | POST, DELETE | Admin |
| Roles | /api/roles | GET, POST | Admin |
| Roles | /api/roles/{id} | GET, PUT, DELETE | Admin |
| Sales | /api/sales | POST | Authenticated |
| Reports | /api/reports/* | GET | Authenticated |

---

## 8. Database Schema

- **Users**, **Roles**, **UserRoles** – Authentication and authorization.
- **RefreshTokens** – Refresh token storage.
- **Products**, **Customers** – Master data.
- **Sales**, **SalesItems** – Sales transactions.
- **StockLedger** – Stock movements (ledger).

---

## 9. Files & Scripts

| File | Purpose |
|------|---------|
| db/create-schema.sql | Create database and tables |
| db/sp-create-sale.sql | Stored procedure for sales |
| db/seed-data.sql | Sample data |
| db/seed-update-passwords.sql | Update password hashes |
| db/user-admin-examples.sql | SQL CRUD examples |
| er-diagram.mmd | Mermaid ER diagram |
| Postman/MiniErp.postman_collection.json | Postman collection |

---

## 10. Technology Stack

- **.NET 8** – Web API
- **SQL Server** – Database
- **Entity Framework Core 8** – ORM
- **JWT Bearer** – Authentication
- **Swagger** – API documentation

---

*End of Project Presentation Document*
