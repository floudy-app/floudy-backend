using Floudy.API.Services;
using Floudy.API.Storage;
using Floudy.API.Utility;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace Floudy.API;

public partial class Program
{
    private const int RECENT_LOG_COUNT = 100;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        //builder.WebHost.ConfigureKestrel((context, options) => options.Configure(context.Configuration.GetSection("Kestrel")));

        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        builder.Services.AddSingleton(new FileRepository());
        builder.Services.AddSingleton(new UserRepository());
        builder.Services.AddSingleton(new ChatRepository());
        builder.Services.AddSingleton(new LogRepository());

        builder.Services.AddSingleton<FileService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<ChatService>();
        builder.Services.AddSingleton<LogService>();
        builder.Services.AddSingleton<MaliciousDetectionService>();
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSingleton<EmailService>();

        builder.Services.AddCors(options => options.AddPolicy("floudy_frontend", policy =>
        {
            policy.WithOrigins("http://10.174.104.44:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }));

        var app = builder.Build();

        app.UseCors("floudy_frontend");
        app.MapOpenApi();
        //app.UseHttpsRedirection();
        app.MapControllers();
        app.MapHub<FloudyHub>("/floudyhub");

        using (var scope = app.Services.CreateScope())
        {
            using (var context = new AppDbContext())
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
