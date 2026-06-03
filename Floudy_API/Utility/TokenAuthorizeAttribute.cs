using System;
using System.Linq;
using System.Threading.Tasks;
using Floudy.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Floudy.API.Utility
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TokenAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public string Roles { get; set; } = string.Empty;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tokenService = context.HttpContext.RequestServices.GetRequiredService<TokenService>();

            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new UnauthorizedObjectResult("Missing or invalid Authorization header.");
                return;
            }

            var tokenString = authHeader.Substring("Bearer ".Length).Trim();
            var token = tokenService.ValidateAndSlideToken(tokenString);

            if (token == null)
            {
                context.Result = new UnauthorizedObjectResult("Session expired or invalid token.");
                return;
            }

            if (!string.IsNullOrEmpty(Roles))
            {
                var allowedRoles = Roles.Split(',').Select(r => r.Trim());
                if (!allowedRoles.Contains(token.Role, StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new ObjectResult("Access denied. Admin role required.") { StatusCode = 403 };
                    return;
                }
            }

            context.HttpContext.Items["UserToken"] = token;

            await next();
        }
    }
}
