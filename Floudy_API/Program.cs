using Floudy.API.Services;
using Floudy.API.Storage;
using Floudy.API.Utility;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace Floudy.API;

public partial class Program
{
    private const int RECENT_LOG_COUNT = 100;
    private const string DefaultCorsOrigin = "https://floudy-app.github.io";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var port = Environment.GetEnvironmentVariable("PORT") ?? "5057";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var floudyDb = builder.Configuration.GetConnectionString("FloudyDB")
            ?? throw new InvalidOperationException("Connection string 'FloudyDB' is not configured.");
        var floudyLogDb = builder.Configuration.GetConnectionString("FloudyLogDB")
            ?? throw new InvalidOperationException("Connection string 'FloudyLogDB' is not configured.");
        var mongoDb = builder.Configuration.GetConnectionString("MongoDB")
            ?? throw new InvalidOperationException("Connection string 'MongoDB' is not configured.");

        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        builder.Services.AddSingleton(new FileRepository(floudyDb));
        builder.Services.AddSingleton(new UserRepository(floudyDb));
        builder.Services.AddSingleton(new ChatRepository(mongoDb));
        builder.Services.AddSingleton(new LogRepository(floudyLogDb));

        builder.Services.AddSingleton<FileService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<ChatService>();
        builder.Services.AddSingleton<LogService>();
        builder.Services.AddSingleton<MaliciousDetectionService>();
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSingleton<EmailService>();

        var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        if (corsOrigins == null || corsOrigins.Length == 0)
        {
            corsOrigins = [DefaultCorsOrigin];
        }

        builder.Services.AddCors(options => options.AddPolicy("floudy_frontend", policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }));

        var app = builder.Build();

        app.UseCors("floudy_frontend");
        app.MapGet("/", () => Results.Ok("Floudy API"));
        app.MapOpenApi();
        app.MapControllers();
        app.MapHub<FloudyHub>("/floudyhub");

        using (var scope = app.Services.CreateScope())
        {
            using (var context = new AppDbContext(floudyDb))
            {
                var max_id = context.Files.Any() ? context.Files.Max(f => f.ID) : 0;
                GlobalIdManager.BaseValue = max_id + 1;
                GlobalIdManager.Reset();
            }

            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.Write("Retreive recent logs? (y/n): ");
                var response = Console.ReadKey();
                Console.WriteLine();

                if (response.Key == ConsoleKey.Y)
                {
                    var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                    var logs = logService.GetRecentLogs().Take(RECENT_LOG_COUNT).Reverse().ToList();

                    Console.WriteLine();
                    foreach (var log in logs) logService.PrintLog(log);
                }
            }
        }

        app.Run();
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
}
