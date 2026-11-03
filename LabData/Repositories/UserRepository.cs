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
            user => user.Email == email
        );
    }
}