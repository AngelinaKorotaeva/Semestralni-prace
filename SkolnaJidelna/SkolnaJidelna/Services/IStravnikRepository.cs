using SkolniJidelna.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkolniJidelna.Services;
public interface IStravnikRepository
{
    // Vrátí všechny strávníky (asynchronně) pro výpis.
    Task<List<Stravnik>> GetAllAsync();
    // Najde strávníka podle primárního klíče IdStravnik.
    Task<Stravnik?> GetByIdAsync(int id);
    // Přidá nového strávníka do databáze.
    Task AddAsync(Stravnik entity);
    // Aktualizuje existujícího strávníka v databázi.
    Task UpdateAsync(Stravnik entity);
    // Smaže strávníka podle IdStravnik.
    Task DeleteAsync(int id);
}