using LabData.Data;

namespace LabData.Repositories.Interfaces;

public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email);

    Task<int> CountAsync();

    Task<IReadOnlyList<ApplicationUser>> GetPageAsync(
        int pageNumber,
        int pageSize);
}