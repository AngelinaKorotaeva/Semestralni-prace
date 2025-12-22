using SkolniJidelna.Data;
using SkolniJidelna.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkolniJidelna.Services;
public class StravnikRepository : IStravnikRepository
{
    private readonly AppDbContext _db;
    public StravnikRepository(AppDbContext db) => _db = db;

    // Vrátí všechny strávníky (bez trackování) pro výpis/listy.
    public async Task<List<Stravnik>> GetAllAsync() =>
        await _db.Stravnik!.AsNoTracking().ToListAsync();

    // Najde strávníka podle primárního klíče IdStravnik.
    public async Task<Stravnik?> GetByIdAsync(int id) =>
        await _db.Stravnik!.FindAsync(id);

    // Přidá nového strávníka a uloží změny do DB.
    public async Task AddAsync(Stravnik entity)
    {
        await _db.Stravnik!.AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    // Aktualizuje existujícího strávníka a uloží změny do DB.
    public async Task UpdateAsync(Stravnik entity)
    {
        _db.Stravnik!.Update(entity);
        await _db.SaveChangesAsync();
    }

    // Smaže strávníka podle Id (pokud existuje) a uloží změny do DB.
    public async Task DeleteAsync(int id)
    {
        var e = await _db.Stravnik!.FindAsync(id);
        if (e != null) { _db.Stravnik.Remove(e); await _db.SaveChangesAsync(); }
    }
}