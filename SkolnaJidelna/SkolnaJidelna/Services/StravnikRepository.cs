using SkolniJidelna.Data;
using SkolniJidelna.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkolniJidelna.Services;
/// <summary>
/// Implementace repozitáře strávníků nad EF Core `AppDbContext`.
/// Centralizuje přístup k tabulce `STRAVNICI` a skrývá detaily EF v aplikační vrstvě.
/// </summary>
public class StravnikRepository : IStravnikRepository
{
    private readonly AppDbContext _db;
    public StravnikRepository(AppDbContext db) => _db = db;

    /// <summary>
    /// Vrátí všechny strávníky (bez trackování) pro potřeby výpisů/listů.
    /// </summary>
    public async Task<List<Stravnik>> GetAllAsync() =>
        await _db.Stravnik!.AsNoTracking().ToListAsync();

    /// <summary>
    /// Najde strávníka podle primárního klíče `IdStravnik`.
    /// </summary>
    public async Task<Stravnik?> GetByIdAsync(int id) =>
        await _db.Stravnik!.FindAsync(id);

    /// <summary>
    /// Přidá nového strávníka a uloží změny do DB.
    /// </summary>
    public async Task AddAsync(Stravnik entity)
    {
        await _db.Stravnik!.AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Aktualizuje existujícího strávníka a uloží změny do DB.
    /// </summary>
    public async Task UpdateAsync(Stravnik entity)
    {
        _db.Stravnik!.Update(entity);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Smaže strávníka podle Id (pokud existuje) a uloží změny do DB.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var e = await _db.Stravnik!.FindAsync(id);
        if (e != null) { _db.Stravnik.Remove(e); await _db.SaveChangesAsync(); }
    }
}