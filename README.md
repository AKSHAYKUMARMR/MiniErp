# MiniErp API

Mini ERP backend built with **.NET 8**, **SQL Server**, and **Clean Architecture**. Provides REST APIs for auth, products, customers, sales, reports, users, and roles.

---

## Prerequisites

- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** — LocalDB, Express, or full SQL Server

---

## Setup Instructions

### 1. Clone / open the solution

```bash
cd MiniErp
```

### 2. Create the database and schema

Using **SQL Server Management Studio**, **Azure Data Studio**, or **sqlcmd**, run the scripts **in this order**:

| Order | Script | Purpose |
|-------|--------|---------|
| 1 | `db/create-schema.sql` | Creates database (if missing), tables, FKs, indexes |
| 2 | `db/sp-create-sale.sql` | Creates `Sp_CreateSale` stored procedure |
| 3 | `db/seed-data.sql` | Inserts roles, users, products, customers, sample stock & sales |

**Example (sqlcmd):**

```bash
sqlcmd -S localhost -U sa -P YourPassword -i db/create-schema.sql
sqlcmd -S localhost -U sa -P YourPassword -i db/sp-create-sale.sql
sqlcmd -S localhost -U sa -P YourPassword -i db/seed-data.sql
```

If the database already exists, run only the scripts you need (e.g. skip `create-schema` or run only the procedure/seed parts).

### 3. Set passwords for seed users (required for login)

Seed data inserts users with **placeholder password hashes**. To log in, set real hashes:

**Option A — Using the API (recommended)**

1. Start the API (step 4 below).
2. Call **POST /api/auth/hash** with body: `"admin@123"` (JSON string).
3. Copy the returned hash.
4. Run this in SQL (replace `PASTE_HASH_HERE` with the hash):

```sql
USE MiniErp;
UPDATE Users SET PasswordHash = 'PASTE_HASH_HERE' WHERE Username = 'admin';
UPDATE Users SET PasswordHash = 'PASTE_HASH_HERE' WHERE Username = 'user1';
```

Or use the helper script: edit `db/seed-update-passwords.sql`, replace `PASTE_HASH_HERE`, then run it.

**Option B — Pre-set hash in seed**

If you prefer, you can replace the dummy hashes in `db/seed-data.sql` with a hash from a previous run of `/api/auth/hash` so the database is ready to use right after seed.

**Seed users**

| Username | Password (after update) | Role  |
|----------|--------------------------|-------|
| admin    | admin@123                 | Admin |
| user1    | admin@123                 | User  |

Login uses **username** (e.g. `admin`), not email.

### 4. Configure the API

Edit **`MiniErp/appsettings.json`**:

- **Connection string** — Set `ConnectionStrings:DefaultConnection` to your SQL Server instance and credentials.

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=YOUR_SERVER;Initial Catalog=MiniErp;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True"
}
```

- **JWT** (optional) — Change `Jwt:Key` to a secure base64 secret (e.g. 32+ bytes). Keep `Issuer`/`Audience` or adjust for your environment.

### 5. Build and run

```bash
dotnet restore
dotnet build
dotnet run --project MiniErp
```

The API listens on the URLs shown in the console (e.g. `https://localhost:7089` or `https://localhost:5001`).

### 6. Open Swagger and log in

1. Open **Swagger UI**: `https://localhost:<port>/swagger` (use the port from the console).
2. Call **POST /api/auth/login** with body:

```json
{
  "username": "admin",
  "password": "admin@123"
}
```

3. Copy the **`accessToken`** from the response.
4. Click **Authorize**, paste **only the token** (no `Bearer` prefix) into the value field, then click **Authorize** and **Close**.
5. Call any protected endpoint (e.g. **GET /api/products**). You should get **200** instead of 401.

---

## Project structure

| Project | Description |
|---------|-------------|
| **MiniErp** | ASP.NET Core Web API, controllers, JWT, Swagger, middleware |
| **MiniErp.Domain** | Entities and domain models |
| **MiniErp.Application** | DTOs, request/response types, service interfaces |
| **MiniErp.Infrastructure** | EF Core DbContext, auth/sales/reporting/user services, SQL Server |

---

## Authentication & security

- **JWT access token** — Short-lived (default 15 min). Send as `Authorization: Bearer <accessToken>`.
- **Refresh token** — Long-lived (default 7 days). Use **POST /api/auth/refresh** with the refresh token to get a new access token (and new refresh token). Stored in `RefreshTokens` with rotation.
- **Roles** — `Admin` (full access), `User` (e.g. create sales). Products, Customers, Users, Roles, and Reports require **Admin**.
- **Password hashing** — PBKDF2 (salt + SHA-256). Use **POST /api/auth/hash** to generate hashes for seed data.

---

## Main API areas

- **Auth** (`/api/auth`) — Login, refresh, logout, forgot/reset password, me, hash/verify helpers.
- **Products** (`/api/products`, Admin) — CRUD.
- **Customers** (`/api/customers`, Admin) — CRUD.
- **Sales** (`/api/sales`, any authenticated user) — **POST** to create a sale (uses `Sp_CreateSale` with JSON items; updates stock).
- **Reports** (`/api/reports`, Admin) — Total sales today, revenue by range, top products/customers, low stock, stock summary, monthly trend.
- **Users** (`/api/users`, Admin) — CRUD and role assign/remove.
- **Roles** (`/api/roles`, Admin) — CRUD.

Full list and request shapes: see **`docs/API-Summary.md`**.

---

## Postman

Import **`docs/MiniErp-Postman-Collection.json`** into Postman.

- Set collection variable **`baseUrl`** to your API base (e.g. `https://localhost:7089`).
- Run **Auth → Login**; the collection script stores **`accessToken`** so other requests use it automatically.

---

## Database scripts reference

| File | Purpose |
|------|---------|
| `db/create-schema.sql` | Database + tables + PasswordResetTokens |
| `db/sp-create-sale.sql` | `Sp_CreateSale` (sale + items + stock in one transaction; sale number `S-{SaleId}`) |
| `db/seed-data.sql` | Roles, users (dummy hashes), products, customers, opening stock, sample sales |
| `db/seed-update-passwords.sql` | Updates user hashes after you get them from **POST /api/auth/hash** |


---

.
