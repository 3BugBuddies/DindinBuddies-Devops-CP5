using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.Application.Interfaces;

public interface IContaRepositorio
{
    Task<IReadOnlyList<Conta>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<Conta?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>Maior NumeroConta da agência, ou null se ainda não houver contas.</summary>
    Task<string?> ObterMaiorNumeroAsync(string agencia, CancellationToken ct = default);

    void Adicionar(Conta conta);
}
