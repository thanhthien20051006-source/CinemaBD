namespace CinemaBD.Infrastructure.Security;

public class Md5PasswordHasher
{
    public string Hash(string password)
    {
        if (password == null)
            password = string.Empty;

        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    public bool Verify(string plainTextPassword, string storedHash)
    {
        if (storedHash == null)
            return false;

        var hashedInput = Hash(plainTextPassword);
        if (string.Equals(storedHash, hashedInput, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(storedHash, plainTextPassword, StringComparison.Ordinal);
    }
}
