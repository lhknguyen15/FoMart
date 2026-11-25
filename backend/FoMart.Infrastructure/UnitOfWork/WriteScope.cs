using System.Data;
using FoMart.Application.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoMart.Infrastructure.UnitOfWork;

public sealed class WriteScope(IDbContextTransaction transaction) : IWriteScope
{
    // Xác nhận transaction.
    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

    // Không Commit trước đó thì transaction tự rollback.
    public ValueTask DisposeAsync() => transaction.DisposeAsync();

    // Mở transaction READ COMMITTED (database bật READ_COMMITTED_SNAPSHOT).
    // Khác FoMed: KHÔNG khóa toàn cục bằng sp_getapplock. Trừ kho khóa dòng UPDLOCK, ROWLOCK
    // trong StockRepository, nên hai máy bán khác sản phẩm không phải chờ nhau (NFR-06).
    public static async Task<IWriteScope> BeginAsync(FoMartDbContext dbContext, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        return new WriteScope(transaction);
    }
}
