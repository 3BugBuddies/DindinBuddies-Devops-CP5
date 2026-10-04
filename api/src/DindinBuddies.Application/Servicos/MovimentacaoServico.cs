using DindinBuddies.Application.Dtos.Movimentacoes;
using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;
using DindinBuddies.Domain.Excecoes;

namespace DindinBuddies.Application.Servicos;

public class MovimentacaoServico(
    IContaRepositorio contas,
    ITransacaoRepositorio transacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public const int DiasPadraoDoExtrato = 30;

    public async Task<MovimentacaoResponse> DepositarAsync(int contaId, MovimentacaoRequest request, CancellationToken ct = default)
    {
        var conta = await ObterContaAsync(contaId, ct);
        var transacao = conta.Depositar(request.Valor!.Value, request.Descricao);
        return await RegistrarAsync(transacao, conta, ct);
    }

    public async Task<MovimentacaoResponse> SacarAsync(int contaId, MovimentacaoRequest request, CancellationToken ct = default)
    {
        var conta = await ObterContaAsync(contaId, ct);
        var transacao = conta.Sacar(request.Valor!.Value, request.Descricao);
        return await RegistrarAsync(transacao, conta, ct);
    }

    /// <summary>
    /// Debita a origem, credita o destino e grava a transação em um único SalvarAsync:
    /// ou tudo é gravado, ou nada é.
    /// </summary>
    public async Task<MovimentacaoResponse> TransferirAsync(int contaId, TransferenciaRequest request, CancellationToken ct = default)
    {
        var origem = await ObterContaAsync(contaId, ct);
        var destino = await contas.ObterPorIdAsync(request.ContaDestinoId!.Value, ct)
            ?? throw new RegraDeNegocioException($"Conta de destino {request.ContaDestinoId} não encontrada.");

        var transacao = origem.TransferirPara(destino, request.Valor!.Value, request.Descricao);
        return await RegistrarAsync(transacao, origem, ct);
    }

    /// <summary>
    /// Extrato no período [inicio, fim], em UTC. Sem período, usa os últimos 30 dias.
    /// </summary>
    public async Task<ExtratoResponse> ObterExtratoAsync(int contaId, DateTime? inicio, DateTime? fim, CancellationToken ct = default)
    {
        var conta = await ObterContaAsync(contaId, ct);

        var fimPeriodo = fim is null ? DateTime.UtcNow : ParaUtc(fim.Value);
        var inicioPeriodo = inicio is null ? fimPeriodo.AddDays(-DiasPadraoDoExtrato) : ParaUtc(inicio.Value);
        if (inicioPeriodo > fimPeriodo)
            throw new RegraDeNegocioException("A data de início deve ser anterior à data de fim.");

        var lista = await transacoes.ListarExtratoAsync(contaId, inicioPeriodo, fimPeriodo, ct);
        var itens = lista.Select(t => new ExtratoItemResponse(
            t.Id,
            t.Tipo,
            SentidoPara(t, contaId),
            t.Valor,
            t.DataHora,
            t.Descricao,
            t.ContaId,
            t.ContaDestinoId)).ToList();

        return new ExtratoResponse(conta.Id, conta.Saldo, inicioPeriodo, fimPeriodo, itens);
    }

    /// <summary>
    /// O ASP.NET converte datas com "Z" para o fuso do servidor (Kind = Local); voltamos para UTC.
    /// Datas sem fuso (Kind = Unspecified) são tratadas como UTC, que é o padrão da API.
    /// </summary>
    private static DateTime ParaUtc(DateTime data) => data.Kind switch
    {
        DateTimeKind.Local => data.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(data, DateTimeKind.Utc),
        _ => data
    };

    private static SentidoMovimentacao SentidoPara(Transacao t, int contaId) => t.Tipo switch
    {
        TipoTransacao.Deposito => SentidoMovimentacao.Entrada,
        TipoTransacao.Saque => SentidoMovimentacao.Saida,
        _ => t.ContaDestinoId == contaId ? SentidoMovimentacao.Entrada : SentidoMovimentacao.Saida
    };

    private async Task<MovimentacaoResponse> RegistrarAsync(Transacao transacao, Conta conta, CancellationToken ct)
    {
        transacoes.Adicionar(transacao);
        await unidadeDeTrabalho.SalvarAsync(ct);

        return new MovimentacaoResponse(
            transacao.Id, transacao.Tipo, transacao.Valor, transacao.DataHora,
            transacao.Descricao, transacao.ContaDestinoId, conta.Saldo);
    }

    private async Task<Conta> ObterContaAsync(int id, CancellationToken ct) =>
        await contas.ObterPorIdAsync(id, ct)
        ?? throw new RecursoNaoEncontradoException($"Conta {id} não encontrada.");
}
