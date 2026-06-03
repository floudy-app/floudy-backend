using Floudy.API.Services;
using Floudy.API.Utility;
using Floudy.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Floudy.API.Controllers
{
    [Route("api/log")]
    [ApiController]
    [TokenAuthorize]
    public class LogController(LogService log_service, MaliciousDetectionService malicious_detection) : ControllerBase
    {
        [HttpPost]
        public IActionResult Log([FromBody] LogRequest request)
        {
            var token = (UserToken)HttpContext.Items["UserToken"]!;
            log_service.LogAction(token.UserId.ToString(), token.Username, token.Role, request.Action, request.Description);

            if (request.Action == "file_upload")
            {
                malicious_detection.CheckBatchUpload(
                    token.UserId.ToString(),
                    token.Username,
                    request.FileCount ?? 0,
                    request.FileNames ?? [],
                    request.FileSizes ?? []
                );
            }

            return Ok();
        }

        [HttpGet("suspicious")]
        [TokenAuthorize(Roles = "Admin")]
        public IActionResult GetSuspiciousUsers()
        {
            var users = log_service.GetSuspiciousUsers().Select(s => new
            {
                id = s.ID,
                userId = s.UserId,
                username = s.Username,
                reason = s.Reason,
                detectedAt = s.DetectedAt
            });

            return Ok(new { users = users.ToList() });
        }
    }
}
