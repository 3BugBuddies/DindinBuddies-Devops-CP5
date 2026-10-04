using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.Application.Interfaces;

public interface IClienteRepositorio
{
    Task<IReadOnlyList<Cliente>> ListarAsync(string? busca, CancellationToken ct = default);
    Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> CpfExisteAsync(string cpf, CancellationToken ct = default);
    Task<bool> EmailExisteAsync(string email, int? ignorarClienteId = null, CancellationToken ct = default);
    Task<bool> PossuiContasAsync(int clienteId, CancellationToken ct = default);
    void Adicionar(Cliente cliente);
    void Remover(Cliente cliente);
}
