using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

public interface IStoreRepository : IRepositoryBase<Store>
{
    // Lấy cấu hình cửa hàng: core.stores luôn có đúng một dòng id = 1 (migration V002).
    Task<Store?> GetSettingsAsync(CancellationToken cancellationToken = default);
}
