using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Repositories;

public interface ISellerRepository
{
    Task<Either<string, Seq<Seller>>> GetAllAsync();
    Task<Option<Seller>> GetByIdAsync(Guid id);
    Task<Option<Seller>> GetByEmailAsync(string email);
    Task<Either<string, Seller>> AddAsync(Seller seller);
    Task<Either<string, Unit>> UpdateAsync(Seller seller);
    Task<Either<string, Unit>> DeleteAsync(Guid id);
}

public class SellerRepository : ISellerRepository
{
    private readonly EvMarketplaceDbContext _context;

    public SellerRepository(EvMarketplaceDbContext context)
    {
        _context = context;
    }

    public async Task<Either<string, Seq<Seller>>> GetAllAsync()
    {
        try
        {
            var sellers = await _context.Sellers.ToListAsync();
            return Seq(sellers);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve sellers: {ex.Message}";
        }
    }

    public async Task<Option<Seller>> GetByIdAsync(Guid id)
    {
        try
        {
            var seller = await _context.Sellers.FindAsync(id);
            return Optional(seller);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<Seller>> GetByEmailAsync(string email)
    {
        try
        {
            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
            return Optional(seller);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Either<string, Seller>> AddAsync(Seller seller)
    {
        try
        {
            // Check for duplicate email
            var existing = await GetByEmailAsync(seller.Email);
            if (existing.IsSome)
                return "A seller with this email already exists";

            _context.Sellers.Add(seller);
            await _context.SaveChangesAsync();
            return seller;
        }
        catch (Exception ex)
        {
            return $"Failed to add seller: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> UpdateAsync(Seller seller)
    {
        try
        {
            _context.Sellers.Update(seller);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to update seller: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> DeleteAsync(Guid id)
    {
        try
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller is null)
                return "Seller not found";

            _context.Sellers.Remove(seller);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to delete seller: {ex.Message}";
        }
    }
}
