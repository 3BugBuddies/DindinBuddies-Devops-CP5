namespace DindinBuddies.Domain.Excecoes;

/// <summary>
/// Violação de uma regra de negócio. A API converte em 422 (ProblemDetails).
/// </summary>
public class RegraDeNegocioException(string mensagem) : Exception(mensagem);
