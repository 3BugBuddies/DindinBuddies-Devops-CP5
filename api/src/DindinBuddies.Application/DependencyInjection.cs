using DindinBuddies.Application.Servicos;
using Microsoft.Extensions.DependencyInjection;

namespace DindinBuddies.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClienteServico>();
        services.AddScoped<ContaServico>();
        services.AddScoped<MovimentacaoServico>();
        return services;
    }
}
