# MiniErp API Summary

Base path: `/api`  
Authentication: JWT Bearer (except Auth endpoints marked public).  
Get token: `POST /api/auth/login` → use `accessToken` in header: `Authorization: Bearer <accessToken>`.

---

## Auth — `/api/auth`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | No | Login. Body: `{ "username", "password" }`. Returns `accessToken`, `refreshToken`. |
| POST | `/api/auth/refresh` | No | Refresh tokens. Body: `{ "refreshToken" }`. |
| POST | `/api/auth/logout` | No | Logout (revoke refresh token). Body: `{ "refreshToken" }`. |
| GET | `/api/auth/me` | Yes | Current user and roles (validates token). |
| POST | `/api/auth/forgot-password` | No | Request password reset. Body: `{ "email" }`. |
| POST | `/api/auth/reset-password` | No | Reset password. Body: `{ "token", "newPassword" }`. |
| POST | `/api/auth/hash` | No | Dev: hash a password. Body: plain string. |
| POST | `/api/auth/verify` | No | Dev: verify password vs hash. Body: `{ "password", "hash" }`. |

---

## Products — `/api/products` (Admin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List all products. |
| GET | `/api/products/{id}` | Get product by ID. |
| POST | `/api/products` | Create. Body: `sku`, `name`, `description?`, `unitPrice`, `reorderLevel?`. |
| PUT | `/api/products/{id}` | Update. Body: `name`, `description?`, `unitPrice`, `reorderLevel?`, `isActive`. |
| DELETE | `/api/products/{id}` | Delete product. |

---

## Customers — `/api/customers` (Admin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | List all customers. |
| GET | `/api/customers/{id}` | Get customer by ID. |
| POST | `/api/customers` | Create. Body: `code`, `name`, `email?`, `phone?`, `billingAddress?`, `shippingAddress?`. |
| PUT | `/api/customers/{id}` | Update. Body: `name`, `email?`, `phone?`, `billingAddress?`, `shippingAddress?`, `isActive`. |
| DELETE | `/api/customers/{id}` | Delete customer. |

---

## Sales — `/api/sales` (Any authenticated user)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sales` | Create sale. Body: `customerId`, `saleDate` (ISO), `items`: `[{ "productId", "quantity", "unitPrice" }]`. Returns sale ID. |

---

## Reports — `/api/reports` (Admin)

| Method | Endpoint | Query / Description |
|--------|----------|---------------------|
| GET | `/api/reports/total-sales-today` | Total sales for today. |
| GET | `/api/reports/revenue` | Revenue in range. Query: `from`, `to` (ISO date). |
| GET | `/api/reports/top-products` | Top products by revenue. Query: `from`, `to`, `top` (default 10). |
| GET | `/api/reports/top-customers` | Top customers by revenue. Query: `from`, `to`, `top` (default 10). |
| GET | `/api/reports/low-stock-products` | Products below reorder level. |
| GET | `/api/reports/stock-summary` | All products stock summary. |
| GET | `/api/reports/monthly-sales-trend` | Monthly sales trend. |

---

## Users — `/api/users` (Admin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List all users. |
| GET | `/api/users/{id}` | Get user by ID. |
| POST | `/api/users` | Create. Body: `username`, `email`, `password`, `isActive`. |
| PUT | `/api/users/{id}` | Update. Body: `email`, `isActive`, `password?`. |
| DELETE | `/api/users/{id}` | Delete user. |
| GET | `/api/users/{id}/roles` | Get user's roles. |
| POST | `/api/users/{id}/roles/{roleId}` | Assign role to user. |
| DELETE | `/api/users/{id}/roles/{roleId}` | Remove role from user. |

---

## Roles — `/api/roles` (Admin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/roles` | List all roles. |
| GET | `/api/roles/{id}` | Get role by ID. |
| POST | `/api/roles` | Create. Body: `{ "name" }`. |
| PUT | `/api/roles/{id}` | Update. Body: `{ "name" }`. |
| DELETE | `/api/roles/{id}` | Delete role. |

---

## Roles & access

- **Admin**: Products, Customers, Users, Roles, Reports (all of the above).
- **User**: Sales (create sale), Auth/me.
- **401**: Missing or invalid token.
- **403**: Valid token but insufficient role (e.g. User calling Admin-only endpoint).
