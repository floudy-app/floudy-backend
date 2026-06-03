using Floudy.API.Services;
using Microsoft.AspNetCore.SignalR;

namespace Floudy.API.Utility
{
    public class FloudyHub(ChatService chat_service, UserService user_service, LogService log_service, MaliciousDetectionService malicious_detection) : Hub
    {
        public async Task JoinGroup(string username) => await Groups.AddToGroupAsync(Context.ConnectionId, username);

        public async Task LeaveGroup(string username) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);

        public async Task JoinChat() => await Groups.AddToGroupAsync(Context.ConnectionId, "chat");

        public async Task LeaveChat() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "chat");

        public async Task SendChatMessage(string username, string text)
        {
            var user = user_service.GetByUsername(username);
            var userId = user?.ID.ToString() ?? "unknown";
            var group = user?.Role.Name ?? "User";

            var message = chat_service.PostMessage(username, text);

            log_service.LogAction(userId, username, group, "message_send", $"Sent chat message: \"{(text.Length > 80 ? text[..80] + "..." : text)}\"");
            malicious_detection.CheckChatMessage(userId, username, text);

            await Clients.Group("chat").SendAsync("ReceiveChatMessage", new
            {
                id = message.Id,
                username = message.Username,
                text = message.Text,
                timestamp = message.Timestamp
            });
        }

        public async Task DeleteChatMessage(string id, string requesterUsername)
        {
            var user = user_service.GetByUsername(requesterUsername);
            if (user == null || user.Role.Name != "Admin") return;

            if (chat_service.DeleteMessage(id))
            {
                log_service.LogAction(user.ID.ToString(), requesterUsername, "Admin", "message_delete", $"Deleted chat message {id}");
                await Clients.Group("chat").SendAsync("ChatMessageDeleted", id);
            }
        }
    }
}
