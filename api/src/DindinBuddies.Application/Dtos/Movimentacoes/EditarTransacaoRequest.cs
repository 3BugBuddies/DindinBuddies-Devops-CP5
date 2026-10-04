using System.ComponentModel.DataAnnotations;

namespace DindinBuddies.Application.Dtos.Movimentacoes;

/// <summary>
/// Só a descrição da transação é editável. Vazia ou nula remove a descrição.
/// </summary>
public class EditarTransacaoRequest
{
    [MaxLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
    public string? Descricao { get; set; }
}
