using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Application.Dtos.Movimentacoes;

/// <summary>
/// Resultado de um depósito, saque ou transferência, com o saldo da conta já atualizado.
/// </summary>
public record MovimentacaoResponse(
    long TransacaoId,
    TipoTransacao Tipo,
    decimal Valor,
    DateTime DataHora,
    string? Descricao,
    int? ContaDestinoId,
    decimal SaldoAtual);
