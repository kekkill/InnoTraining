using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace InnoShop.Users.Api.Composition.Configuration;

public static class DevelopmentConfiguration
{
    public static void AddLocalDevelopment(this WebApplicationBuilder builder, string service, string[] args)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        var root = FindSolutionRoot(builder.Environment.ContentRootPath);
        var localDirectory = Path.Combine(root, ".local");
        builder.Configuration.AddJsonFile(Path.Combine(localDirectory, "development.json"));
        builder.Configuration["ConnectionStrings:Sqlite"] =
            $"Data Source={Path.Combine(localDirectory, service.ToLowerInvariant() + ".db")}";

        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);
    }

    private static string FindSolutionRoot(string contentRoot)
    {
        var directory = new DirectoryInfo(contentRoot);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InnoShop.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("solution file was not found");
    }
}
