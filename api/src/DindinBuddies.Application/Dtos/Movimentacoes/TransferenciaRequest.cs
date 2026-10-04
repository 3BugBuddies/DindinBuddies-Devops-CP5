using System.ComponentModel.DataAnnotations;

namespace DindinBuddies.Application.Dtos.Movimentacoes;

public class TransferenciaRequest
{
    [Required(ErrorMessage = "A conta de destino é obrigatória.")]
    public int? ContaDestinoId { get; set; }

    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal? Valor { get; set; }

    [MaxLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
    public string? Descricao { get; set; }
}
