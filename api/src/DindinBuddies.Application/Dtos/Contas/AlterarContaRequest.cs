using System.ComponentModel.DataAnnotations;
using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Application.Dtos.Contas;

/// <summary>
/// Dados editáveis da conta. Agência, número e saldo não mudam por edição.
/// </summary>
public class AlterarContaRequest
{
    [Required(ErrorMessage = "O tipo de conta é obrigatório.")]
    [EnumDataType(typeof(TipoConta), ErrorMessage = "Tipo de conta inválido.")]
    public TipoConta? TipoConta { get; set; }
}
