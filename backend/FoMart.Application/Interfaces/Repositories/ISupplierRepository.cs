using FoMart.Application.DTO.Catalog;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

public interface ISupplierRepository : IRepositoryBase<Supplier>
{
    // Tìm nhà cung cấp theo tên (không dấu) hoặc số điện thoại, sắp theo tên.
    Task<IReadOnlyList<SupplierResponse>> SearchAsync(SupplierSearchRequest request, CancellationToken cancellationToken = default);
}
