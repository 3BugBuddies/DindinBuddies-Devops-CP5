using System.ComponentModel.DataAnnotations;

namespace DindinBuddies.Application.Dtos.Movimentacoes;

/// <summary>
/// Entrada de depósito e de saque.
/// </summary>
public class MovimentacaoRequest
{
    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal? Valor { get; set; }

    [MaxLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
    public string? Descricao { get; set; }
}
