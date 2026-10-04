using DindinBuddies.Application.Interfaces;
using DindinBuddies.Infrastructure.Persistencia;
using DindinBuddies.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DindinBuddies.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DindinBuddiesDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IContaRepositorio, ContaRepositorio>();
        services.AddScoped<ITransacaoRepositorio, TransacaoRepositorio>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        return services;
    }
}
