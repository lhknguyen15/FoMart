using FoMart.Application.Interfaces.Repositories;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Infrastructure.Repositories;

namespace FoMart.Infrastructure.UnitOfWork;

public sealed class UnitOfWork(FoMartDbContext dbContext) : IUnitOfWork
{
    // Repository được tạo lần đầu khi dùng tới và dùng chung DbContext của request,
    // nên không đăng ký từng repository trong Program.cs (khác FoMed).
    private IStoreRepository? _storeRepository;

    public IStoreRepository StoreRepository => _storeRepository ??= new StoreRepository(dbContext);

    // Mở transaction cho nghiệp vụ ghi nhiều bảng.
    public Task<IWriteScope> BeginWriteAsync(CancellationToken cancellationToken = default) =>
        WriteScope.BeginAsync(dbContext, cancellationToken);

    // Lưu mọi thay đổi đang được theo dõi.
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
