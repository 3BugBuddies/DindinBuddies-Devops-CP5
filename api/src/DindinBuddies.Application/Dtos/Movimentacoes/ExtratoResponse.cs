using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Application.Dtos.Movimentacoes;

public record ExtratoResponse(
    int ContaId,
    decimal SaldoAtual,
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<ExtratoItemResponse> Itens);

public record ExtratoItemResponse(
    long TransacaoId,
    TipoTransacao Tipo,
    SentidoMovimentacao Sentido,
    decimal Valor,
    DateTime DataHora,
    string? Descricao,
    int ContaOrigemId,
    int? ContaDestinoId);
