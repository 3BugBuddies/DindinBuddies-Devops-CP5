using DindinBuddies.Application.Dtos.Movimentacoes;
using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using DindinBuddies.Application.Servicos;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;
using DindinBuddies.Domain.Excecoes;
using DindinBuddies.UnitTests.Comum;
using NSubstitute;

namespace DindinBuddies.UnitTests.Application;

public class MovimentacaoServicoTests
{
    private readonly IContaRepositorio _contas = Substitute.For<IContaRepositorio>();
    private readonly ITransacaoRepositorio _transacoes = Substitute.For<ITransacaoRepositorio>();
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho = Substitute.For<IUnidadeDeTrabalho>();
    private readonly MovimentacaoServico _servico;

    public MovimentacaoServicoTests() => _servico = new MovimentacaoServico(_contas, _transacoes, _unidadeDeTrabalho);

    private Conta ContaCadastrada(int id, decimal saldo = 0)
    {
        var conta = Entidades.Conta(id, saldo);
        _contas.ObterPorIdAsync(id, Arg.Any<CancellationToken>()).Returns(conta);
        return conta;
    }

    [Fact]
    public async Task DepositarAsync_ValorValido_RegistraTransacaoSalvaERetornaSaldoAtual()
    {
        ContaCadastrada(1, saldo: 100);

        var resposta = await _servico.DepositarAsync(1, new MovimentacaoRequest { Valor = 50, Descricao = "Pix" });

        Assert.Equal(150m, resposta.SaldoAtual);
        Assert.Equal(TipoTransacao.Deposito, resposta.Tipo);
        Assert.Equal(50m, resposta.Valor);
        _transacoes.Received(1).Adicionar(Arg.Is<Transacao>(t => t.Tipo == TipoTransacao.Deposito && t.Valor == 50));
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DepositarAsync_ContaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => _servico.DepositarAsync(99, new MovimentacaoRequest { Valor = 10 }));
    }

    [Fact]
    public async Task SacarAsync_ComSaldo_RegistraTransacaoERetornaSaldoAtual()
    {
        ContaCadastrada(1, saldo: 100);

        var resposta = await _servico.SacarAsync(1, new MovimentacaoRequest { Valor = 30 });

        Assert.Equal(70m, resposta.SaldoAtual);
        Assert.Equal(TipoTransacao.Saque, resposta.Tipo);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SacarAsync_SaldoInsuficiente_LancaRegraDeNegocioENaoRegistraNada()
    {
        var conta = ContaCadastrada(1, saldo: 10);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.SacarAsync(1, new MovimentacaoRequest { Valor = 50 }));

        Assert.Equal(10m, conta.Saldo);
        _transacoes.DidNotReceiveWithAnyArgs().Adicionar(default!);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task TransferirAsync_DadosValidos_AtualizaAsDuasContasESalvaUmaVez()
    {
        var origem = ContaCadastrada(1, saldo: 100);
        var destino = ContaCadastrada(2, saldo: 5);

        var resposta = await _servico.TransferirAsync(1, new TransferenciaRequest { ContaDestinoId = 2, Valor = 40 });

        Assert.Equal(60m, origem.Saldo);
        Assert.Equal(45m, destino.Saldo);
        Assert.Equal(60m, resposta.SaldoAtual);
        Assert.Equal(2, resposta.ContaDestinoId);
        _transacoes.Received(1).Adicionar(Arg.Is<Transacao>(t =>
            t.Tipo == TipoTransacao.Transferencia && t.ContaId == 1 && t.ContaDestinoId == 2));
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferirAsync_DestinoInexistente_LancaRegraDeNegocioENaoSalva()
    {
        var origem = ContaCadastrada(1, saldo: 100);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.TransferirAsync(1, new TransferenciaRequest { ContaDestinoId = 99, Valor = 10 }));

        Assert.Equal(100m, origem.Saldo);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task TransferirAsync_ParaAPropriaConta_LancaRegraDeNegocio()
    {
        ContaCadastrada(1, saldo: 100);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.TransferirAsync(1, new TransferenciaRequest { ContaDestinoId = 1, Valor = 10 }));
    }

    [Fact]
    public async Task EditarTransacaoAsync_TrocaSoADescricaoESalva()
    {
        var conta = Entidades.Conta(id: 1, saldo: 100);
        var transacao = conta.Sacar(30, "Antiga");
        _transacoes.ObterPorIdAsync(10, Arg.Any<CancellationToken>()).Returns(transacao);

        var resposta = await _servico.EditarTransacaoAsync(10, new EditarTransacaoRequest { Descricao = " Nova " });

        Assert.Equal("Nova", resposta.Descricao);
        Assert.Equal(30m, resposta.Valor);
        Assert.Equal(70m, conta.Saldo);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExcluirTransacaoAsync_Transferencia_EstornaAsDuasContasRemoveESalvaUmaVez()
    {
        var origem = Entidades.Conta(id: 1, saldo: 100);
        var destino = Entidades.Conta(id: 2);
        var transacao = origem.TransferirPara(destino, 40);
        _transacoes.ObterPorIdAsync(10, Arg.Any<CancellationToken>()).Returns(transacao);

        await _servico.ExcluirTransacaoAsync(10);

        Assert.Equal(100m, origem.Saldo);
        Assert.Equal(0m, destino.Saldo);
        _transacoes.Received(1).Remover(transacao);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExcluirTransacaoAsync_EstornoDeixariaSaldoNegativo_LancaRegraDeNegocioENaoRemove()
    {
        var conta = Entidades.Conta(id: 1);
        var deposito = conta.Depositar(100);
        conta.Sacar(80);
        _transacoes.ObterPorIdAsync(10, Arg.Any<CancellationToken>()).Returns(deposito);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.ExcluirTransacaoAsync(10));

        Assert.Equal(20m, conta.Saldo);
        _transacoes.DidNotReceiveWithAnyArgs().Remover(default!);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task ObterTransacaoAsync_Inexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ObterTransacaoAsync(99));
    }

    [Fact]
    public async Task ObterExtratoAsync_MarcaEntradasESaidasEmRelacaoAConta()
    {
        var conta = ContaCadastrada(5, saldo: 1000);
        var outra = Entidades.Conta(id: 8, saldo: 1000);
        var deposito = conta.Depositar(100);
        var saque = conta.Sacar(30);
        var enviada = conta.TransferirPara(outra, 20);
        var recebida = outra.TransferirPara(conta, 70);
        _transacoes.ListarExtratoAsync(5, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([recebida, enviada, saque, deposito]);

        var extrato = await _servico.ObterExtratoAsync(5, null, null);

        Assert.Equal(conta.Saldo, extrato.SaldoAtual);
        Assert.Collection(extrato.Itens,
            i => Assert.Equal((TipoTransacao.Transferencia, SentidoMovimentacao.Entrada, 8), (i.Tipo, i.Sentido, i.ContaOrigemId)),
            i => Assert.Equal((TipoTransacao.Transferencia, SentidoMovimentacao.Saida, 5), (i.Tipo, i.Sentido, i.ContaOrigemId)),
            i => Assert.Equal((TipoTransacao.Saque, SentidoMovimentacao.Saida), (i.Tipo, i.Sentido)),
            i => Assert.Equal((TipoTransacao.Deposito, SentidoMovimentacao.Entrada), (i.Tipo, i.Sentido)));
    }

    [Fact]
    public async Task ObterExtratoAsync_SemPeriodo_UsaUltimos30Dias()
    {
        ContaCadastrada(5);
        var antes = DateTime.UtcNow;

        var extrato = await _servico.ObterExtratoAsync(5, null, null);

        Assert.InRange(extrato.Fim, antes, DateTime.UtcNow);
        Assert.Equal(extrato.Fim.AddDays(-30), extrato.Inicio);
        await _transacoes.Received(1).ListarExtratoAsync(5, extrato.Inicio, extrato.Fim, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObterExtratoAsync_DatasLocaisOuSemFuso_ConsultaEmUtc()
    {
        ContaCadastrada(5);
        var inicioUtc = new DateTime(2026, 1, 1, 3, 0, 0, DateTimeKind.Utc);
        var inicioLocal = inicioUtc.ToLocalTime();
        var fimSemFuso = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Unspecified);

        var extrato = await _servico.ObterExtratoAsync(5, inicioLocal, fimSemFuso);

        Assert.Equal(inicioUtc, extrato.Inicio);
        Assert.Equal(DateTimeKind.Utc, extrato.Inicio.Kind);
        Assert.Equal(new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc), extrato.Fim);
        Assert.Equal(DateTimeKind.Utc, extrato.Fim.Kind);
    }

    [Fact]
    public async Task ObterExtratoAsync_InicioDepoisDoFim_LancaRegraDeNegocio()
    {
        ContaCadastrada(5);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.ObterExtratoAsync(5, new DateTime(2026, 2, 1), new DateTime(2026, 1, 1)));
    }

    [Fact]
    public async Task ObterExtratoAsync_ContaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ObterExtratoAsync(99, null, null));
    }
}
