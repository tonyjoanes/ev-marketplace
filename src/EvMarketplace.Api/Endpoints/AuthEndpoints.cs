using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace EvMarketplace.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithOpenApi()
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi()
            .AllowAnonymous();

        group.MapPost("/validate", ValidateToken)
            .WithName("ValidateToken")
            .WithOpenApi();
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        IAuthService authService)
    {
        var result = await authService.RegisterAsync(
            request.Email,
            request.Password,
            request.Name,
            request.Type,
            request.CompanyName,
            request.Location
        );

        return result.Match(
            Right: authResult => Results.Ok(new
            {
                token = authResult.Token,
                refreshToken = authResult.RefreshToken,
                expiresAt = authResult.ExpiresAt,
                seller = new
                {
                    id = authResult.Seller.Id,
                    name = authResult.Seller.Name,
                    email = authResult.Seller.Email,
                    type = authResult.Seller.Type.ToString(),
                    subscriptionTier = authResult.Seller.CurrentSubscriptionTier.ToString(),
                    isVerified = authResult.Seller.IsVerified
                }
            }),
            Left: error => Results.BadRequest(new { error })
        );
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IAuthService authService)
    {
        var result = await authService.LoginAsync(request.Email, request.Password);

        return result.Match(
            Right: authResult => Results.Ok(new
            {
                token = authResult.Token,
                refreshToken = authResult.RefreshToken,
                expiresAt = authResult.ExpiresAt,
                seller = new
                {
                    id = authResult.Seller.Id,
                    name = authResult.Seller.Name,
                    email = authResult.Seller.Email,
                    type = authResult.Seller.Type.ToString(),
                    subscriptionTier = authResult.Seller.CurrentSubscriptionTier.ToString(),
                    isVerified = authResult.Seller.IsVerified,
                    currentListingCount = authResult.Seller.CurrentListingCount,
                    canAddListing = authResult.Seller.CanAddListing(),
                    remainingListings = authResult.Seller.GetRemainingListings()
                }
            }),
            Left: error => Results.Unauthorized()
        );
    }

    private static IResult ValidateToken(
        [FromHeader(Name = "Authorization")] string authorization,
        IAuthService authService)
    {
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            return Results.Unauthorized();

        var token = authorization.Substring("Bearer ".Length).Trim();

        var result = authService.ValidateToken(token);

        return result.Match(
            Right: principal => Results.Ok(new
            {
                valid = true,
                userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            }),
            Left: error => Results.Unauthorized()
        );
    }
}

// Request DTOs
public record RegisterRequest(
    string Email,
    string Password,
    string Name,
    SellerType Type,
    Option<string> CompanyName,
    Option<string> Location
);

public record LoginRequest(
    string Email,
    string Password
);
