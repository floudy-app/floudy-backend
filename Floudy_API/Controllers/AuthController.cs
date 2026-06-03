using Floudy.API.Services;
using Floudy.API.Storage.Entities;
using Floudy.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Floudy.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(UserService user_service, LogService log_service, TokenService token_service, EmailService email_service, IConfiguration configuration) : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = user_service.GetByUsernameOrEmail(request.Username);
            if (user == null || user.Password != request.Password) return Unauthorized("Invalid username or password.");
            if (user.IsBlocked) return StatusCode(403, "Your account is blocked.");

            var token = token_service.GenerateToken(user);

            log_service.LogAction(user.ID.ToString(), user.Username, user.Role.Name, "login", "User logged into the application");

            return Ok(new
            {
                id = user.ID.ToString(),
                username = user.Username,
                role = user.Role.Name,
                token = token.Token
            });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (!user_service.ValidateRegistration(request.Username, request.Email, out var errorMessage))
            {
                return BadRequest(errorMessage);
            }

            var user = new UserEntity
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                RoleId = 2,
                IsBlocked = false
            };

            user_service.RegisterUser(user);
            return Ok();
        }

        [HttpGet("check-username")]
        public IActionResult CheckUsername([FromQuery] string username)
        {
            return Ok(new { exists = user_service.UsernameExists(username) });
        }

        [HttpPost("recovery/check")]
        public IActionResult CheckRecovery([FromBody] RecoveryCheckRequest request)
        {
            if (!user_service.ValidateRecoveryCheck(request.UsernameOrEmail, out var errorMessage, out var user)) return BadRequest(errorMessage);
            return Ok(new { username = user!.Username, email = user.Email });
        }

        [HttpPost("recovery/send")]
        public IActionResult SendRecovery([FromBody] RecoverySendRequest request)
        {
            if (!user_service.ValidateRecoveryCheck(request.UsernameOrEmail, out var errorMessage, out var user)) return StatusCode(422, errorMessage);

            try
            {
                var resetToken = token_service.GenerateResetToken(user!.ID);
                var origin = Request.Headers.Origin.ToString();

                if (string.IsNullOrEmpty(origin)) origin = Request.Headers.Referer.ToString();
                if (string.IsNullOrEmpty(origin))
                {
                    var configuredOrigin = configuration["FrontendSettings:BaseUrl"];
                    if (!string.IsNullOrWhiteSpace(configuredOrigin)) origin = configuredOrigin;
                }

                origin = origin.TrimEnd('/');

                var resetUrl = $"{origin}/recovery?reset={resetToken}";

                email_service.SendRecoveryEmail(user.Email, user.Username, resetUrl);
                log_service.LogAction(user.ID.ToString(), user.Username, user.Role.Name, "password_recovery", $"Sent password recovery email with token to {user.Email}");
                
                return Ok(new { message = $"A recovery email has been sent to {user.Email}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send email: {ex.Message}");
            }
        }

        [HttpGet("recovery/validate")]
        public IActionResult ValidateRecoveryToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Token is required.");

            var status = token_service.ValidateResetToken(token, out var resetToken);
            if (status == -1)
            {
                return Unauthorized("Invalid reset token.");
            }
            if (status == 0)
            {
                return StatusCode(410, "Your reset token has expired.");
            }

            return Ok(new { userId = resetToken!.UserId });
        }

        [HttpPost("recovery/reset")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!user_service.ValidateResetPasswordInputs(request.Token, request.Password, out var errorMessage))
            {
                return BadRequest(errorMessage);
            }

            var status = token_service.ValidateResetToken(request.Token, out var resetToken);
            if (status == -1)
            {
                return Unauthorized("Invalid reset token.");
            }
            if (status == 0)
            {
                token_service.ConsumeResetToken(request.Token);
                return StatusCode(410, "Your password reset session has expired.");
            }

            var user = user_service.GetById(resetToken!.UserId);
            if (user == null)
            {
                token_service.ConsumeResetToken(request.Token);
                return NotFound("User not found.");
            }

            user.Password = request.Password;
            user_service.UpdateUser(user);

            token_service.ConsumeResetToken(request.Token);

            log_service.LogAction(user.ID.ToString(), user.Username, user.Role.Name, "password_reset", "Successfully reset password using recovery token");

            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var tokenString = authHeader["Bearer ".Length..].Trim();
                token_service.InvalidateToken(tokenString);
            }
            return Ok();
        }
    }
}
