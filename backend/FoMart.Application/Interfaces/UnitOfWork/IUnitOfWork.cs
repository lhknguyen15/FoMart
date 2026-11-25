using FoMart.Application.Interfaces.Repositories;

namespace FoMart.Application.Interfaces.UnitOfWork;

// Điểm vào duy nhất của Service tới dữ liệu: unitOfWork.<ChủĐề>Repository, rồi SaveChangesAsync.
// Thêm chủ đề mới: thêm 1 property ở đây và 1 field + property trong Infrastructure/UnitOfWork/UnitOfWork.cs.
public interface IUnitOfWork
{
    IStoreRepository StoreRepository { get; }

    IUserRepository UserRepository { get; }

    IProductRepository ProductRepository { get; }

    ICategoryRepository CategoryRepository { get; }

    ISupplierRepository SupplierRepository { get; }

    ILocationRepository LocationRepository { get; }

    // Mở transaction cho nghiệp vụ ghi nhiều bảng (nhập hàng, bán hàng, kiểm kê...).
    Task<IWriteScope> BeginWriteAsync(CancellationToken cancellationToken = default);

    // Lưu mọi thay đổi đang được theo dõi của request hiện tại.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
