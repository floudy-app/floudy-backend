namespace Floudy.API.Models
{
    public record LogRequest
    (
        string UserId,
        string Username,
        string GroupName,
        string Action,
        string Description,
        int? FileCount = null,
        string[]? FileNames = null,
        long[]? FileSizes = null
    );
}
