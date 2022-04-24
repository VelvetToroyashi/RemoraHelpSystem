using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using VTP.Remora.Commands.HelpSystem.Services;

namespace VTP.Remora.Commands.HelpSystem;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddHelpSystem(this IServiceCollection services, string? treeName = null, bool addHelpCommand = true)
    {
        services.Configure<HelpSystemOptions>(o => o = o with { TreeName = treeName });

        if (addHelpCommand)
        {
            services
                .AddDiscordCommands()
                .AddCommandTree(treeName)
                .WithCommandGroup<HelpCommand>()
                .Finish();
        }

        services.AddScoped<TreeWalker>();

        services.AddScoped<IHelpFormatter, DefaultHelpFormatter>();
        services.AddScoped<ICommandHelpService, CommandHelpService>();
        
        return services;
    }
}