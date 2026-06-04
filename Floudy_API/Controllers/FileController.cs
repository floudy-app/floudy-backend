using Floudy.API.Services;
using Floudy.API.Models;
using Floudy.API.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Floudy.API.Controllers;

[Route("api/files")]
[ApiController]
[TokenAuthorize]
public class FileController(FileService file_service, IHubContext<FloudyHub> hub_context, LogService log_service) : ControllerBase
{
    private readonly FileService file_service = file_service;

    [HttpGet("page/{page_index}")]
    public IActionResult GetPage(int page_index)
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;

        var files = file_service.GetPage(page_index, token.UserId).ToList();
        var baseIndex = (page_index - 1) * FileService.PAGE_FILE_COUNT;

        var response = files.Select((file, i) => new
        {
            id = file.ID.ToString(),
            displayId = baseIndex + i + 1,
            name = file.Name,
            size = file.ByteSize,
            type = file.Type,
            uploaded = file.UploadDate,
            base64 = Convert.ToBase64String(file.Content)
        });

        return Ok(new
        {
            total_pages = file_service.TotalPageCount(token.UserId),
            files = response.ToList()
        });
    }

    [HttpGet("recent")]
    public IActionResult GetRecent([FromQuery] int count = 10)
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;
        var positionMap = file_service.GetFileIdPositionMap(token.UserId);

        var response = file_service.GetRecent(count, token.UserId).Select(file => new
        {
            id = file.ID.ToString(),
            displayId = positionMap.TryGetValue(file.ID, out var pos) ? pos : 0,
            name = file.Name,
            size = file.ByteSize,
            type = file.Type,
            uploaded = file.UploadDate,
            base64 = Convert.ToBase64String(file.Content)
        });

        return Ok(new { files = response.ToList() });
    }

    [HttpPost]
    public async Task<IActionResult> Post(IFormFile file, [FromForm] string metadata)
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;
        var uName = token.Username;

        using var ms = new MemoryStream();
        file.CopyTo(ms);

        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
        data?.Add("raw", ms.ToArray());
        await ms.DisposeAsync();

        if (!file_service.UploadFromMetadata(data, token.UserId)) return StatusCode(422);

        await hub_context.Clients.Group(uName).SendAsync("FileChanged", "added");

        return StatusCode(201);
    }

    [HttpPut("rename/{id}")]
    public async Task<IActionResult> Put(long id, [FromQuery] string name = "")
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;
        var uName = token.Username;
        var uId = token.UserId.ToString();
        var uGroup = token.Role;

        if (!file_service.BelongsToUser(id, token.UserId)) return NotFound();

        if (file_service.ModifyMetadata(id, new RawFileMetadata(name), token.UserId))
        {
            log_service.LogAction(uId, uName, uGroup, "file_rename", $"Renamed file {id} to \"{name}\"");

            await hub_context.Clients.Group(uName).SendAsync("FileChanged", "updated");
            return Ok();
        }
        return StatusCode(400);
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;
        var uName = token.Username;
        var uId = token.UserId.ToString();
        var uGroup = token.Role;

        if (!file_service.BelongsToUser(id, token.UserId)) return NotFound();

        var fileName = file_service.GetById(id, token.UserId).Name;

        if (file_service.DeleteFile(id, token.UserId))
        {
            log_service.LogAction(uId, uName, uGroup, "file_delete", $"Deleted file #{id} ({fileName})");

            await hub_context.Clients.Group(uName).SendAsync("FileChanged", "deleted");
            return StatusCode(204);
        }
        return StatusCode(404);
    }
}
