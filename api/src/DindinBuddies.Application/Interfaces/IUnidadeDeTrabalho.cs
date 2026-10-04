using DindinBuddies.Application.Excecoes;

namespace DindinBuddies.Application.Interfaces;

public interface IUnidadeDeTrabalho
{
    /// <summary>
    /// Grava todas as alterações pendentes em uma única transação do banco.
    /// </summary>
    /// <exception cref="ConflitoDeDadosException">Violação de índice único.</exception>
    Task SalvarAsync(CancellationToken ct = default);
}
