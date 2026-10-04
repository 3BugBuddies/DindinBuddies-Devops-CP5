using DindinBuddies.Application.Dtos.Movimentacoes;
using DindinBuddies.Application.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace DindinBuddies.Api.Controllers;

[ApiController]
[Route("api/contas/{contaId:int}")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class MovimentacoesController(MovimentacaoServico movimentacoes) : ControllerBase
{
    [HttpPost("depositos")]
    [ProducesResponseType<MovimentacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<MovimentacaoResponse> Depositar(int contaId, MovimentacaoRequest request, CancellationToken ct) =>
        movimentacoes.DepositarAsync(contaId, request, ct);

    [HttpPost("saques")]
    [ProducesResponseType<MovimentacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<MovimentacaoResponse> Sacar(int contaId, MovimentacaoRequest request, CancellationToken ct) =>
        movimentacoes.SacarAsync(contaId, request, ct);

    /// <summary>Transfere da conta da rota para a conta de destino, em uma única transação do banco.</summary>
    [HttpPost("transferencias")]
    [ProducesResponseType<MovimentacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<MovimentacaoResponse> Transferir(int contaId, TransferenciaRequest request, CancellationToken ct) =>
        movimentacoes.TransferirAsync(contaId, request, ct);

    /// <summary>Extrato do período, em UTC. Sem datas, retorna os últimos 30 dias.</summary>
    [HttpGet("extrato")]
    [ProducesResponseType<ExtratoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ExtratoResponse> Extrato(int contaId, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken ct) =>
        movimentacoes.ObterExtratoAsync(contaId, inicio, fim, ct);
}
