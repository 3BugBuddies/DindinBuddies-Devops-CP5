using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Application.Dtos.Contas;

public record ContaResponse(
    int Id,
    int ClienteId,
    string Agencia,
    string NumeroConta,
    TipoConta TipoConta,
    decimal Saldo,
    DateTime DataAbertura,
    bool Ativa)
{
    public static ContaResponse De(Conta c) =>
        new(c.Id, c.ClienteId, c.Agencia, c.NumeroConta, c.TipoConta, c.Saldo, c.DataAbertura, c.Ativa);
}
