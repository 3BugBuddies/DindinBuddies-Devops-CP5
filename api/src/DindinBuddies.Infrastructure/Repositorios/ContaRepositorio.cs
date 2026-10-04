using DindinBuddies.Application.Interfaces;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace DindinBuddies.Infrastructure.Repositorios;

public class ContaRepositorio(DindinBuddiesDbContext contexto) : IContaRepositorio
{
    public async Task<IReadOnlyList<Conta>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default) =>
        await contexto.Contas
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderBy(c => c.DataAbertura)
            .ToListAsync(ct);

    public Task<Conta?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Contas.FirstOrDefaultAsync(c => c.Id == id, ct);

    // NumeroConta tem largura fixa (000001), então o maior texto é também o maior número.
    public Task<string?> ObterMaiorNumeroAsync(string agencia, CancellationToken ct = default) =>
        contexto.Contas
            .Where(c => c.Agencia == agencia)
            .MaxAsync(c => (string?)c.NumeroConta, ct);

    public void Adicionar(Conta conta) => contexto.Contas.Add(conta);
}
