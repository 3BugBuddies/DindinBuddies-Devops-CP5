namespace DindinBuddies.Application.Excecoes;

/// <summary>
/// O recurso pedido pelo id não existe. A API converte em 404 (ProblemDetails).
/// </summary>
public class RecursoNaoEncontradoException(string mensagem) : Exception(mensagem);
