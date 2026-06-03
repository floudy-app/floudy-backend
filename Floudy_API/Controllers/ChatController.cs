using Floudy.API.Services;
using Floudy.API.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Floudy.API.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [TokenAuthorize]
    public class ChatController(ChatService chat_service) : ControllerBase
    {
        [HttpGet("messages")]
        public IActionResult GetMessages([FromQuery] int count = 100)
        {
            var messages = chat_service.GetRecent(count).Select(m => new
            {
                id = m.Id,
                username = m.Username,
                text = m.Text,
                timestamp = m.Timestamp
            });

            return Ok(new { messages = messages.ToList() });
        }
    }
}
