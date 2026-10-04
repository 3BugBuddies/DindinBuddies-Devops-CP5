using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.UnitTests.Domain;

public class ClienteTests
{
    private static readonly DateOnly Nascimento = new(1990, 5, 20);

    [Fact]
    public void Construtor_NormalizaNomeEmailETelefone()
    {
        var cliente = new Cliente("  Ana Souza ", "12345678901", "  Ana@Email.COM ", "   ", Nascimento);

        Assert.Equal("Ana Souza", cliente.Nome);
        Assert.Equal("ana@email.com", cliente.Email);
        Assert.Null(cliente.Telefone);
        Assert.Equal("12345678901", cliente.Cpf);
        Assert.Equal(Nascimento, cliente.DataNascimento);
        Assert.Equal(DateTimeKind.Utc, cliente.DataCadastro.Kind);
    }

    [Fact]
    public void Atualizar_TrocaDadosEMantemCpfEDataCadastro()
    {
        var cliente = new Cliente("Ana", "12345678901", "ana@email.com", null, Nascimento);
        var dataCadastro = cliente.DataCadastro;

        cliente.Atualizar("Ana Lima", "ANA.LIMA@email.com", " 11 99999-0000 ", new DateOnly(1991, 1, 1));

        Assert.Equal("Ana Lima", cliente.Nome);
        Assert.Equal("ana.lima@email.com", cliente.Email);
        Assert.Equal("11 99999-0000", cliente.Telefone);
        Assert.Equal(new DateOnly(1991, 1, 1), cliente.DataNascimento);
        Assert.Equal("12345678901", cliente.Cpf);
        Assert.Equal(dataCadastro, cliente.DataCadastro);
    }
}
