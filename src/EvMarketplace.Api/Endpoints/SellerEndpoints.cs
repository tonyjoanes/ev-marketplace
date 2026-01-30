using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using static LanguageExt.Prelude;

namespace EvMarketplace.Api.Endpoints;

public static class SellerEndpoints
{
    public static void MapSellerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sellers")
            .WithTags("Sellers")
            .WithOpenApi();

        group.MapGet("/", GetAllSellers)
            .WithName("GetAllSellers")
            .Produces<List<Seller>>();

        group.MapGet("/{id:guid}", GetSellerById)
            .WithName("GetSellerById")
            .Produces<Seller>()
            .Produces(404);

        group.MapGet("/email/{email}", GetSellerByEmail)
            .WithName("GetSellerByEmail")
            .Produces<Seller>()
            .Produces(404);

        group.MapPost("/", CreateSeller)
            .WithName("CreateSeller")
            .Produces<Seller>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateSeller)
            .WithName("UpdateSeller")
            .Produces(204)
            .Produces(400);

        group.MapDelete("/{id:guid}", DeleteSeller)
            .WithName("DeleteSeller")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllSellers(ISellerRepository repository)
    {
        var result = await repository.GetAllAsync();
        return result.Match(
            Right: sellers => Results.Ok(sellers.ToList()),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> GetSellerById(Guid id, ISellerRepository repository)
    {
        var result = await repository.GetByIdAsync(id);
        return result.Match(
            Some: seller => Results.Ok(seller),
            None: () => Results.NotFound()
        );
    }

    private static async Task<IResult> GetSellerByEmail(string email, ISellerRepository repository)
    {
        var result = await repository.GetByEmailAsync(email);
        return result.Match(
            Some: seller => Results.Ok(seller),
            None: () => Results.NotFound()
        );
    }

    private static async Task<IResult> CreateSeller(
        [FromBody] CreateSellerRequest request,
        ISellerRepository repository)
    {
        var sellerResult = Seller.Create(
            Guid.NewGuid(),
            request.Name,
            (SellerType)request.Type,
            request.Email,
            request.PhoneNumber,
            request.CompanyName.ToOption(),
            request.Location.ToOption()
        );

        return await sellerResult.Match(
            Right: async seller =>
            {
                var addResult = await repository.AddAsync(seller);
                return addResult.Match(
                    Right: created => Results.Created($"/api/sellers/{created.Id}", created),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(error))
        );
    }

    private static async Task<IResult> UpdateSeller(
        Guid id,
        [FromBody] UpdateSellerRequest request,
        ISellerRepository repository)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing.IsNone)
            return Results.NotFound();

        // Safe: seller exists (verified by IsNone check above)
        var existingSeller = existing.IfNone(() => default(Seller)!);
        var seller = existingSeller with
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            CompanyName = request.CompanyName.ToOption(),
            Location = request.Location.ToOption()
        };

        var result = await repository.UpdateAsync(seller);
        return result.Match(
            Right: _ => Results.NoContent(),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> DeleteSeller(Guid id, ISellerRepository repository)
    {
        var result = await repository.DeleteAsync(id);
        return result.Match(
            Right: _ => Results.NoContent(),
            Left: error => Results.NotFound(error)
        );
    }
}

// Request DTOs
public sealed record CreateSellerRequest(
    string Name,
    int Type,
    string Email,
    string PhoneNumber,
    string? CompanyName,
    string? Location
);

public sealed record UpdateSellerRequest(
    string Name,
    string PhoneNumber,
    string? CompanyName,
    string? Location
);
