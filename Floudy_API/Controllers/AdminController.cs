using Floudy.API.Services;
using Floudy.API.Utility;
using Floudy.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Floudy.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [TokenAuthorize(Roles = "Admin")]
    public class AdminController(UserService user_service, IHubContext<FloudyHub> hub_context, LogService log_service) : ControllerBase
    {
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var users = user_service.GetAllNonAdmins().Select(u => new
            {
                id = u.ID.ToString(),
                username = u.Username,
                role = u.Role.Name,
                isBlocked = u.IsBlocked
            });

            return Ok(new { users = users.ToList() });
        }

        [HttpPut("users/{id}/rename")]
        public async Task<IActionResult> RenameUser(long id, [FromQuery] string username)
        {
            var token = (UserToken)HttpContext.Items["UserToken"]!;
            var uId = token.UserId.ToString();
            var uName = token.Username;

            if (!user_service.ValidateRename(username, out var errorMessage)) return BadRequest(errorMessage);

            var target = user_service.GetById(id);
            if (target == null) return NotFound();
            var oldName = target.Username;

            target.Username = username;
            user_service.UpdateUser(target);

            log_service.LogAction(uId, uName, "Admin", "user_rename", $"Renamed user \"{oldName}\" (id {id}) to \"{username}\"");

            await hub_context.Clients.Group(oldName).SendAsync("ForceLogout");

            return Ok();
        }

        [HttpPut("users/{id}/block")]
        public async Task<IActionResult> BlockUser(long id)
        {
            var token = (UserToken)HttpContext.Items["UserToken"]!;
            var uId = token.UserId.ToString();
            var uName = token.Username;

            var target = user_service.GetById(id);
            if (target == null) return NotFound();

            target.IsBlocked = true;
            user_service.UpdateUser(target);

            log_service.LogAction(uId, uName, "Admin", "user_block", $"Blocked user \"{target.Username}\" (id {id})");

            await hub_context.Clients.Group(target.Username).SendAsync("ForceLogout");
            return Ok();
        }

        [HttpPut("users/{id}/unblock")]
        public IActionResult UnblockUser(long id)
        {
            var token = (UserToken)HttpContext.Items["UserToken"]!;
            var uId = token.UserId.ToString();
            var uName = token.Username;

            var target = user_service.GetById(id);
            if (target == null) return NotFound();

            target.IsBlocked = false;
            user_service.UpdateUser(target);

            log_service.LogAction(uId, uName, "Admin", "user_unblock", $"Unblocked user \"{target.Username}\" (id {id})");

            return Ok();
        }
    }
}
