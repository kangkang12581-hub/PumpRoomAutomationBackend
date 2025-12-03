using BCrypt.Net;

namespace PumpRoomAutomationBackend.Services.Security;

/// <summary>
/// 密码服务
/// Password Service
/// </summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public class PasswordService : IPasswordService
{
    /// <summary>
    /// 生成密码哈希
    /// Generate password hash
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
    }
    
    /// <summary>
    /// 验证密码
    /// Verify password
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}

