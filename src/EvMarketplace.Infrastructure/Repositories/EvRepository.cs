using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Repositories;

/// <summary>
/// Functional repository for Electric Vehicles.
/// Returns Either for operations that can fail, Option for nullable results.
/// All operations are async and composable.
/// </summary>
public interface IEvRepository
{
    Task<Either<string, Seq<ElectricVehicle>>> GetAllAsync();
    Task<Option<ElectricVehicle>> GetByIdAsync(Guid id);
    Task<Either<string, Seq<ElectricVehicle>>> SearchAsync(EvSearchCriteria criteria);
    Task<Either<string, ElectricVehicle>> AddAsync(ElectricVehicle ev);
    Task<Either<string, Unit>> UpdateAsync(ElectricVehicle ev);
    Task<Either<string, Unit>> DeleteAsync(Guid id);
}

public class EvRepository : IEvRepository
{
    private readonly EvMarketplaceDbContext _context;

    public EvRepository(EvMarketplaceDbContext context)
    {
        _context = context;
    }

    public async Task<Either<string, Seq<ElectricVehicle>>> GetAllAsync()
    {
        try
        {
            var evs = await _context.ElectricVehicles.ToListAsync();
            return Seq(evs);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve electric vehicles: {ex.Message}";
        }
    }

    public async Task<Option<ElectricVehicle>> GetByIdAsync(Guid id)
    {
        try
        {
            var ev = await _context.ElectricVehicles.FindAsync(id);
            return Optional(ev);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Either<string, Seq<ElectricVehicle>>> SearchAsync(EvSearchCriteria criteria)
    {
        try
        {
            var query = _context.ElectricVehicles.AsQueryable();

            query = criteria.Make.Match(
                Some: make => query.Where(ev => ev.Make.ToLower().Contains(make.ToLower())),
                None: () => query);

            query = criteria.BodyType.Match(
                Some: bodyType => query.Where(ev => ev.BodyType.ToLower() == bodyType.ToLower()),
                None: () => query);

            query = criteria.MinPrice.Match(
                Some: minPrice => query.Where(ev => ev.PriceGbp >= minPrice),
                None: () => query);

            query = criteria.MaxPrice.Match(
                Some: maxPrice => query.Where(ev => ev.PriceGbp <= maxPrice),
                None: () => query);

            query = criteria.MinRange.Match(
                Some: minRange => query.Where(ev => ev.WltpRangeKm >= minRange),
                None: () => query);

            var results = await query.ToListAsync();
            return Seq(results);
        }
        catch (Exception ex)
        {
            return $"Search failed: {ex.Message}";
        }
    }

    public async Task<Either<string, ElectricVehicle>> AddAsync(ElectricVehicle ev)
    {
        try
        {
            _context.ElectricVehicles.Add(ev);
            await _context.SaveChangesAsync();
            return ev;
        }
        catch (Exception ex)
        {
            return $"Failed to add electric vehicle: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> UpdateAsync(ElectricVehicle ev)
    {
        try
        {
            _context.ElectricVehicles.Update(ev);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to update electric vehicle: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> DeleteAsync(Guid id)
    {
        try
        {
            var ev = await _context.ElectricVehicles.FindAsync(id);
            if (ev is null)
                return "Electric vehicle not found";

            _context.ElectricVehicles.Remove(ev);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to delete electric vehicle: {ex.Message}";
        }
    }
}

/// <summary>
/// Search criteria using Option types for optional parameters
/// </summary>
public sealed record EvSearchCriteria(
    Option<string> Make,
    Option<string> BodyType,
    Option<decimal> MinPrice,
    Option<decimal> MaxPrice,
    Option<decimal> MinRange
);
