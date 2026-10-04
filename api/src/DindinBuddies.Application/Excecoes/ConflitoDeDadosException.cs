namespace DindinBuddies.Application.Excecoes;

/// <summary>
/// O banco recusou a gravação por violar um índice único (ex.: CPF cadastrado ao mesmo tempo
/// por duas requisições). A API converte em 422 (ProblemDetails).
/// </summary>
public class ConflitoDeDadosException(string mensagem, Exception? interna = null) : Exception(mensagem, interna);
