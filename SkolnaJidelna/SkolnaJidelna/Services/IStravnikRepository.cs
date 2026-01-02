using SkolniJidelna.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkolniJidelna.Services;
/// <summary>
/// Rozhraní repozitáře strávníků – definuje CRUD operace nad datovým úložištěm.
/// Používá se ve ViewModelech přes DI pro testovatelnost (možno mockovat).
/// </summary>
public interface IStravnikRepository
{
    /// <summary>
    /// Vrátí všechny strávníky (asynchronně) pro výpis.
    /// </summary>
    Task<List<Stravnik>> GetAllAsync();
    /// <summary>
    /// Najde strávníka podle primárního klíče `IdStravnik`.
    /// </summary>
    Task<Stravnik?> GetByIdAsync(int id);
    /// <summary>
    /// Přidá nového strávníka do databáze.
    /// </summary>
    Task AddAsync(Stravnik entity);
    /// <summary>
    /// Aktualizuje existujícího strávníka v databázi.
    /// </summary>
    Task UpdateAsync(Stravnik entity);
    /// <summary>
    /// Smaže strávníka podle `IdStravnik`.
    /// </summary>
    Task DeleteAsync(int id);
}