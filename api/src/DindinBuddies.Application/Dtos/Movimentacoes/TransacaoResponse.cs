using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Application.Dtos.Movimentacoes;

public record TransacaoResponse(
    long Id,
    int ContaId,
    TipoTransacao Tipo,
    decimal Valor,
    DateTime DataHora,
    string? Descricao,
    int? ContaDestinoId)
{
    public static TransacaoResponse De(Transacao t) =>
        new(t.Id, t.ContaId, t.Tipo, t.Valor, t.DataHora, t.Descricao, t.ContaDestinoId);
}
