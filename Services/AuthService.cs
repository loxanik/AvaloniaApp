using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

// Сервис отвечающий за авторизацию и регистрацию пользователей
public class AuthService(ShopContext shopContext, IUserContext userContext) : IAuthService
{
    // Зависимости
    private readonly ShopContext _shopContext = shopContext;
    private readonly IUserContext _userContext = userContext;
    
    // Метод входа
    public async Task<bool> UserLoginAsync(string login, string password)
    {
        try
        {
            // Поиск совпадений в БД
            bool isSuccess = await _shopContext.Users.AnyAsync(u => u.Login == login
                                                                && u.Password == password);
            // Если нашло, то сохраняем данные о клиенте в память программы
            if (isSuccess)
            {
                var user = await _shopContext.Users
                    .Include(u => u.PersonalInfos)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Login == login);
                
                // Проверка на уволенного сотрудника
                if (user.IsDismissed)
                {
                    return false; // Уволенный сотрудник не может войти
                }
                
                _userContext.CurrentUser = user;
            }
            
            return isSuccess;
        }
        catch (Exception ex)
        {
            // Логирование ошибки
            AppLogger.LogError(ex, $"Login error: {login}");
            return false;
        }
    }

    // Метод регистрации клиента
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
            // Проверка на занятость логина
            bool isRepeatedUser = await _shopContext.Users.AnyAsync(u => u.Login == login);
            
            if (isRepeatedUser)
                return false;
            
            // Создание нового пользователя с ролью клиент
            var user = new User
            {
                Login = login,
                Password = password,
                RoleId = 3
            };
            
            // Сохранение в таблицу "User"
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
            
            // Сохранение в таблицу "PersonalInfo"
            _shopContext.PersonalInfos.Add(personal);
            await _shopContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception e)
        {
            // Логирование ошибки
            AppLogger.LogError(e, $"Register error: {login}");
            return false;
        }
    }
    
    // Метод проверки статуса сотрудника
    public async Task<bool> IsUserDismissedAsync(string login)
    {
        try
        {
            var user = await _shopContext.Users
                .FirstOrDefaultAsync(u => u.Login == login);
            
            return user?.IsDismissed ?? false;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Check dismissed status error: {login}");
            return false;
        }
    }
    
}