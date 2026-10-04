using DindinBuddies.Application.Dtos.Clientes;
using DindinBuddies.Application.Excecoes;
using DindinBuddies.Application.Interfaces;
using DindinBuddies.Application.Servicos;
using DindinBuddies.Domain.Entidades;
using DindinBuddies.Domain.Excecoes;
using DindinBuddies.UnitTests.Comum;
using NSubstitute;

namespace DindinBuddies.UnitTests.Application;

public class ClienteServicoTests
{
    private readonly IClienteRepositorio _clientes = Substitute.For<IClienteRepositorio>();
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho = Substitute.For<IUnidadeDeTrabalho>();
    private readonly ClienteServico _servico;

    public ClienteServicoTests() => _servico = new ClienteServico(_clientes, _unidadeDeTrabalho);

    private static CriarClienteRequest NovoCliente() => new()
    {
        Nome = "Ana Souza",
        Cpf = "12345678901",
        Email = " Ana@Email.com ",
        DataNascimento = new DateOnly(1990, 5, 20)
    };

    private static EditarClienteRequest Edicao() => new()
    {
        Nome = "Ana Lima",
        Email = "ANA.LIMA@email.com",
        DataNascimento = new DateOnly(1990, 5, 20)
    };

    [Fact]
    public async Task CriarAsync_DadosValidos_AdicionaSalvaERetornaCliente()
    {
        var resposta = await _servico.CriarAsync(NovoCliente());

        _clientes.Received(1).Adicionar(Arg.Is<Cliente>(c => c.Cpf == "12345678901" && c.Email == "ana@email.com"));
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Ana Souza", resposta.Nome);
        Assert.Equal("ana@email.com", resposta.Email);
    }

    [Fact]
    public async Task CriarAsync_CpfDuplicado_LancaRegraDeNegocioENaoSalva()
    {
        _clientes.CpfExisteAsync("12345678901", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.CriarAsync(NovoCliente()));

        Assert.Contains("CPF", ex.Message);
        _clientes.DidNotReceiveWithAnyArgs().Adicionar(default!);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task CriarAsync_EmailDuplicado_ConsultaEmailNormalizadoELancaRegraDeNegocio()
    {
        _clientes.EmailExisteAsync("ana@email.com", null, Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.CriarAsync(NovoCliente()));

        Assert.Contains("e-mail", ex.Message);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task ObterAsync_ClienteInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ObterAsync(99));
    }

    [Fact]
    public async Task EditarAsync_DadosValidos_AtualizaESalva()
    {
        var cliente = Entidades.Cliente(id: 1);
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(cliente);

        var resposta = await _servico.EditarAsync(1, Edicao());

        Assert.Equal("Ana Lima", cliente.Nome);
        Assert.Equal("ana.lima@email.com", resposta.Email);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarAsync_EmailDeOutroCliente_LancaRegraDeNegocioENaoSalva()
    {
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Entidades.Cliente(id: 1));
        // O próprio cliente é ignorado na checagem: só conta se o e-mail for de outro.
        _clientes.EmailExisteAsync("ana.lima@email.com", 1, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.EditarAsync(1, Edicao()));

        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task EditarAsync_ClienteInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.EditarAsync(99, Edicao()));
    }

    [Fact]
    public async Task ExcluirAsync_ClienteSemContas_RemoveESalva()
    {
        var cliente = Entidades.Cliente(id: 1);
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(cliente);

        await _servico.ExcluirAsync(1);

        _clientes.Received(1).Remover(cliente);
        await _unidadeDeTrabalho.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExcluirAsync_ClienteComContas_LancaRegraDeNegocioENaoRemove()
    {
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Entidades.Cliente(id: 1));
        _clientes.PossuiContasAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.ExcluirAsync(1));

        _clientes.DidNotReceiveWithAnyArgs().Remover(default!);
        await _unidadeDeTrabalho.DidNotReceiveWithAnyArgs().SalvarAsync(default);
    }

    [Fact]
    public async Task ExcluirAsync_ClienteInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.ExcluirAsync(99));
    }
}
