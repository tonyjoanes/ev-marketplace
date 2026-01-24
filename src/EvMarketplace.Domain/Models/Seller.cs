using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Represents a seller (dealer or private individual)
/// </summary>
public sealed record Seller
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required SellerType Type { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required Option<string> CompanyName { get; init; }
    public required Option<string> Location { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsVerified { get; init; }

    public static Either<string, Seller> Create(
        Guid id,
        string name,
        SellerType type,
        string email,
        string phoneNumber,
        Option<string> companyName = default,
        Option<string> location = default,
        DateTime? createdAt = null,
        bool isVerified = false)
    {
        var validations = Seq(
            ValidateName(name),
            ValidateEmail(email),
            ValidatePhoneNumber(phoneNumber),
            ValidateCompanyName(type, companyName)
        );

        var errors = validations.Lefts().ToSeq();

        return errors.IsEmpty
            ? new Seller
            {
                Id = id,
                Name = name,
                Type = type,
                Email = email,
                PhoneNumber = phoneNumber,
                CompanyName = companyName,
                Location = location,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                IsVerified = isVerified
            }
            : string.Join(", ", errors);
    }

    private static Either<string, Unit> ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? "Name cannot be empty"
            : unit;

    private static Either<string, Unit> ValidateEmail(string email) =>
        string.IsNullOrWhiteSpace(email) || !email.Contains("@")
            ? "Valid email is required"
            : unit;

    private static Either<string, Unit> ValidatePhoneNumber(string phone) =>
        string.IsNullOrWhiteSpace(phone)
            ? "Phone number is required"
            : unit;

    private static Either<string, Unit> ValidateCompanyName(
        SellerType type,
        Option<string> companyName) =>
        type == SellerType.Dealer && companyName.IsNone
            ? "Company name is required for dealers"
            : unit;
}

public enum SellerType
{
    Private,
    Dealer
}
