using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using eSKHub.API.Data;
using eSKHub.API.Models;

namespace eSKHub.API.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> AuthenticateAsync(LoginRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
        {
            try
            {
                // Find user by username
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username."
                    };
                }

                // Verify password
                var hashedPassword = HashPassword(request.Password);
                if (user.Password != hashedPassword)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Incorrect password."
                    };
                }

                // Check if user is Council member
                if (user.Role != "Council")
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Only Council members can access the mobile app."
                    };
                }

                // Return success with user data
                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email ?? string.Empty,
                        Role = user.Role ?? "Council",
                        Fullname = string.IsNullOrWhiteSpace(user.Fullname) ? user.Username : user.Fullname,
                        Position = user.Position,
                        Area = user.Area,
                        Image = user.Image
                    }
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}