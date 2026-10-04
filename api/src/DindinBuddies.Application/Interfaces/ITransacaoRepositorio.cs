using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.Application.Interfaces;

public interface ITransacaoRepositorio
{
    /// <summary>Transação com a conta de origem e, se houver, a de destino carregadas.</summary>
    Task<Transacao?> ObterPorIdAsync(long id, CancellationToken ct = default);

    /// <summary>Verdadeiro se a conta aparece em alguma transação, como origem ou destino.</summary>
    Task<bool> ExisteParaContaAsync(int contaId, CancellationToken ct = default);

    void Adicionar(Transacao transacao);
    void Remover(Transacao transacao);

    /// <summary>
    /// Transações em que a conta é origem (ContaId) ou destino (ContaDestinoId),
    /// com DataHora em [inicio, fim], da mais recente para a mais antiga.
    /// </summary>
    Task<IReadOnlyList<Transacao>> ListarExtratoAsync(int contaId, DateTime inicio, DateTime fim, CancellationToken ct = default);
}
