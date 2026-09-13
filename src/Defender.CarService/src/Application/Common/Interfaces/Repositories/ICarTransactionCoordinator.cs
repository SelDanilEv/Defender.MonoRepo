namespace Defender.CarService.Application.Common.Interfaces.Repositories;

public interface ICarTransactionCoordinator
{
    Task<T> ExecuteAsync<T>(
        Func<ICarTransactionContext, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        Func<ICarTransactionContext, Task> operation,
        CancellationToken cancellationToken = default);
}
