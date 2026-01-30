using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Services;

/// <summary>
/// Authentication service for JWT token generation and validation
/// </summary>
public interface IAuthService
{
    Task<Either<string, AuthResult>> RegisterAsync(
        string email,
        string password,
        string name,
        SellerType type,
        Option<string> companyName = default,
        Option<string> location = default);

    Task<Either<string, AuthResult>> LoginAsync(string email, string password);
    Either<string, ClaimsPrincipal> ValidateToken(string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public record AuthResult(
    string Token,
    string RefreshToken,
    Seller Seller,
    DateTime ExpiresAt
);

public class AuthService : IAuthService
{
    private readonly ISellerRepository _sellerRepository;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    public AuthService(ISellerRepository sellerRepository, IConfiguration configuration)
    {
        _sellerRepository = sellerRepository;
        _configuration = configuration;

        _jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "ev-marketplace";
        _jwtAudience = configuration["Jwt:Audience"] ?? "ev-marketplace-api";
        _jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");
    }

    public async Task<Either<string, AuthResult>> RegisterAsync(
        string email,
        string password,
        string name,
        SellerType type,
        Option<string> companyName = default,
        Option<string> location = default)
    {
        try
        {
            // Check if seller already exists
            var existingSeller = await _sellerRepository.GetByEmailAsync(email);

            if (existingSeller.IsSome)
                return "A seller with this email already exists";

            // Validate password strength
            var passwordValidation = ValidatePassword(password);
            if (passwordValidation.IsLeft)
                return passwordValidation.Match(l => l, r => "");

            // Hash password
            var hashedPassword = HashPassword(password);

            // Create seller
            var sellerResult = Seller.Create(
                Guid.NewGuid(),
                name,
                type,
                email,
                "", // Phone number can be added later
                hashedPassword,
                companyName,
                location
            );

            return await sellerResult.Match(
                Right: async seller =>
                {
                    var addResult = await _sellerRepository.AddAsync(seller);

                    return await addResult.Match(
                        Right: async createdSeller =>
                        {
                            // Generate tokens
                            var token = GenerateJwtToken(createdSeller);
                            var refreshToken = GenerateRefreshToken();
                            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

                            return new AuthResult(token, refreshToken, createdSeller, expiresAt);
                        },
                        Left: error => Task.FromResult<Either<string, AuthResult>>(error)
                    );
                },
                Left: error => Task.FromResult<Either<string, AuthResult>>(error)
            );
        }
        catch (Exception ex)
        {
            return $"Registration failed: {ex.Message}";
        }
    }

    public async Task<Either<string, AuthResult>> LoginAsync(string email, string password)
    {
        try
        {
            // Find seller by email
            var sellerOption = await _sellerRepository.GetByEmailAsync(email);

            if (sellerOption.IsNone)
                return "Invalid email or password";

            // Safe: seller exists (verified by IsNone check above)
            var seller = sellerOption.IfNone(() => default(Seller)!);

            // Verify password
            if (!VerifyPassword(password, seller.PasswordHash))
                return "Invalid email or password";

            // Generate tokens
            var token = GenerateJwtToken(seller);
            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

            return new AuthResult(token, refreshToken, seller, expiresAt);
        }
        catch (Exception ex)
        {
            return $"Login failed: {ex.Message}";
        }
    }

    public Either<string, ClaimsPrincipal> ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return "Invalid token";
            }

            return principal;
        }
        catch (Exception ex)
        {
            return $"Token validation failed: {ex.Message}";
        }
    }

    public string HashPassword(string password)
    {
        // Use PBKDF2 with SHA256
        const int saltSize = 16;
        const int hashSize = 32;
        const int iterations = 100000;

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[saltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(hashSize);

        // Combine salt and hash
        var hashBytes = new byte[saltSize + hashSize];
        Array.Copy(salt, 0, hashBytes, 0, saltSize);
        Array.Copy(hash, 0, hashBytes, saltSize, hashSize);

        // Convert to base64
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            const int saltSize = 16;
            const int hashSize = 32;
            const int iterations = 100000;

            // Convert from base64
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract salt
            var salt = new byte[saltSize];
            Array.Copy(hashBytes, 0, salt, 0, saltSize);

            // Compute hash on password
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(hashSize);

            // Compare hashes
            for (var i = 0; i < hashSize; i++)
            {
                if (hashBytes[i + saltSize] != hash[i])
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateJwtToken(Seller seller)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, seller.Id.ToString()),
            new(ClaimTypes.Email, seller.Email),
            new(ClaimTypes.Name, seller.Name),
            new("seller_type", seller.Type.ToString()),
            new("subscription_tier", seller.CurrentSubscriptionTier.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private Either<string, Unit> ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required";

        if (password.Length < 8)
            return "Password must be at least 8 characters long";

        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter";

        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one number";

        return unit;
    }
}
