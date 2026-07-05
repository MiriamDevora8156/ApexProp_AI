using System.Security.Cryptography;
using System.Text;

namespace ApexProp.Infrastructure.Services;

/// <summary>
/// PasswordService - השמרה בטוחה של סיסמאות
/// משתמשים ב-PBKDF2 + SHA256 (כמו bcrypt אבל יותר פשוט)
/// </summary>
public class PasswordService
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 20; // 160 bits
    private const int Iterations = 10000;

    /// <summary>
    /// Hash סיסמה
    /// </summary>
    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, Iterations, HashAlgorithmName.SHA256);

        byte[] hash = pbkdf2.GetBytes(HashSize);
        byte[] hashWithSalt = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashWithSalt, 0, SaltSize);
        Array.Copy(hash, 0, hashWithSalt, SaltSize, HashSize);

        return Convert.ToBase64String(hashWithSalt);
    }

    /// <summary>
    /// בדוק סיסמה
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            byte[] hashWithSalt = Convert.FromBase64String(hash);
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashWithSalt, 0, salt, 0, SaltSize);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash2 = pbkdf2.GetBytes(HashSize);

                for (int i = 0; i < HashSize; i++)
                {
                    if (hashWithSalt[i + SaltSize] != hash2[i])
                        return false;
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}