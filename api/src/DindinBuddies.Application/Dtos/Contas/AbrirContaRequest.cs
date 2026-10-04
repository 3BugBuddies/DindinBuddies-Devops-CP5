using System.ComponentModel.DataAnnotations;
using DindinBuddies.Domain.Enums;

namespace DindinBuddies.Application.Dtos.Contas;

public class AbrirContaRequest
{
    [Required(ErrorMessage = "O tipo de conta é obrigatório.")]
    [EnumDataType(typeof(TipoConta), ErrorMessage = "Tipo de conta inválido.")]
    public TipoConta? TipoConta { get; set; }
}
