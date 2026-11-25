using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;

namespace FoMart.Application.Services.Catalog;

// Vị trí: kho sau, kệ bán (UC-02, BR-16).
public sealed class LocationService(IUnitOfWork unitOfWork)
{
    // Lấy danh sách vị trí kèm tổng tồn.
    public async Task<HTTPResponseData<IReadOnlyList<LocationResponse>?>> GetAllAsync(
        bool includeInactive, CancellationToken cancellationToken)
    {
        var locations = await unitOfWork.LocationRepository.GetAllAsync(includeInactive, cancellationToken);
        return HTTPResponse.Success(locations, CatalogResponseMessageDTO.GetLocationsSuccess);
    }

    // Tạo vị trí mới (chưa phải vị trí bán mặc định; muốn đổi mặc định thì dùng UpdateAsync).
    public async Task<HTTPResponseData<LocationResponse?>> CreateAsync(
        CreateLocationRequest request, CancellationToken cancellationToken)
    {
        if (!LocationType.All.Contains(request.Type))
            return HTTPResponse.Fail<LocationResponse>(400, CatalogResponseMessageDTO.InvalidLocationType);

        var name = request.Name.Trim();
        if (await unitOfWork.LocationRepository.NameExistsAsync(name, null, cancellationToken))
            return HTTPResponse.Fail<LocationResponse>(409, CatalogResponseMessageDTO.LocationNameExists);

        var location = new Location { Name = name, Type = request.Type, IsActive = true };
        await unitOfWork.LocationRepository.AddAsync(location, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success(Map(location, stockQty: 0), CatalogResponseMessageDTO.CreateLocationSuccess, 201);
    }

    // Sửa vị trí: đổi tên/loại, chọn làm vị trí bán mặc định, ngừng dùng khi tồn bằng 0.
    public async Task<HTTPResponseData<LocationResponse?>> UpdateAsync(
        int locationId, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        if (!LocationType.All.Contains(request.Type))
            return HTTPResponse.Fail<LocationResponse>(400, CatalogResponseMessageDTO.InvalidLocationType);

        var location = await unitOfWork.LocationRepository.GetByIdAsync(locationId, cancellationToken);
        if (location is null)
            return HTTPResponse.Fail<LocationResponse>(404, CatalogResponseMessageDTO.LocationNotFound);

        var name = request.Name.Trim();
        if (await unitOfWork.LocationRepository.NameExistsAsync(name, locationId, cancellationToken))
            return HTTPResponse.Fail<LocationResponse>(409, CatalogResponseMessageDTO.LocationNameExists);

        // BR-16: luôn có đúng một vị trí bán mặc định và nó phải là kệ bán đang dùng.
        if (location.IsDefaultSale && !request.IsDefaultSale)
            return HTTPResponse.Fail<LocationResponse>(409, CatalogResponseMessageDTO.DefaultSaleCannotBeUnset);
        if (request.IsDefaultSale && request.Type != LocationType.Shelf)
            return HTTPResponse.Fail<LocationResponse>(400, CatalogResponseMessageDTO.DefaultSaleMustBeShelf);
        if (request.IsDefaultSale && !request.IsActive)
            return HTTPResponse.Fail<LocationResponse>(409, CatalogResponseMessageDTO.DefaultSaleCannotBeDeactivated);

        // UC-02: chỉ ngừng dùng được khi tồn tại vị trí bằng 0.
        if (location.IsActive && !request.IsActive
            && await unitOfWork.LocationRepository.HasStockAsync(locationId, cancellationToken))
            return HTTPResponse.Fail<LocationResponse>(409, CatalogResponseMessageDTO.LocationHasStock);

        location.Name = name;
        location.Type = request.Type;
        location.IsActive = request.IsActive;

        if (request.IsDefaultSale && !location.IsDefaultSale)
        {
            // Bỏ mặc định ở vị trí cũ rồi mới đặt vị trí mới: index UX_locations_default_sale chỉ cho một dòng.
            await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
            var current = await unitOfWork.LocationRepository.GetDefaultSaleAsync(cancellationToken);
            if (current is not null) current.IsDefaultSale = false;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            location.IsDefaultSale = true;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await write.CommitAsync(cancellationToken);
        }
        else
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var stockQty = await unitOfWork.LocationRepository.GetStockQtyAsync(locationId, cancellationToken);
        return HTTPResponse.Success(Map(location, stockQty), CatalogResponseMessageDTO.UpdateLocationSuccess);
    }

    private static LocationResponse Map(Location location, int stockQty) =>
        new(location.Id, location.Name, location.Type, location.IsDefaultSale, location.IsActive, stockQty);
}
