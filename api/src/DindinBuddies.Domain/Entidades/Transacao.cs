using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Domain.Entidades;

/// <summary>
/// Movimentação de uma conta. Criada somente pelos métodos de <see cref="Conta"/>,
/// que atualizam o saldo junto.
/// </summary>
public class Transacao
{
    public long Id { get; private set; }
    public int ContaId { get; private set; }
    public Conta Conta { get; private set; } = null!;
    public TipoTransacao Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? Descricao { get; private set; }
    public int? ContaDestinoId { get; private set; }
    public Conta? ContaDestino { get; private set; }

    // Usado pelo EF Core.
    private Transacao() { }

    private Transacao(Conta conta, TipoTransacao tipo, decimal valor, string? descricao, Conta? contaDestino = null)
    {
        Conta = conta;
        ContaId = conta.Id;
        Tipo = tipo;
        Valor = valor;
        DataHora = DateTime.UtcNow;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        ContaDestino = contaDestino;
        ContaDestinoId = contaDestino?.Id;
    }

    internal static Transacao Deposito(Conta conta, decimal valor, string? descricao) =>
        new(conta, TipoTransacao.Deposito, valor, descricao);

    internal static Transacao Saque(Conta conta, decimal valor, string? descricao) =>
        new(conta, TipoTransacao.Saque, valor, descricao);

    internal static Transacao Transferencia(Conta origem, Conta destino, decimal valor, string? descricao) =>
        new(origem, TipoTransacao.Transferencia, valor, descricao, destino);
}
