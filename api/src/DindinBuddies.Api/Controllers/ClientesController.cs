using DindinBuddies.Application.Dtos.Clientes;
using DindinBuddies.Application.Dtos.Contas;
using DindinBuddies.Application.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace DindinBuddies.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController(ClienteServico clientes, ContaServico contas) : ControllerBase
{
    /// <summary>Lista os clientes, com busca opcional por nome ou CPF.</summary>
    [HttpGet]
    public Task<IReadOnlyList<ClienteResponse>> Listar([FromQuery] string? busca, CancellationToken ct) =>
        clientes.ListarAsync(busca, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ClienteResponse> Obter(int id, CancellationToken ct) =>
        clientes.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteResponse>> Criar(CriarClienteRequest request, CancellationToken ct)
    {
        var cliente = await clientes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ClienteResponse> Editar(int id, EditarClienteRequest request, CancellationToken ct) =>
        clientes.EditarAsync(id, request, ct);

    /// <summary>Exclui o cliente. Só é permitido se ele não tiver contas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await clientes.ExcluirAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/contas")]
    [ProducesResponseType<IReadOnlyList<ContaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ContaResponse>> ListarContas(int id, CancellationToken ct) =>
        contas.ListarPorClienteAsync(id, ct);

    /// <summary>Abre uma conta para o cliente. Agência e número são gerados pela API.</summary>
    [HttpPost("{id:int}/contas")]
    [ProducesResponseType<ContaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContaResponse>> AbrirConta(int id, AbrirContaRequest request, CancellationToken ct)
    {
        var conta = await contas.AbrirAsync(id, request, ct);
        return CreatedAtAction(nameof(ContasController.Obter), "Contas", new { id = conta.Id }, conta);
    }
}
