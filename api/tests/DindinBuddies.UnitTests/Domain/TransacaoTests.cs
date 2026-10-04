using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;
using DindinBuddies.Domain.Excecoes;

namespace DindinBuddies.UnitTests.Domain;

public class TransacaoTests
{
    private static Conta NovaConta(decimal saldo = 0)
    {
        var conta = new Conta(clienteId: 1, agencia: "0001", numeroConta: "000001", TipoConta.Corrente);
        if (saldo > 0)
            conta.Depositar(saldo);
        return conta;
    }

    [Fact]
    public void AlterarDescricao_TrocaApenasADescricao()
    {
        var conta = NovaConta(saldo: 100);
        var transacao = conta.Sacar(30, "Antiga");

        transacao.AlterarDescricao("  Nova  ");

        Assert.Equal("Nova", transacao.Descricao);
        Assert.Equal(30m, transacao.Valor);
        Assert.Equal(TipoTransacao.Saque, transacao.Tipo);
    }

    [Fact]
    public void AlterarDescricao_Vazia_RemoveADescricao()
    {
        var transacao = NovaConta().Depositar(10, "Algo");

        transacao.AlterarDescricao("   ");

        Assert.Null(transacao.Descricao);
    }

    [Fact]
    public void Estornar_Deposito_TiraOValorDaConta()
    {
        var conta = NovaConta(saldo: 50);
        var deposito = conta.Depositar(100);

        deposito.Estornar();

        Assert.Equal(50m, conta.Saldo);
    }

    [Fact]
    public void Estornar_DepositoJaGasto_LancaRegraDeNegocioESaldoNaoMuda()
    {
        var conta = NovaConta();
        var deposito = conta.Depositar(100);
        conta.Sacar(80);

        Assert.Throws<RegraDeNegocioException>(() => deposito.Estornar());
        Assert.Equal(20m, conta.Saldo);
    }

    [Fact]
    public void Estornar_Saque_DevolveOValorAConta()
    {
        var conta = NovaConta(saldo: 100);
        var saque = conta.Sacar(40);

        saque.Estornar();

        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void Estornar_Transferencia_DevolveAOrigemETiraDoDestino()
    {
        var origem = NovaConta(saldo: 100);
        var destino = NovaConta(saldo: 10);
        var transferencia = origem.TransferirPara(destino, 30);

        transferencia.Estornar();

        Assert.Equal(100m, origem.Saldo);
        Assert.Equal(10m, destino.Saldo);
    }

    [Fact]
    public void Estornar_TransferenciaComDestinoSemSaldo_LancaRegraDeNegocioENenhumSaldoMuda()
    {
        var origem = NovaConta(saldo: 100);
        var destino = NovaConta();
        var transferencia = origem.TransferirPara(destino, 30);
        destino.Sacar(25);

        Assert.Throws<RegraDeNegocioException>(() => transferencia.Estornar());
        Assert.Equal(70m, origem.Saldo);
        Assert.Equal(5m, destino.Saldo);
    }

    [Fact]
    public void Estornar_ContaEncerrada_LancaRegraDeNegocio()
    {
        var conta = NovaConta(saldo: 10);
        var saque = conta.Sacar(10);
        conta.Encerrar();

        Assert.Throws<RegraDeNegocioException>(() => saque.Estornar());
        Assert.Equal(0m, conta.Saldo);
    }
}
