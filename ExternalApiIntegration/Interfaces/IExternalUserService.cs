using ExternalApiIntegration.Models;

namespace ExternalApiIntegration.Interfaces
{
    public interface IExternalUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(int userId);
    }
}
