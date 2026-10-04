using System.ComponentModel.DataAnnotations;

namespace DindinBuddies.Application.Dtos.Clientes;

/// <summary>
/// Dados editáveis do cliente. O CPF não muda depois do cadastro.
/// </summary>
public class EditarClienteRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
    public string Nome { get; set; } = null!;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail é inválido.")]
    [MaxLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = null!;

    [MaxLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    public DateOnly? DataNascimento { get; set; }
}
