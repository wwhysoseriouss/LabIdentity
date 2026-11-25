using LabData.Data;
using LabData.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LabData.Repositories;

public class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(
            user => user.Email == email);
    }

    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetPageAsync(
        int pageNumber,
        int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var skip = checked((pageNumber - 1) * pageSize);

        return await _dbSet
            .AsNoTracking()
            .OrderBy(user => user.NormalizedEmail)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }
}