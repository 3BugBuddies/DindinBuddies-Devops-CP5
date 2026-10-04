using DindinBuddies.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace DindinBuddies.Api.Inicializacao;

/// <summary>
/// Aplica as migrations do EF Core em segundo plano ao iniciar a API.
/// Se o banco ainda não estiver pronto (ex.: SQL Server subindo no docker-compose),
/// tenta de novo algumas vezes. Falhas vão para o log e, na Azure, para o App Insights.
/// </summary>
public class AplicarMigrationsHostedService(IServiceProvider services, ILogger<AplicarMigrationsHostedService> logger)
    : BackgroundService
{
    private const int Tentativas = 10;
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var tentativa = 1; tentativa <= Tentativas; tentativa++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var contexto = scope.ServiceProvider.GetRequiredService<DindinBuddiesDbContext>();
                await contexto.Database.MigrateAsync(stoppingToken);

                logger.LogInformation("Migrations aplicadas com sucesso.");
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (tentativa == Tentativas)
                {
                    logger.LogError(ex, "Não foi possível aplicar as migrations após {Tentativas} tentativas.", Tentativas);
                    return;
                }

                logger.LogWarning("Banco indisponível (tentativa {Tentativa} de {Tentativas}): {Mensagem}. Nova tentativa em {Segundos}s.",
                    tentativa, Tentativas, ex.Message, Intervalo.TotalSeconds);
                await Task.Delay(Intervalo, stoppingToken);
            }
        }
    }
}
