namespace MiniErp.Application;

public record LoginRequest(string Username, string Password);

public record AuthResult(string AccessToken, string RefreshToken);

public record RefreshTokenRequest(string RefreshToken);

public record VerifyPasswordRequest(string Password, string Hash);

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResult(bool Success, string? ResetToken);

public record ResetPasswordRequest(string Token, string NewPassword);

public record SaleItemRequest(int ProductId, decimal Quantity, decimal UnitPrice);

public record CreateSaleRequest(int CustomerId, DateTime SaleDate, IReadOnlyList<SaleItemRequest> Items);

public record ProductDto(int Id, string Sku, string Name, string? Description, decimal UnitPrice, decimal? ReorderLevel, bool IsActive);

public record CreateProductRequest(string Sku, string Name, string? Description, decimal UnitPrice, decimal? ReorderLevel);

public record UpdateProductRequest(string Name, string? Description, decimal UnitPrice, decimal? ReorderLevel, bool IsActive);

public record CustomerDto(int Id, string Code, string Name, string? Email, string? Phone, string? BillingAddress, string? ShippingAddress, bool IsActive);

public record CreateCustomerRequest(string Code, string Name, string? Email, string? Phone, string? BillingAddress, string? ShippingAddress);

public record UpdateCustomerRequest(string Name, string? Email, string? Phone, string? BillingAddress, string? ShippingAddress, bool IsActive);

public record TotalSalesTodayResult(decimal TotalSalesToday);

public record RevenueByRangeResult(DateTime From, DateTime To, decimal Revenue);

public record TopProductResult(int ProductId, string Name, decimal TotalQuantity, decimal Revenue);

public record TopCustomerResult(int CustomerId, string Name, decimal Revenue);

public record StockSummaryResult(int ProductId, string Name, decimal QuantityOnHand, decimal? ReorderLevel);

public record MonthlySalesTrendPoint(int Year, int Month, decimal Revenue);

public record UserDto(int Id, string Username, string Email, bool IsActive, DateTime CreatedAt);

public record RoleDto(int Id, string Name);

public record UserRoleDto(int UserId, int RoleId, string RoleName);

public record CreateUserRequest(string Username, string Email, string Password, bool IsActive);

public record UpdateUserRequest(string Email, bool IsActive, string? Password);

public record CreateRoleRequest(string Name);

public record UpdateRoleRequest(string Name);

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public interface ISalesService
{
    Task<long> CreateSaleAsync(CreateSaleRequest request, int userId, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IReportingService
{
    Task<TotalSalesTodayResult> GetTotalSalesTodayAsync(CancellationToken cancellationToken = default);
    Task<RevenueByRangeResult> GetRevenueByRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductResult>> GetTopProductsAsync(DateTime fromDate, DateTime toDate, int top, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopCustomerResult>> GetTopCustomersAsync(DateTime fromDate, DateTime toDate, int top, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockSummaryResult>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockSummaryResult>> GetStockSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlySalesTrendPoint>> GetMonthlySalesTrendAsync(CancellationToken cancellationToken = default);
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
}

