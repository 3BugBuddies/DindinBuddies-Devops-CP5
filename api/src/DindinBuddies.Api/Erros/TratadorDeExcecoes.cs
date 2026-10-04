using DindinBuddies.Application.Excecoes;
using DindinBuddies.Domain.Excecoes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DindinBuddies.Api.Erros;

/// <summary>
/// Converte exceções em respostas ProblemDetails:
/// regra de negócio ou conflito de dados → 422, recurso inexistente → 404, demais → 500.
/// </summary>
public class TratadorDeExcecoes(IProblemDetailsService problemDetails, ILogger<TratadorDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, titulo, detalhe) = exception switch
        {
            RegraDeNegocioException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            ConflitoDeDadosException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            RecursoNaoEncontradoException e => (StatusCodes.Status404NotFound, "Recurso não encontrado", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado. Tente novamente mais tarde.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo, Detail = detalhe }
        });
    }
}
