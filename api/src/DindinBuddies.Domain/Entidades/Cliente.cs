namespace DindinBuddies.Domain.Entidades;

public class Cliente
{
    private readonly List<Conta> _contas = [];

    public int Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public DateOnly DataNascimento { get; private set; }
    public DateTime DataCadastro { get; private set; }

    public IReadOnlyCollection<Conta> Contas => _contas;

    // Usado pelo EF Core.
    private Cliente() { }

    public Cliente(string nome, string cpf, string email, string? telefone, DateOnly dataNascimento)
    {
        Cpf = cpf;
        DataCadastro = DateTime.UtcNow;
        Atualizar(nome, email, telefone, dataNascimento);
    }

    /// <summary>
    /// Edita os dados cadastrais. O CPF não muda depois do cadastro.
    /// </summary>
    public void Atualizar(string nome, string email, string? telefone, DateOnly dataNascimento)
    {
        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        DataNascimento = dataNascimento;
    }
}
