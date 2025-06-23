📂 Project Structure
ExternalApiIntegrationSolution/
├── ExternalApiIntegration/           # Core logic, models, services
├── ExternalApiIntegration.Demo/      # Console app to run the integration
├── ExternalApiIntegration.Tests/     # Unit tests using Xunit & Moq

✨ Features
- ✅ Typed HttpClient with API Key header injection
- ✅ Strongly typed config via IOptions<T>
- ✅ Pagination support with GetAllUsersAsync()
- ✅ Per-user and full-list caching using IMemoryCache
- ✅ Graceful error handling for 404s, deserialization, and network issues
- ✅ Clean architecture using SOLID principles
- ✅ Unit test coverage for deserialization, caching, errors, and paging

🛠️ Tech Stack
- .NET 8 with Generic Host for DI, logging, and config
- HttpClient, System.Text.Json, Microsoft.Extensions.*
- Xunit, Moq, and Moq.Protected for mocking HttpMessageHandler

⚙️ Setup Instructions
1. Clone the repository
git clone https://github.com/your-username/ExternalApiIntegrationDemo.git
cd ExternalApiIntegrationDemo

2 Create appsettings.json in ExternalApiIntegration.Demo/
{
  "ReqresApi": {
    "BaseUrl": "https://reqres.in/api",
    "ApiKey": "reqres-free-v1"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "System.Net.Http.HttpClient": "None",
      "System.Net.Http.HttpClient.Default": "None"
    }
  }
}
- 📌 Set Build Action: Content and Copy to Output Directory: Copy if newer.

3 Run the Console App
dotnet run --project ExternalApiIntegration.Demo

4 Run Tests
dotnet test ExternalApiIntegration.Tests

📦 NuGet Packages

Runtime Dependencies
dotnet add package Microsoft.Extensions.Http
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Caching.Memory
dotnet add package System.Text.Json

Unit Testing
dotnet add package xunit
dotnet add package Moq
dotnet add package Moq.Protected
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio

🔎 Example Output
=== All Users ===
1: George Bluth - george.bluth@reqres.in
2: Janet Weaver - janet.weaver@reqres.in

Enter user ID to fetch: 2
User 2: Janet Weaver - janet.weaver@reqres.in

📚 Learning Goals
- How to consume paginated REST APIs with HttpClient
- Clean separation using service interfaces
- Dependency injection in console apps
- How to mock HttpClient calls in unit tests
- Using IMemoryCache to reduce external calls


