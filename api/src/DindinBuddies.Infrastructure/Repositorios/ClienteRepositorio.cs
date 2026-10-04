using DindinBuddies.Application.Interfaces;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace DindinBuddies.Infrastructure.Repositorios;

public class ClienteRepositorio(DindinBuddiesDbContext contexto) : IClienteRepositorio
{
    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? busca, CancellationToken ct = default)
    {
        var consulta = contexto.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            // Busca por CPF ignora a máscara (pontos e traço) que o usuário possa digitar.
            var digitos = new string(termo.Where(char.IsDigit).ToArray());

            consulta = digitos.Length > 0
                ? consulta.Where(c => c.Nome.Contains(termo) || c.Cpf.StartsWith(digitos))
                : consulta.Where(c => c.Nome.Contains(termo));
        }

        return await consulta.OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> CpfExisteAsync(string cpf, CancellationToken ct = default) =>
        contexto.Clientes.AnyAsync(c => c.Cpf == cpf, ct);

    public Task<bool> EmailExisteAsync(string email, int? ignorarClienteId = null, CancellationToken ct = default) =>
        contexto.Clientes.AnyAsync(c => c.Email == email && c.Id != ignorarClienteId, ct);

    public Task<bool> PossuiContasAsync(int clienteId, CancellationToken ct = default) =>
        contexto.Contas.AnyAsync(c => c.ClienteId == clienteId, ct);

    public void Adicionar(Cliente cliente) => contexto.Clientes.Add(cliente);

    public void Remover(Cliente cliente) => contexto.Clientes.Remove(cliente);
}
