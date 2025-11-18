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
    public async Task<List<Stravnik>> GetAllAsync() =>
        await _db.Stravnik!.AsNoTracking().ToListAsync();
    public async Task<Stravnik?> GetByIdAsync(int id) =>
        await _db.Stravnik!.FindAsync(id);
    public async Task AddAsync(Stravnik entity)
    {
        await _db.Stravnik!.AddAsync(entity);
        await _db.SaveChangesAsync();
    }
    public async Task UpdateAsync(Stravnik entity)
    {
        _db.Stravnik!.Update(entity);
        await _db.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id)
    {
        var e = await _db.Stravnik!.FindAsync(id);
        if (e != null) { _db.Stravnik.Remove(e); await _db.SaveChangesAsync(); }
    }
}