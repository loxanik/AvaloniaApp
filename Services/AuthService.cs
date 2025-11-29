using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Interfaces;
using Shop.Models;
using Shop.Utils;

namespace Shop.Services;

public class AuthService(ShopContext shopContext, IUserContext userContext) : IAuthService
{
    private readonly ShopContext _shopContext = shopContext;
    private readonly IUserContext _userContext = userContext;

    public async Task<bool> UserLoginAsync(string login, string password)
    {
        try
        {
            bool isSuccess = await _shopContext.Users.AnyAsync(u => u.Login == login
                                                                && u.Password == password);
            if (isSuccess)
                _userContext.CurrentUser = await _shopContext.Users
                    .Include(u => u.PersonalInfos)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Login == login);
            
            return isSuccess;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Login error: {login}");
            return false;
        }
    }

    public async Task<bool> ClientRegisterAsync(string login, 
        string password, 
        string name, 
        string surname, 
        string? patronymic, 
        string phoneNumber, 
        string email)
    {
        try
        {
            bool isRepeatedUser = await _shopContext.Users.AnyAsync(u => u.Login == login);
            
            if (isRepeatedUser)
                return false;
            
            var user = new User
            {
                Login = login,
                Password = password,
                RoleId = 3
            };
            
            _shopContext.Users.Add(user);
            await _shopContext.SaveChangesAsync();

            var personal = new PersonalInfo
            {
                UserId = user.Id,
                Email = email,
                Name = name,
                Surname = surname,
                Patronymic = patronymic,
                PhoneNumber = phoneNumber,
            };
            
            _shopContext.PersonalInfos.Add(personal);
            await _shopContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Register error: {login}");
            return false;
        }
    }
    
}