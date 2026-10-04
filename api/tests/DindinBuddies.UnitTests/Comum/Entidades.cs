using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;

namespace DindinBuddies.UnitTests.Comum;

/// <summary>
/// Fábricas de entidades para os testes. O Id normalmente é gerado pelo banco,
/// então aqui ele é definido por reflexão quando o teste precisa dele.
/// </summary>
internal static class Entidades
{
    public static Cliente Cliente(int id = 1) =>
        ComId(new Cliente("Ana Souza", "12345678901", "ana@email.com", null, new DateOnly(1990, 5, 20)), id);

    public static Conta Conta(int id = 1, decimal saldo = 0)
    {
        var conta = ComId(new Conta(clienteId: 1, "0001", id.ToString("D6"), TipoConta.Corrente), id);
        if (saldo > 0)
            conta.Depositar(saldo);
        return conta;
    }

    private static T ComId<T>(T entidade, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entidade, id);
        return entidade;
    }
}
