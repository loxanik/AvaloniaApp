using System.Threading.Tasks;

namespace Shop.Interfaces;

public interface IAuthService
{
    Task<bool> UserLoginAsync(string login, 
        string password);

    Task<bool> ClientRegisterAsync(string login,
        string password,
        string firstName,
        string lastName,
        string? patronymic,
        string phoneNumber,
        string email);
}