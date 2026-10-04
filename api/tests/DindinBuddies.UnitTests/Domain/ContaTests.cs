using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;
using DindinBuddies.Domain.Excecoes;

namespace DindinBuddies.UnitTests.Domain;

public class ContaTests
{
    private static Conta NovaConta(decimal saldoInicial = 0)
    {
        var conta = new Conta(clienteId: 1, agencia: "0001", numeroConta: "000001", TipoConta.Corrente);
        if (saldoInicial > 0)
            conta.Depositar(saldoInicial);
        return conta;
    }

    [Fact]
    public void Construtor_ContaNova_ComecaAtivaComSaldoZero()
    {
        var conta = NovaConta();

        Assert.True(conta.Ativa);
        Assert.Equal(0m, conta.Saldo);
        Assert.Equal(DateTimeKind.Utc, conta.DataAbertura.Kind);
    }

    [Fact]
    public void Construtor_TipoContaInvalido_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => new Conta(1, "0001", "000001", (TipoConta)99));
    }

    [Fact]
    public void Depositar_ValorValido_SomaAoSaldoEGeraTransacaoDeDeposito()
    {
        var conta = NovaConta(saldoInicial: 50);

        var transacao = conta.Depositar(25.50m, "  Salário  ");

        Assert.Equal(75.50m, conta.Saldo);
        Assert.Equal(TipoTransacao.Deposito, transacao.Tipo);
        Assert.Equal(25.50m, transacao.Valor);
        Assert.Equal("Salário", transacao.Descricao);
        Assert.Same(conta, transacao.Conta);
        Assert.Null(transacao.ContaDestinoId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Depositar_ValorNaoPositivo_LancaRegraDeNegocioESaldoNaoMuda(decimal valor)
    {
        var conta = NovaConta(saldoInicial: 100);

        Assert.Throws<RegraDeNegocioException>(() => conta.Depositar(valor));
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void Depositar_ContaEncerrada_LancaRegraDeNegocio()
    {
        var conta = NovaConta();
        conta.Encerrar();

        Assert.Throws<RegraDeNegocioException>(() => conta.Depositar(10));
    }

    [Fact]
    public void Sacar_ComSaldoSuficiente_SubtraiDoSaldoEGeraTransacaoDeSaque()
    {
        var conta = NovaConta(saldoInicial: 100);

        var transacao = conta.Sacar(40);

        Assert.Equal(60m, conta.Saldo);
        Assert.Equal(TipoTransacao.Saque, transacao.Tipo);
        Assert.Equal(40m, transacao.Valor);
    }

    [Fact]
    public void Sacar_SaldoInteiro_DeixaSaldoZero()
    {
        var conta = NovaConta(saldoInicial: 100);

        conta.Sacar(100);

        Assert.Equal(0m, conta.Saldo);
    }

    [Fact]
    public void Sacar_ComSaldoInsuficiente_LancaRegraDeNegocioESaldoNaoMuda()
    {
        var conta = NovaConta(saldoInicial: 100);

        var ex = Assert.Throws<RegraDeNegocioException>(() => conta.Sacar(100.01m));
        Assert.Equal("Saldo insuficiente.", ex.Message);
        Assert.Equal(100m, conta.Saldo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Sacar_ValorNaoPositivo_LancaRegraDeNegocio(decimal valor)
    {
        var conta = NovaConta(saldoInicial: 100);

        Assert.Throws<RegraDeNegocioException>(() => conta.Sacar(valor));
    }

    [Fact]
    public void Sacar_ContaEncerrada_LancaRegraDeNegocio()
    {
        var conta = NovaConta();
        conta.Encerrar();

        Assert.Throws<RegraDeNegocioException>(() => conta.Sacar(10));
    }

    [Fact]
    public void TransferirPara_ComSaldoSuficiente_DebitaOrigemCreditaDestinoEGeraUmaTransacao()
    {
        var origem = NovaConta(saldoInicial: 100);
        var destino = NovaConta(saldoInicial: 10);

        var transacao = origem.TransferirPara(destino, 30, "Aluguel");

        Assert.Equal(70m, origem.Saldo);
        Assert.Equal(40m, destino.Saldo);
        Assert.Equal(TipoTransacao.Transferencia, transacao.Tipo);
        Assert.Same(origem, transacao.Conta);
        Assert.Same(destino, transacao.ContaDestino);
        Assert.Equal(30m, transacao.Valor);
    }

    [Fact]
    public void TransferirPara_ComSaldoInsuficiente_LancaRegraDeNegocioENenhumSaldoMuda()
    {
        var origem = NovaConta(saldoInicial: 20);
        var destino = NovaConta(saldoInicial: 10);

        Assert.Throws<RegraDeNegocioException>(() => origem.TransferirPara(destino, 50));
        Assert.Equal(20m, origem.Saldo);
        Assert.Equal(10m, destino.Saldo);
    }

    [Fact]
    public void TransferirPara_MesmaConta_LancaRegraDeNegocio()
    {
        var conta = NovaConta(saldoInicial: 100);

        Assert.Throws<RegraDeNegocioException>(() => conta.TransferirPara(conta, 10));
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void TransferirPara_DestinoEncerrado_LancaRegraDeNegocioESaldoDaOrigemNaoMuda()
    {
        var origem = NovaConta(saldoInicial: 100);
        var destino = NovaConta();
        destino.Encerrar();

        Assert.Throws<RegraDeNegocioException>(() => origem.TransferirPara(destino, 10));
        Assert.Equal(100m, origem.Saldo);
    }

    [Fact]
    public void TransferirPara_OrigemEncerrada_LancaRegraDeNegocio()
    {
        var origem = NovaConta();
        origem.Encerrar();
        var destino = NovaConta();

        Assert.Throws<RegraDeNegocioException>(() => origem.TransferirPara(destino, 10));
    }

    [Fact]
    public void TransferirPara_ValorNaoPositivo_LancaRegraDeNegocio()
    {
        var origem = NovaConta(saldoInicial: 100);
        var destino = NovaConta();

        Assert.Throws<RegraDeNegocioException>(() => origem.TransferirPara(destino, 0));
    }

    [Fact]
    public void AlterarTipo_ContaAtiva_TrocaOTipo()
    {
        var conta = NovaConta();

        conta.AlterarTipo(TipoConta.Poupanca);

        Assert.Equal(TipoConta.Poupanca, conta.TipoConta);
    }

    [Fact]
    public void AlterarTipo_TipoInvalido_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => NovaConta().AlterarTipo((TipoConta)99));
    }

    [Fact]
    public void AlterarTipo_ContaEncerrada_LancaRegraDeNegocio()
    {
        var conta = NovaConta();
        conta.Encerrar();

        Assert.Throws<RegraDeNegocioException>(() => conta.AlterarTipo(TipoConta.Poupanca));
    }

    [Fact]
    public void Encerrar_ComSaldoZero_DesativaConta()
    {
        var conta = NovaConta();

        conta.Encerrar();

        Assert.False(conta.Ativa);
    }

    [Fact]
    public void Encerrar_ComSaldo_LancaRegraDeNegocioEContaContinuaAtiva()
    {
        var conta = NovaConta(saldoInicial: 0.01m);

        Assert.Throws<RegraDeNegocioException>(() => conta.Encerrar());
        Assert.True(conta.Ativa);
    }

    [Fact]
    public void Encerrar_ContaJaEncerrada_LancaRegraDeNegocio()
    {
        var conta = NovaConta();
        conta.Encerrar();

        Assert.Throws<RegraDeNegocioException>(() => conta.Encerrar());
    }
}
