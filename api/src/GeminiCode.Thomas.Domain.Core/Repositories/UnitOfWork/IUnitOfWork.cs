using System;
using System.Threading.Tasks;

namespace GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;

public interface IUnitOfWork: IDisposable
{
    IUserRepository UserRepository { get; }
    IRefreshTokenRepository RefreshTokenRepository { get; }
    IProjectRepository ProjectRepository { get; }
    IErrorLogRepository ErrorLogRepository { get; }
    Task CommitAsync();

    /// <summary>
    /// Executes the given operation inside a database transaction, guaranteeing that all the
    /// writes performed by the operation are persisted atomically: either every change is
    /// committed or every change is rolled back.
    /// </summary>
    /// <param name="operation">The operation to be executed. It is expected to stage the writes through the repositories and call <see cref="CommitAsync"/>.</param>
    Task ExecuteInTransactionAsync(Func<Task> operation);

    void DiscardChanges();
}
