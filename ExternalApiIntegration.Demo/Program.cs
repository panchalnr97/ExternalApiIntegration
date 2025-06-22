using ExternalApiIntegration.Configuration;
using ExternalApiIntegration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration((context, config) =>
          {
              config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
          })
          .ConfigureLogging(logging =>
          {
              logging.ClearProviders();
              logging.AddConsole();
              logging.SetMinimumLevel(LogLevel.Warning);
              logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
              logging.AddFilter("System.Net.Http.HttpClient.Default", LogLevel.None);
          })
          .ConfigureServices((context, services) =>
          {
              var config = context.Configuration;

              services.Configure<ReqresApiOptions>(config.GetSection("ReqresApi"));

              services.AddMemoryCache();

              services.AddHttpClient<ExternalUserService>((sp, client) =>
              {
                  var options = sp.GetRequiredService<IOptions<ReqresApiOptions>>().Value;
                  client.BaseAddress = new Uri(options.BaseUrl);
                  client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                  if (!string.IsNullOrWhiteSpace(options.ApiKey))
                      client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
              });

              services.AddTransient<ExternalUserService>();
          })
          .Build();

        var userService = host.Services.GetRequiredService<ExternalUserService>();

        try
        {
            // Fetch and display all users
            var users = await userService.GetAllUsersAsync();
            Console.WriteLine("=== All Users ===");
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Id}: {user.First_Name} {user.Last_Name} - {user.Email}");
            }

            Console.WriteLine();

            // Fetch specific user by ID
            Console.Write("Enter user ID to fetch: ");
            if (int.TryParse(Console.ReadLine(), out int userId))
            {
                var user = await userService.GetUserByIdAsync(userId);
                Console.WriteLine(user != null
                    ? $"User {user.Id}: {user.First_Name} {user.Last_Name} - {user.Email}"
                    : "User not found.");
            }
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"Not found: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network issue: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

    }
}

