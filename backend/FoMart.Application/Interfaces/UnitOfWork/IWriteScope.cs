namespace FoMart.Application.Interfaces.UnitOfWork;

// Transaction của một nghiệp vụ ghi. Cách dùng trong Service:
//   await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
//   ... thao tác qua repository ...
//   await unitOfWork.SaveChangesAsync(cancellationToken);
//   await write.CommitAsync(cancellationToken);
// Không gọi CommitAsync thì DisposeAsync tự rollback.
public interface IWriteScope : IAsyncDisposable
{
    // Xác nhận transaction.
    Task CommitAsync(CancellationToken cancellationToken = default);
}
