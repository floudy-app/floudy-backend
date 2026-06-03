using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Floudy.API.Storage.Entities;

using Floudy.API.Models;

namespace Floudy.API.Services
{
    public class TokenService
    {
        private readonly ConcurrentDictionary<string, UserToken> tokens = new();
        private const int InactivityMinutes = 10;

        public UserToken GenerateToken(UserEntity user)
        {
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            var tokenString = Convert.ToBase64String(tokenBytes)
                                     .Replace("+", "")
                                     .Replace("/", "")
                                     .Replace("=", "");

            var token = new UserToken
            {
                Token = tokenString,
                UserId = user.ID,
                Username = user.Username,
                Role = user.Role.Name,
                ExpiresAt = DateTime.UtcNow.AddMinutes(InactivityMinutes)
            };

            tokens[tokenString] = token;
            return token;
        }

        public UserToken? ValidateAndSlideToken(string tokenString)
        {
            if (string.IsNullOrEmpty(tokenString)) return null;

            if (tokens.TryGetValue(tokenString, out var token))
            {
                if (token.ExpiresAt < DateTime.UtcNow)
                {
                    tokens.TryRemove(tokenString, out _);
                    return null;
                }

                token.ExpiresAt = DateTime.UtcNow.AddMinutes(InactivityMinutes);
                return token;
            }

            return null;
        }

        public void InvalidateToken(string tokenString)
        {
            if (!string.IsNullOrEmpty(tokenString)) tokens.TryRemove(tokenString, out _);
        }

        private readonly ConcurrentDictionary<string, ResetToken> resetTokens = new();
        private const int ResetTokenExpiryMinutes = 5;

        public string GenerateResetToken(long userId)
        {
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(tokenBytes);

            var tokenString = Convert.ToBase64String(tokenBytes)
                                     .Replace("+", "")
                                     .Replace("/", "")
                                     .Replace("=", "");

            var resetToken = new ResetToken
            {
                Token = tokenString,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes)
            };

            resetTokens[tokenString] = resetToken;
            return tokenString;
        }

        public int ValidateResetToken(string tokenString, out ResetToken? resetToken)
        {
            if (string.IsNullOrEmpty(tokenString))
            {
                resetToken = null;
                return -1;
            }

            if (resetTokens.TryGetValue(tokenString, out resetToken))
            {
                if (resetToken.ExpiresAt < DateTime.UtcNow) return 0;
                return 1;
            }

            resetToken = null;
            return -1;
        }

        public void ConsumeResetToken(string tokenString)
        {
            if (!string.IsNullOrEmpty(tokenString)) resetTokens.TryRemove(tokenString, out _);
        }
    }
}
