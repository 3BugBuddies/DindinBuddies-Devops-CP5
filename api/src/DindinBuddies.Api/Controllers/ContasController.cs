using DindinBuddies.Application.Dtos.Contas;
using DindinBuddies.Application.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace DindinBuddies.Api.Controllers;

[ApiController]
[Route("api/contas")]
[Produces("application/json")]
public class ContasController(ContaServico contas) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<ContaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ContaResponse> Obter(int id, CancellationToken ct) =>
        contas.ObterAsync(id, ct);

    /// <summary>Encerra a conta. Só é permitido com saldo zero.</summary>
    [HttpPost("{id:int}/encerrar")]
    [ProducesResponseType<ContaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ContaResponse> Encerrar(int id, CancellationToken ct) =>
        contas.EncerrarAsync(id, ct);
}
