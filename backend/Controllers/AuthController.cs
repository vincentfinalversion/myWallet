using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("All fields are required.");
            }

            if (!request.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only Gmail addresses are accepted.");
            }

            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email);
            bool usernameTaken = await _db.Users.AnyAsync(u => u.Username == request.Username);
            if (emailTaken || usernameTaken)
            {
                return BadRequest("Username or email is already registered.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            string otpCode = new Random().Next(100000, 999999).ToString();

            var existingOtp = await _db.EmailOtps.FirstOrDefaultAsync(o => o.Email == request.Email);
            if (existingOtp != null)
            {
                _db.EmailOtps.Remove(existingOtp);
            }

            var otp = new EmailOtp
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = passwordHash,
                Code = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _db.EmailOtps.Add(otp);
            await _db.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(request.Email, otpCode);

            return Ok(new RegisterResponse { Message = "Verification code sent to email." });
        }
    }
}