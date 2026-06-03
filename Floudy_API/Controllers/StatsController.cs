using Floudy.API.Models;
using Floudy.API.Services;
using Floudy.API.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Floudy.API.Controllers;

[Route("api/stats")]
[ApiController]
[TokenAuthorize]
public class StatsController(FileService file_service) : ControllerBase
{
    private readonly FileService file_service = file_service;

    [HttpGet("type")]
    public IActionResult GetByType(int page_index)
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;

        var response = file_service.TypeStatistics(token.UserId).Select(entry => new
        {
            type = entry.Key,
            count = entry.Value
        });

        return Ok(new { entries = response.ToList() });
    }

    [HttpGet("upload")]
    public IActionResult GetByUpload([FromQuery] int count = 10)
    {
        var token = (UserToken)HttpContext.Items["UserToken"]!;

        var response = file_service.UploadStatistics(token.UserId).Select(entry => new
        {
            date = $"{entry.Key.Year}/{entry.Key.Month:D2}",
            count = entry.Value
        });

        return Ok(new { entries = response.ToList() });
    }
}
