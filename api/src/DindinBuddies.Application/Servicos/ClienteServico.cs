using DindinBuddies.Application.Dtos.Clientes;
using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Excecoes;

namespace DindinBuddies.Application.Servicos;

public class ClienteServico(IClienteRepositorio clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<IReadOnlyList<ClienteResponse>> ListarAsync(string? busca, CancellationToken ct = default)
    {
        var lista = await clientes.ListarAsync(busca, ct);
        return lista.Select(ClienteResponse.De).ToList();
    }

    public async Task<ClienteResponse> ObterAsync(int id, CancellationToken ct = default) =>
        ClienteResponse.De(await ObterEntidadeAsync(id, ct));

    public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request, CancellationToken ct = default)
    {
        if (await clientes.CpfExisteAsync(request.Cpf, ct))
            throw new RegraDeNegocioException("Já existe um cliente com este CPF.");
        if (await clientes.EmailExisteAsync(NormalizarEmail(request.Email), null, ct))
            throw new RegraDeNegocioException("Já existe um cliente com este e-mail.");

        var cliente = new Cliente(request.Nome, request.Cpf, request.Email, request.Telefone, request.DataNascimento!.Value);
        clientes.Adicionar(cliente);
        await unidadeDeTrabalho.SalvarAsync(ct);

        return ClienteResponse.De(cliente);
    }

    public async Task<ClienteResponse> EditarAsync(int id, EditarClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await ObterEntidadeAsync(id, ct);

        if (await clientes.EmailExisteAsync(NormalizarEmail(request.Email), id, ct))
            throw new RegraDeNegocioException("Já existe um cliente com este e-mail.");

        cliente.Atualizar(request.Nome, request.Email, request.Telefone, request.DataNascimento!.Value);
        await unidadeDeTrabalho.SalvarAsync(ct);

        return ClienteResponse.De(cliente);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var cliente = await ObterEntidadeAsync(id, ct);

        // Mesmo contas encerradas impedem a exclusão: o histórico nunca é apagado.
        if (await clientes.PossuiContasAsync(id, ct))
            throw new RegraDeNegocioException("O cliente possui contas e não pode ser excluído.");

        clientes.Remover(cliente);
        await unidadeDeTrabalho.SalvarAsync(ct);
    }

    // Mesma normalização que Cliente aplica ao gravar.
    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task<Cliente> ObterEntidadeAsync(int id, CancellationToken ct) =>
        await clientes.ObterPorIdAsync(id, ct)
        ?? throw new RecursoNaoEncontradoException($"Cliente {id} não encontrado.");
}
