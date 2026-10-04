using System.Globalization;
using DindinBuddies.Application.Dtos.Contas;
using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.Application.Servicos;

public class ContaServico(IClienteRepositorio clientes, IContaRepositorio contas, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public const string AgenciaPadrao = "0001";

    // Duas aberturas simultâneas podem calcular o mesmo número; o índice único
    // (Agencia, NumeroConta) recusa a segunda, que tenta de novo com o próximo.
    private const int TentativasDeNumeracao = 3;

    public async Task<IReadOnlyList<ContaResponse>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default)
    {
        await GarantirClienteExisteAsync(clienteId, ct);
        var lista = await contas.ListarPorClienteAsync(clienteId, ct);
        return lista.Select(ContaResponse.De).ToList();
    }

    public async Task<ContaResponse> ObterAsync(int id, CancellationToken ct = default) =>
        ContaResponse.De(await ObterEntidadeAsync(id, ct));

    /// <summary>
    /// Busca pela agência e pelo número que o usuário conhece (usado para achar o destino de uma transferência).
    /// Números digitados sem os zeros à esquerda (ex.: 42) são completados para 6 dígitos (000042).
    /// </summary>
    public async Task<ContaResponse> ObterPorNumeroAsync(string agencia, string numeroConta, CancellationToken ct = default)
    {
        var numero = numeroConta.Trim().PadLeft(6, '0');
        var conta = await contas.ObterPorNumeroAsync(agencia.Trim(), numero, ct)
            ?? throw new RecursoNaoEncontradoException($"Conta {agencia}/{numero} não encontrada.");
        return ContaResponse.De(conta);
    }

    public async Task<ContaResponse> AbrirAsync(int clienteId, AbrirContaRequest request, CancellationToken ct = default)
    {
        await GarantirClienteExisteAsync(clienteId, ct);

        for (var tentativa = 1; ; tentativa++)
        {
            var conta = new Conta(clienteId, AgenciaPadrao, await ProximoNumeroAsync(ct), request.TipoConta!.Value);
            contas.Adicionar(conta);

            try
            {
                await unidadeDeTrabalho.SalvarAsync(ct);
                return ContaResponse.De(conta);
            }
            catch (ConflitoDeDadosException) when (tentativa < TentativasDeNumeracao)
            {
                // Número já usado por outra abertura simultânea; calcula de novo.
            }
        }
    }

    public async Task<ContaResponse> EncerrarAsync(int id, CancellationToken ct = default)
    {
        var conta = await ObterEntidadeAsync(id, ct);
        conta.Encerrar();
        await unidadeDeTrabalho.SalvarAsync(ct);
        return ContaResponse.De(conta);
    }

    /// <summary>Número sequencial de 6 dígitos na agência padrão: 000001, 000002...</summary>
    private async Task<string> ProximoNumeroAsync(CancellationToken ct)
    {
        var maior = await contas.ObterMaiorNumeroAsync(AgenciaPadrao, ct);
        var proximo = maior is null ? 1 : int.Parse(maior, CultureInfo.InvariantCulture) + 1;
        return proximo.ToString("D6", CultureInfo.InvariantCulture);
    }

    private async Task GarantirClienteExisteAsync(int clienteId, CancellationToken ct)
    {
        if (await clientes.ObterPorIdAsync(clienteId, ct) is null)
            throw new RecursoNaoEncontradoException($"Cliente {clienteId} não encontrado.");
    }

    private async Task<Conta> ObterEntidadeAsync(int id, CancellationToken ct) =>
        await contas.ObterPorIdAsync(id, ct)
        ?? throw new RecursoNaoEncontradoException($"Conta {id} não encontrada.");
}
