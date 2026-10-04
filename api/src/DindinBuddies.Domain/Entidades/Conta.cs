using DindinBuddies.Domain.Enums;
using DindinBuddies.Domain.Excecoes;

namespace DindinBuddies.Domain.Entidades;

public class Conta
{
    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public string Agencia { get; private set; } = null!;
    public string NumeroConta { get; private set; } = null!;
    public TipoConta TipoConta { get; private set; }
    public decimal Saldo { get; private set; }
    public DateTime DataAbertura { get; private set; }
    public bool Ativa { get; private set; }

    // Usado pelo EF Core.
    private Conta() { }

    public Conta(int clienteId, string agencia, string numeroConta, TipoConta tipoConta)
    {
        GarantirTipoValido(tipoConta);

        ClienteId = clienteId;
        Agencia = agencia;
        NumeroConta = numeroConta;
        TipoConta = tipoConta;
        Saldo = 0;
        DataAbertura = DateTime.UtcNow;
        Ativa = true;
    }

    public Transacao Depositar(decimal valor, string? descricao = null)
    {
        GarantirAtiva();
        GarantirValorPositivo(valor);

        Saldo += valor;
        return Transacao.Deposito(this, valor, descricao);
    }

    public Transacao Sacar(decimal valor, string? descricao = null)
    {
        GarantirAtiva();
        GarantirValorPositivo(valor);
        GarantirSaldoSuficiente(valor);

        Saldo -= valor;
        return Transacao.Saque(this, valor, descricao);
    }

    public Transacao TransferirPara(Conta destino, decimal valor, string? descricao = null)
    {
        if (ReferenceEquals(this, destino) || (Id != 0 && Id == destino.Id))
            throw new RegraDeNegocioException("Não é possível transferir para a mesma conta.");

        GarantirAtiva();
        if (!destino.Ativa)
            throw new RegraDeNegocioException("A conta de destino está encerrada.");
        GarantirValorPositivo(valor);
        GarantirSaldoSuficiente(valor);

        Saldo -= valor;
        destino.Saldo += valor;
        return Transacao.Transferencia(this, destino, valor, descricao);
    }

    public void AlterarTipo(TipoConta tipoConta)
    {
        GarantirAtiva();
        GarantirTipoValido(tipoConta);
        TipoConta = tipoConta;
    }

    /// <summary>
    /// Encerra a conta. Só é permitido com saldo zero, para o dinheiro não ficar preso.
    /// </summary>
    public void Encerrar()
    {
        GarantirAtiva();
        if (Saldo != 0)
            throw new RegraDeNegocioException("A conta só pode ser encerrada com saldo zero.");

        Ativa = false;
    }

    // Usados pelo estorno (Transacao.Estornar), que valida as duas contas antes de alterar os saldos.
    internal void GarantirPodeEstornarCredito(decimal valor)
    {
        GarantirAtiva();
        if (valor > Saldo)
            throw new RegraDeNegocioException(
                $"Estorno recusado: a conta {NumeroConta} não tem saldo suficiente para devolver o valor.");
    }

    internal void EstornarCredito(decimal valor) => Saldo -= valor;

    internal void EstornarDebito(decimal valor) => Saldo += valor;

    internal void GarantirAtiva()
    {
        if (!Ativa)
            throw new RegraDeNegocioException("A conta está encerrada.");
    }

    private static void GarantirTipoValido(TipoConta tipoConta)
    {
        if (!Enum.IsDefined(tipoConta))
            throw new RegraDeNegocioException("Tipo de conta inválido.");
    }

    private static void GarantirValorPositivo(decimal valor)
    {
        if (valor <= 0)
            throw new RegraDeNegocioException("O valor deve ser maior que zero.");
    }

    private void GarantirSaldoSuficiente(decimal valor)
    {
        if (valor > Saldo)
            throw new RegraDeNegocioException("Saldo insuficiente.");
    }
}
