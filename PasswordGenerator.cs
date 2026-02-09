using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

public class PasswordGenerator
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations))
        {
            var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
            var salt = Convert.ToBase64String(algorithm.Salt);
            return string.Format("{0}.{1}.{2}", Iterations, salt, key);
        }
    }

    public static void Main()
    {
        string password = "Password@2026!"; // Harder password to avoid sequential char checks
        string hash = HashPassword(password);
        
        Console.WriteLine("-- Generated Hash for: " + password);
        Console.WriteLine("UPDATE Users SET PasswordHash = '" + hash + "', RequirePasswordChange = 0 WHERE UserName = 'superadmin';");
        Console.WriteLine("UPDATE Users SET PasswordHash = '" + hash + "', RequirePasswordChange = 0 WHERE UserName = 'sam';");
        Console.WriteLine("UPDATE Users SET PasswordHash = '" + hash + "', RequirePasswordChange = 0 WHERE UserName = 'hr';");
    }
}
