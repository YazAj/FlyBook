using FlyBook.Models;
using Microsoft.AspNetCore.Identity;

namespace FlyBook.Services
{
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<User> Hasher = new();

        public static string HashPassword(string password)
        {
            return Hasher.HashPassword(new User(), password);
        }

        public static PasswordVerificationResult VerifyPassword(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                return PasswordVerificationResult.Failed;
            }

            try
            {
                var result = Hasher.VerifyHashedPassword(user, user.Password, password);

                if (result != PasswordVerificationResult.Failed)
                {
                    return result;
                }
            }
            catch (FormatException)
            {
                // Existing academic seed/user data may still contain plain text passwords.
            }

            return user.Password == password
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }

        public static bool IsHashedPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                var decoded = Convert.FromBase64String(password);

                return decoded.Length > 0 && decoded[0] == 0x01;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
