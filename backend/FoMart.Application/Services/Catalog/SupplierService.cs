using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;

namespace FoMart.Application.Services.Catalog;

// Nhà cung cấp (UC-02).
public sealed class SupplierService(IUnitOfWork unitOfWork)
{
    // Tìm nhà cung cấp theo tên hoặc số điện thoại.
    public async Task<HTTPResponseData<IReadOnlyList<SupplierResponse>?>> SearchAsync(
        SupplierSearchRequest request, CancellationToken cancellationToken)
    {
        var suppliers = await unitOfWork.SupplierRepository.SearchAsync(request, cancellationToken);
        return HTTPResponse.Success(suppliers, CatalogResponseMessageDTO.SearchSuppliersSuccess);
    }

    // Lấy một nhà cung cấp.
    public async Task<HTTPResponseData<SupplierResponse?>> GetByIdAsync(int supplierId, CancellationToken cancellationToken)
    {
        var supplier = await unitOfWork.SupplierRepository.GetByIdAsync(supplierId, cancellationToken);
        return supplier is null
            ? HTTPResponse.Fail<SupplierResponse>(404, CatalogResponseMessageDTO.SupplierNotFound)
            : HTTPResponse.Success(Map(supplier), CatalogResponseMessageDTO.GetSupplierSuccess);
    }

    // Tạo nhà cung cấp.
    public async Task<HTTPResponseData<SupplierResponse?>> CreateAsync(
        CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Phone = NullIfEmpty(request.Phone),
            Zalo = NullIfEmpty(request.Zalo),
            Note = NullIfEmpty(request.Note),
            IsActive = true,
        };
        await unitOfWork.SupplierRepository.AddAsync(supplier, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success(Map(supplier), CatalogResponseMessageDTO.CreateSupplierSuccess, 201);
    }

    // Sửa nhà cung cấp; IsActive = false để ẩn (không xóa vì còn gắn với phiếu nhập, phiếu đặt).
    public async Task<HTTPResponseData<SupplierResponse?>> UpdateAsync(
        int supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await unitOfWork.SupplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null)
            return HTTPResponse.Fail<SupplierResponse>(404, CatalogResponseMessageDTO.SupplierNotFound);

        supplier.Name = request.Name.Trim();
        supplier.Phone = NullIfEmpty(request.Phone);
        supplier.Zalo = NullIfEmpty(request.Zalo);
        supplier.Note = NullIfEmpty(request.Note);
        supplier.IsActive = request.IsActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success(Map(supplier), CatalogResponseMessageDTO.UpdateSupplierSuccess);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SupplierResponse Map(Supplier supplier) =>
        new(supplier.Id, supplier.Name, supplier.Phone, supplier.Zalo, supplier.Note, supplier.IsActive, supplier.CreatedAt);
}
