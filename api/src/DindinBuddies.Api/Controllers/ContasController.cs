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

    /// <summary>Busca a conta pela agência e pelo número (ex.: agencia=0001&amp;numero=000042).</summary>
    [HttpGet("busca")]
    [ProducesResponseType<ContaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ContaResponse> Buscar([FromQuery] BuscarContaRequest filtro, CancellationToken ct) =>
        contas.ObterPorNumeroAsync(filtro.Agencia, filtro.Numero, ct);

    /// <summary>Encerra a conta. Só é permitido com saldo zero.</summary>
    [HttpPost("{id:int}/encerrar")]
    [ProducesResponseType<ContaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ContaResponse> Encerrar(int id, CancellationToken ct) =>
        contas.EncerrarAsync(id, ct);
}
