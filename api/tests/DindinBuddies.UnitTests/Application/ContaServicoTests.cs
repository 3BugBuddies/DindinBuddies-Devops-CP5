using DindinBuddies.Application.Dtos.Contas;
using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using DindinBuddies.Application.Servicos;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Enums;
using DindinBuddies.Domain.Excecoes;
using DindinBuddies.UnitTests.Comum;
using NSubstitute;

namespace DindinBuddies.UnitTests.Application;

public class ContaServicoTests
{
    private readonly IClienteRepositorio _clientes = Substitute.For<IClienteRepositorio>();
    private readonly IContaRepositorio _contas = Substitute.For<IContaRepositorio>();
    private readonly ITransacaoRepositorio _transacoes = Substitute.For<ITransacaoRepositorio>();
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho = Substitute.For<IUnidadeDeTrabalho>();
    private readonly ContaServico _servico;

    private static readonly AbrirContaRequest Poupanca = new() { TipoConta = TipoConta.Poupanca };

    public ContaServicoTests()
    {
        _servico = new ContaServico(_clientes, _contas, _transacoes, _unidadeDeTrabalho);
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Entidades.Cliente(id: 1));
    }

    [Fact]
    public async Task AbrirAsync_PrimeiraConta_RecebeAgencia0001ENumero000001()
    {
        _contas.ObterMaiorNumeroAsync("0001", Arg.Any<CancellationToken>()).Returns((string?)null);

        var conta = await _servico.AbrirAsync(1, Poupanca);

        Assert.Equal("0001", conta.Agencia);
        Assert.Equal("000001", conta.NumeroConta);
        Assert.Equal(TipoConta.Poupanca, conta.TipoConta);
        Assert.Equal(0m, conta.Saldo);
        Assert.True(conta.Ativa);
        _contas.Received(1).Adicionar(Arg.Any<Conta>());
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AbrirAsync_JaExistemContas_RecebeProximoNumero()
    {
        _contas.ObterMaiorNumeroAsync("0001", Arg.Any<CancellationToken>()).Returns("000041");

        var conta = await _servico.AbrirAsync(1, Poupanca);

        Assert.Equal("000042", conta.NumeroConta);
    }

    [Fact]
    public async Task AbrirAsync_ConflitoNaPrimeiraTentativa_CalculaNovoNumeroETentaDeNovo()
    {
        // Outra abertura simultânea pegou o 000002 antes; na segunda tentativa o maior já é 000002.
        _contas.ObterMaiorNumeroAsync("0001", Arg.Any<CancellationToken>()).Returns("000001", "000002");
        _unidadeDeTrabalho.SalvarAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConflitoDeDadosException("duplicado")), Task.CompletedTask);

        var conta = await _servico.AbrirAsync(1, Poupanca);

        Assert.Equal("000003", conta.NumeroConta);
        _contas.Received(2).Adicionar(Arg.Any<Conta>());
        await _unidadeDeTrabalho.Received(2).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AbrirAsync_ConflitoEmTodasAsTentativas_RepassaConflito()
    {
        _contas.ObterMaiorNumeroAsync("0001", Arg.Any<CancellationToken>()).Returns((string?)null);
        _unidadeDeTrabalho.SalvarAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConflitoDeDadosException("duplicado")));

        await Assert.ThrowsAsync<ConflitoDeDadosException>(() => _servico.AbrirAsync(1, Poupanca));

        await _unidadeDeTrabalho.Received(3).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AbrirAsync_ClienteInexistente_LancaRecursoNaoEncontradoENaoAdiciona()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.AbrirAsync(99, Poupanca));

        _contas.DidNotReceiveWithAnyArgs().Adicionar(default!);
    }

    [Fact]
    public async Task ListarPorClienteAsync_ClienteInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ListarPorClienteAsync(99));
    }

    [Fact]
    public async Task EncerrarAsync_SaldoZero_EncerraESalva()
    {
        var conta = Entidades.Conta(id: 7);
        _contas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(conta);

        var resposta = await _servico.EncerrarAsync(7);

        Assert.False(resposta.Ativa);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EncerrarAsync_ComSaldo_LancaRegraDeNegocioENaoSalva()
    {
        _contas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(Entidades.Conta(id: 7, saldo: 10));

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.EncerrarAsync(7));

        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task AlterarAsync_ContaAtiva_TrocaTipoESalva()
    {
        _contas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(Entidades.Conta(id: 7));

        var resposta = await _servico.AlterarAsync(7, new AlterarContaRequest { TipoConta = TipoConta.Poupanca });

        Assert.Equal(TipoConta.Poupanca, resposta.TipoConta);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlterarAsync_ContaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => _servico.AlterarAsync(99, new AlterarContaRequest { TipoConta = TipoConta.Poupanca }));
    }

    [Fact]
    public async Task ExcluirAsync_ContaSemMovimentacoes_RemoveESalva()
    {
        var conta = Entidades.Conta(id: 7);
        _contas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(conta);

        await _servico.ExcluirAsync(7);

        _contas.Received(1).Remover(conta);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExcluirAsync_ContaComMovimentacoes_LancaRegraDeNegocioENaoRemove()
    {
        _contas.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(Entidades.Conta(id: 7));
        _transacoes.ExisteParaContaAsync(7, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.ExcluirAsync(7));

        _contas.DidNotReceiveWithAnyArgs().Remover(default!);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task ExcluirAsync_ContaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ExcluirAsync(99));
    }

    [Fact]
    public async Task ObterPorNumeroAsync_NumeroSemZerosAEsquerda_CompletaPara6DigitosEEncontra()
    {
        _contas.ObterPorNumeroAsync("0001", "000042", Arg.Any<CancellationToken>()).Returns(Entidades.Conta(id: 42));

        var conta = await _servico.ObterPorNumeroAsync("0001", "42");

        Assert.Equal(42, conta.Id);
        Assert.Equal("000042", conta.NumeroConta);
    }

    [Fact]
    public async Task ObterPorNumeroAsync_ContaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ObterPorNumeroAsync("0001", "999999"));
    }

    [Fact]
    public async Task ObterAsync_ContaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ObterAsync(99));
    }
}
