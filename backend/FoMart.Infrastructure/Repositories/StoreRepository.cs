using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;

namespace FoMart.Infrastructure.Repositories;

public sealed class StoreRepository(FoMartDbContext dbContext)
    : RepositoryBase<Store>(dbContext), IStoreRepository
{
    private const byte StoreId = 1;

    // Lấy cấu hình cửa hàng (dòng duy nhất id = 1).
    public Task<Store?> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        GetByIdAsync(StoreId, cancellationToken);
}
