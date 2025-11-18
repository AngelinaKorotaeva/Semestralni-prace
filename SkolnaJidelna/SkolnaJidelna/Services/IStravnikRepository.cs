using SkolniJidelna.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkolniJidelna.Services;
public interface IStravnikRepository
{
    Task<List<Stravnik>> GetAllAsync();
    Task<Stravnik?> GetByIdAsync(int id);
    Task AddAsync(Stravnik entity);
    Task UpdateAsync(Stravnik entity);
    Task DeleteAsync(int id);
}