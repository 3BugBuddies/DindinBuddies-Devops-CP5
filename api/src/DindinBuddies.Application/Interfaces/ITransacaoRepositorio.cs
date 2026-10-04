using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.Application.Interfaces;

public interface ITransacaoRepositorio
{
    void Adicionar(Transacao transacao);

    /// <summary>
    /// Transações em que a conta é origem (ContaId) ou destino (ContaDestinoId),
    /// com DataHora em [inicio, fim], da mais recente para a mais antiga.
    /// </summary>
    Task<IReadOnlyList<Transacao>> ListarExtratoAsync(int contaId, DateTime inicio, DateTime fim, CancellationToken ct = default);
}
