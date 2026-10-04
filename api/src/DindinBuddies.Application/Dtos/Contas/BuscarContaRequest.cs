using System.ComponentModel.DataAnnotations;

namespace DindinBuddies.Application.Dtos.Contas;

/// <summary>
/// Filtro da busca de conta por agência e número (query string).
/// </summary>
public class BuscarContaRequest
{
    [Required(ErrorMessage = "A agência é obrigatória.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "A agência deve ter 4 dígitos.")]
    public string Agencia { get; set; } = null!;

    [Required(ErrorMessage = "O número da conta é obrigatório.")]
    [RegularExpression(@"^\d{1,6}$", ErrorMessage = "O número da conta deve ter até 6 dígitos.")]
    public string Numero { get; set; } = null!;
}
