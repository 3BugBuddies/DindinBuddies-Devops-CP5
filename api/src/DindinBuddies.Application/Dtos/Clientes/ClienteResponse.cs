using DindinBuddies.Domain.Entidades;

namespace DindinBuddies.Application.Dtos.Clientes;

public record ClienteResponse(
    int Id,
    string Nome,
    string Cpf,
    string Email,
    string? Telefone,
    DateOnly DataNascimento,
    DateTime DataCadastro)
{
    public static ClienteResponse De(Cliente c) =>
        new(c.Id, c.Nome, c.Cpf, c.Email, c.Telefone, c.DataNascimento, c.DataCadastro);
}
