using DindinBuddies.Application.Dtos.Movimentacoes;
using DindinBuddies.Application.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace DindinBuddies.Api.Controllers;

/// <summary>
/// Transações já registradas. Elas são criadas pelas movimentações (depósito, saque e transferência).
/// </summary>
[ApiController]
[Route("api/transacoes")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class TransacoesController(MovimentacaoServico movimentacoes) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<TransacaoResponse>(StatusCodes.Status200OK)]
    public Task<TransacaoResponse> Obter(long id, CancellationToken ct) =>
        movimentacoes.ObterTransacaoAsync(id, ct);

    /// <summary>Edita a descrição da transação. Valor, tipo e contas não mudam.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<TransacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<TransacaoResponse> Editar(long id, EditarTransacaoRequest request, CancellationToken ct) =>
        movimentacoes.EditarTransacaoAsync(id, request, ct);

    /// <summary>
    /// Exclui a transação com estorno: desfaz o efeito no saldo das contas envolvidas.
    /// Recusado se algum saldo ficar negativo ou se alguma conta estiver encerrada.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Excluir(long id, CancellationToken ct)
    {
        await movimentacoes.ExcluirTransacaoAsync(id, ct);
        return NoContent();
    }
}
