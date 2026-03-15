using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiniErp.Domain;
using MiniErp.Application;

namespace MiniErp.Infrastructure;

public class MiniErpDbContext : DbContext
{
    public MiniErpDbContext(DbContextOptions<MiniErpDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ReorderLevel).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SaleNumber).IsUnique();
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.CustomerId);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.SalesCreated)
                .HasForeignKey(x => x.CreatedByUserId);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.ToTable("SalesItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Sale)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.SaleId);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.SaleItems)
                .HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<StockLedgerEntry>(entity =>
        {
            entity.ToTable("StockLedger");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuantityChange).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Product)
                .WithMany(x => x.StockLedgerEntries)
                .HasForeignKey(x => x.ProductId);
        });
    }
}

public class AuthService : IAuthService
{
    private readonly MiniErpDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(MiniErpDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return await GenerateTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existingToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles!)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;

        var result = await GenerateTokensAsync(existingToken.User, cancellationToken);

        existingToken.ReplacedByToken = result.RefreshToken;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existingToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken != null && !existingToken.IsRevoked)
        {
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new ForgotPasswordResult(true, null);
        }

        var expiryHours = int.Parse(_configuration["PasswordReset:TokenExpiryHours"] ?? "1");
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            IsUsed = false
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResult(true, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var resetToken = await _dbContext.PasswordResetTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset token.");
        }

        resetToken.User.PasswordHash = HashPassword(request.NewPassword);
        resetToken.IsUsed = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResult> GenerateTokensAsync(User user, CancellationToken cancellationToken)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var accessTokenMinutes = int.Parse(jwtSection["AccessTokenMinutes"] ?? "15");
        var refreshTokenDays = int.Parse(jwtSection["RefreshTokenDays"] ?? "7");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var identity = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username)
        });

        foreach (var role in user.UserRoles.Select(ur => ur.Role.Name))
        {
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(tokenDescriptor);
        var accessToken = handler.WriteToken(token);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, refreshToken);
    }

    public static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var result = new byte[48];
        Buffer.BlockCopy(salt, 0, result, 0, 16);
        Buffer.BlockCopy(hash, 0, result, 16, 32);
        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Verifies if a password matches a stored hash. Use to check if hash corresponds to a known password.
    /// </summary>
    public static bool VerifyPasswordHash(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;
        try
        {
            return VerifyPassword(password, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var bytes = Convert.FromBase64String(storedHash);
        var salt = new byte[16];
        Buffer.BlockCopy(bytes, 0, salt, 0, 16);
        var storedSubkey = new byte[32];
        Buffer.BlockCopy(bytes, 16, storedSubkey, 0, 32);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var generatedSubkey = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(storedSubkey, generatedSubkey);
    }
}

public class SalesService : ISalesService
{
    private readonly MiniErpDbContext _dbContext;

    public SalesService(MiniErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<long> CreateSaleAsync(CreateSaleRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var itemsJson = System.Text.Json.JsonSerializer.Serialize(
            request.Items.Select(i => new { i.ProductId, i.Quantity, i.UnitPrice }));

        var connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        await using var command = new SqlCommand("dbo.Sp_CreateSale", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = request.CustomerId });
        command.Parameters.Add(new SqlParameter("@SaleDate", SqlDbType.DateTime2) { Value = request.SaleDate });
        command.Parameters.Add(new SqlParameter("@CreatedByUserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@ItemsJson", SqlDbType.NVarChar, -1) { Value = itemsJson });

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
        {
            throw new InvalidOperationException("Sale creation stored procedure did not return a SaleId.");
        }

        return Convert.ToInt64(result);
    }
}

public class ProductService : IProductService
{
    private readonly MiniErpDbContext _dbContext;

    public ProductService(MiniErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.UnitPrice, p.ReorderLevel, p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.UnitPrice, p.ReorderLevel, p.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            throw new KeyNotFoundException("Product not found.");
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UnitPrice = request.UnitPrice;
        entity.ReorderLevel = request.ReorderLevel;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            return;
        }

        _dbContext.Products.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class CustomerService : ICustomerService
{
    private readonly MiniErpDbContext _dbContext;

    public CustomerService(MiniErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto(c.Id, c.Code, c.Name, c.Email, c.Phone, c.BillingAddress, c.ShippingAddress, c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto(c.Id, c.Code, c.Name, c.Email, c.Phone, c.BillingAddress, c.ShippingAddress, c.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Customer
        {
            Code = request.Code,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            BillingAddress = request.BillingAddress,
            ShippingAddress = request.ShippingAddress,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        entity.Name = request.Name;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.BillingAddress = request.BillingAddress;
        entity.ShippingAddress = request.ShippingAddress;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            return;
        }

        _dbContext.Customers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class ReportingService : IReportingService
{
    private readonly MiniErpDbContext _dbContext;

    public ReportingService(MiniErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TotalSalesTodayResult> GetTotalSalesTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var total = await _dbContext.Sales
            .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        return new TotalSalesTodayResult(total);
    }

    public async Task<RevenueByRangeResult> GetRevenueByRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var endExclusive = toDate.Date.AddDays(1);
        var total = await _dbContext.Sales
            .Where(s => s.SaleDate >= fromDate.Date && s.SaleDate < endExclusive)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        return new RevenueByRangeResult(fromDate.Date, toDate.Date, total);
    }

    public async Task<IReadOnlyList<TopProductResult>> GetTopProductsAsync(DateTime fromDate, DateTime toDate, int top, CancellationToken cancellationToken = default)
    {
        var endExclusive = toDate.Date.AddDays(1);

        var query = from si in _dbContext.SaleItems
                    join s in _dbContext.Sales on si.SaleId equals s.Id
                    join p in _dbContext.Products on si.ProductId equals p.Id
                    where s.SaleDate >= fromDate.Date && s.SaleDate < endExclusive
                    group new { si, p } by new { si.ProductId, p.Name } into g
                    orderby g.Sum(x => x.si.Quantity) descending
                    select new TopProductResult(
                        g.Key.ProductId,
                        g.Key.Name,
                        g.Sum(x => x.si.Quantity),
                        g.Sum(x => x.si.LineTotal));

        return await query.Take(top).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopCustomerResult>> GetTopCustomersAsync(DateTime fromDate, DateTime toDate, int top, CancellationToken cancellationToken = default)
    {
        var endExclusive = toDate.Date.AddDays(1);

        var query = from s in _dbContext.Sales
                    join c in _dbContext.Customers on s.CustomerId equals c.Id
                    where s.SaleDate >= fromDate.Date && s.SaleDate < endExclusive
                    group new { s, c } by new { c.Id, c.Name } into g
                    orderby g.Sum(x => x.s.TotalAmount) descending
                    select new TopCustomerResult(
                        g.Key.Id,
                        g.Key.Name,
                        g.Sum(x => x.s.TotalAmount));

        return await query.Take(top).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockSummaryResult>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .Where(p => p.ReorderLevel != null)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ReorderLevel,
                QtyOnHand = _dbContext.StockLedgerEntries
                    .Where(sl => sl.ProductId == p.Id)
                    .Sum(sl => sl.QuantityChange)
            })
            .Where(x => x.QtyOnHand <= x.ReorderLevel!.Value)
            .Select(x => new StockSummaryResult(x.Id, x.Name, x.QtyOnHand, x.ReorderLevel));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockSummaryResult>> GetStockSummaryAsync(CancellationToken cancellationToken = default)
    {
        var stock = _dbContext.StockLedgerEntries
            .GroupBy(sl => sl.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantityOnHand = g.Sum(x => x.QuantityChange)
            });

        var query = from p in _dbContext.Products
                    join s in stock on p.Id equals s.ProductId into ps
                    from s in ps.DefaultIfEmpty()
                    let qty = s == null ? 0m : s.QuantityOnHand
                    select new StockSummaryResult(p.Id, p.Name, qty, p.ReorderLevel);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlySalesTrendPoint>> GetMonthlySalesTrendAsync(CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sales
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlySalesTrendPoint(
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.TotalAmount)));

        return await query.ToListAsync(cancellationToken);
    }
}

public class UserAdminService : IUserAdminService
{
    private readonly MiniErpDbContext _dbContext;

    public UserAdminService(MiniErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.IsActive, u.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = AuthService.HashPassword(request.Password),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.Email = request.Email;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = AuthService.HashPassword(request.Password);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
        {
            return;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .Where(r => r.Id == id)
            .Select(r => new RoleDto(r.Id, r.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = new Role { Name = request.Name };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return role.Id;
    }

    public async Task UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FindAsync(new object[] { id }, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException("Role not found.");
        }

        role.Name = request.Name;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FindAsync(new object[] { id }, cancellationToken);
        if (role == null)
        {
            return;
        }

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => new UserRoleDto(ur.UserId, ur.RoleId, ur.Role.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserRoles.AnyAsync(
            ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (exists)
        {
            return;
        }

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .SingleOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole == null)
        {
            return;
        }

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

