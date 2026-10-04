using DindinBuddies.Application.Interfaces;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace DindinBuddies.Infrastructure.Repositorios;

public class TransacaoRepositorio(DindinBuddiesDbContext contexto) : ITransacaoRepositorio
{
    public Task<Transacao?> ObterPorIdAsync(long id, CancellationToken ct = default) =>
        contexto.Transacoes
            .Include(t => t.Conta)
            .Include(t => t.ContaDestino)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> ExisteParaContaAsync(int contaId, CancellationToken ct = default) =>
        contexto.Transacoes.AnyAsync(t => t.ContaId == contaId || t.ContaDestinoId == contaId, ct);

    public void Adicionar(Transacao transacao) => contexto.Transacoes.Add(transacao);

    public void Remover(Transacao transacao) => contexto.Transacoes.Remove(transacao);

    public async Task<IReadOnlyList<Transacao>> ListarExtratoAsync(int contaId, DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        await contexto.Transacoes
            .AsNoTracking()
            .Where(t => (t.ContaId == contaId || t.ContaDestinoId == contaId)
                        && t.DataHora >= inicio
                        && t.DataHora <= fim)
            .OrderByDescending(t => t.DataHora)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct);
}
