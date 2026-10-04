using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DindinBuddies.Infrastructure.Persistencia;

public class UnidadeDeTrabalho(DindinBuddiesDbContext contexto) : IUnidadeDeTrabalho
{
    // Códigos do SQL Server para violação de índice único e de chave única.
    private const int IndiceUnicoViolado = 2601;
    private const int ChaveUnicaViolada = 2627;

    public async Task SalvarAsync(CancellationToken ct = default)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: IndiceUnicoViolado or ChaveUnicaViolada })
        {
            // Descarta as alterações recusadas para que quem chamou possa tentar de novo.
            contexto.ChangeTracker.Clear();
            throw new ConflitoDeDadosException("Já existe um registro com estes dados.", ex);
        }
    }
}
