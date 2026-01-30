using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using EvMarketplace.Infrastructure.Services;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Moq;
using static LanguageExt.Prelude;

namespace EvMarketplace.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<ISellerRepository> _sellerRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _sellerRepositoryMock = new Mock<ISellerRepository>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup default configuration values
        _configurationMock.Setup(c => c["Jwt:Secret"]).Returns("ThisIsATestSecretKeyForJWTThatIsLongEnough123456");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("test-audience");
        _configurationMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");

        _authService = new AuthService(_sellerRepositoryMock.Object, _configurationMock.Object);
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedString()
    {
        // Act
        var hashed = _authService.HashPassword("SecurePassword123");

        // Assert
        hashed.Should().NotBeNullOrEmpty();
        hashed.Should().NotBe("SecurePassword123", "Password should be hashed");
        hashed.Length.Should().BeGreaterThan(40, "Hash should be substantial");
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ReturnsDifferentHashes()
    {
        // Act
        var hash1 = _authService.HashPassword("SecurePassword123");
        var hash2 = _authService.HashPassword("SecurePassword123");

        // Assert
        hash1.Should().NotBe(hash2, "Each hash should use a unique salt");
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword123";
        var hashed = _authService.HashPassword(password);

        // Act
        var isValid = _authService.VerifyPassword(password, hashed);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "SecurePassword123";
        var wrongPassword = "WrongPassword456";
        var hashed = _authService.HashPassword(correctPassword);

        // Act
        var isValid = _authService.VerifyPassword(wrongPassword, hashed);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidHash_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword123";
        var invalidHash = "this-is-not-a-valid-hash";

        // Act
        var isValid = _authService.VerifyPassword(password, invalidHash);

        // Assert
        isValid.Should().BeFalse("Invalid hash should return false, not throw");
    }

    [Fact]
    public async Task RegisterAsync_ValidInputs_ReturnSuccessWithToken()
    {
        // Arrange
        var email = "newuser@example.com";
        var password = "SecurePassword123";
        var name = "John Doe";

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Option<Seller>.None);

        _sellerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Seller>()))
            .ReturnsAsync((Seller s) => Right<string, Seller>(s));

        // Act
        var result = await _authService.RegisterAsync(
            email,
            password,
            name,
            SellerType.Private);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: authResult =>
            {
                authResult.Token.Should().NotBeNullOrEmpty();
                authResult.RefreshToken.Should().NotBeNullOrEmpty();
                authResult.Seller.Email.Should().Be(email);
                authResult.Seller.Name.Should().Be(name);
                authResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );

        // Verify repository was called
        _sellerRepositoryMock.Verify(r => r.GetByEmailAsync(email), Times.Once);
        _sellerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Seller>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsError()
    {
        // Arrange
        var email = "existing@example.com";
        var existingSeller = CreateTestSeller(email);

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Some(existingSeller));

        // Act
        var result = await _authService.RegisterAsync(
            email,
            "SecurePassword123",
            "John Doe",
            SellerType.Private);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("already exists"));

        // Verify AddAsync was never called
        _sellerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Seller>()), Times.Never);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nouppercase123")]
    [InlineData("NOLOWERCASE123")]
    [InlineData("NoDigitsHere")]
    public async Task RegisterAsync_WeakPassword_ReturnsError(string weakPassword)
    {
        // Arrange
        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(Option<Seller>.None);

        // Act
        var result = await _authService.RegisterAsync(
            "newuser@example.com",
            weakPassword,
            "John Doe",
            SellerType.Private);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Password"));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var email = "user@example.com";
        var password = "SecurePassword123";
        var hashedPassword = _authService.HashPassword(password);
        var seller = CreateTestSeller(email, hashedPassword);

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Some(seller));

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: authResult =>
            {
                authResult.Token.Should().NotBeNullOrEmpty();
                authResult.RefreshToken.Should().NotBeNullOrEmpty();
                authResult.Seller.Email.Should().Be(email);
                authResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsError()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Option<Seller>.None);

        // Act
        var result = await _authService.LoginAsync(email, "AnyPassword123");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Invalid email or password"));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsError()
    {
        // Arrange
        var email = "user@example.com";
        var correctPassword = "SecurePassword123";
        var wrongPassword = "WrongPassword456";
        var hashedPassword = _authService.HashPassword(correctPassword);
        var seller = CreateTestSeller(email, hashedPassword);

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Some(seller));

        // Act
        var result = await _authService.LoginAsync(email, wrongPassword);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Invalid email or password"));
    }

    [Fact]
    public async Task LoginAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var email = "user@example.com";
        var password = "SecurePassword123";
        var hashedPassword = _authService.HashPassword(password);
        var seller = CreateTestSeller(email, hashedPassword);

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Some(seller));

        // Act
        var loginResult = await _authService.LoginAsync(email, password);

        // Assert - Extract token and validate it
        loginResult.IsRight.Should().BeTrue();
        var token = loginResult.Match(
            Right: authResult => authResult.Token,
            Left: _ => throw new Exception("Login failed")
        );

        var validateResult = _authService.ValidateToken(token);
        validateResult.IsRight.Should().BeTrue();
        validateResult.Match(
            Right: principal =>
            {
                var emailClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Email);
                emailClaim.Should().NotBeNull();
                emailClaim!.Value.Should().Be(email);
            },
            Left: error => throw new Exception($"Token validation failed: {error}")
        );
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsError()
    {
        // Arrange
        var invalidToken = "this.is.not.a.valid.jwt.token";

        // Act
        var result = _authService.ValidateToken(invalidToken);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("validation failed"));
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsError()
    {
        // Act
        var result = _authService.ValidateToken("");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_DealerWithCompanyName_Succeeds()
    {
        // Arrange
        var email = "dealer@example.com";
        var companyName = "Premium Motors Ltd";

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(Option<Seller>.None);

        _sellerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Seller>()))
            .ReturnsAsync((Seller s) => Right<string, Seller>(s));

        // Act
        var result = await _authService.RegisterAsync(
            email,
            "SecurePassword123",
            "Bob Smith",
            SellerType.Dealer,
            Some(companyName),
            Some("London, UK"));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: authResult =>
            {
                authResult.Seller.Type.Should().Be(SellerType.Dealer);
                authResult.Seller.CompanyName.IsSome.Should().BeTrue();
                authResult.Seller.CompanyName.Match(
                    name => name.Should().Be(companyName),
                    () => throw new Exception("Expected company name")
                );
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashed()
    {
        // Arrange
        var password = "SecurePassword123";
        Seller? capturedSeller = null;

        _sellerRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(Option<Seller>.None);

        _sellerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Seller>()))
            .ReturnsAsync((Seller s) =>
            {
                capturedSeller = s;
                return Right<string, Seller>(s);
            });

        // Act
        await _authService.RegisterAsync(
            "newuser@example.com",
            password,
            "John Doe",
            SellerType.Private);

        // Assert
        capturedSeller.Should().NotBeNull();
        capturedSeller!.PasswordHash.Should().NotBe(password, "Password should be hashed, not stored in plain text");
        _authService.VerifyPassword(password, capturedSeller.PasswordHash).Should().BeTrue("Hashed password should verify correctly");
    }

    // Helper method to create test sellers
    private Seller CreateTestSeller(string email, string? passwordHash = null)
    {
        var result = Seller.Create(
            Guid.NewGuid(),
            "Test User",
            SellerType.Private,
            email,
            "+44 7700 900123",
            passwordHash ?? "hashedpassword123",
            None,
            Some("London, UK"));

        return result.IfLeft(err => throw new Exception($"Failed to create test seller: {err}"));
    }
}
