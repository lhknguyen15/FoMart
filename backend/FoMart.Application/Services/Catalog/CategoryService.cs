using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;

namespace FoMart.Application.Services.Catalog;

// Nhóm hàng (UC-02).
public sealed class CategoryService(IUnitOfWork unitOfWork)
{
    // Lấy danh sách nhóm hàng; includeInactive = true để thấy cả nhóm đã ẩn.
    public async Task<HTTPResponseData<IReadOnlyList<CategoryResponse>?>> GetAllAsync(
        bool includeInactive, CancellationToken cancellationToken)
    {
        var categories = await unitOfWork.CategoryRepository.GetAllAsync(includeInactive, cancellationToken);
        return HTTPResponse.Success(categories, CatalogResponseMessageDTO.GetCategoriesSuccess);
    }

    // Tạo nhóm hàng; tên không được trùng (không phân biệt dấu).
    public async Task<HTTPResponseData<CategoryResponse?>> CreateAsync(
        CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await unitOfWork.CategoryRepository.NameExistsAsync(name, null, cancellationToken))
            return HTTPResponse.Fail<CategoryResponse>(409, CatalogResponseMessageDTO.CategoryNameExists);

        var category = new Category { Name = name, SortOrder = request.SortOrder, IsActive = true };
        await unitOfWork.CategoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success(Map(category, productCount: 0), CatalogResponseMessageDTO.CreateCategorySuccess, 201);
    }

    // Sửa nhóm hàng; IsActive = false để ẩn (sản phẩm vẫn giữ nhóm cũ).
    public async Task<HTTPResponseData<CategoryResponse?>> UpdateAsync(
        int categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await unitOfWork.CategoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return HTTPResponse.Fail<CategoryResponse>(404, CatalogResponseMessageDTO.CategoryNotFound);

        var name = request.Name.Trim();
        if (await unitOfWork.CategoryRepository.NameExistsAsync(name, categoryId, cancellationToken))
            return HTTPResponse.Fail<CategoryResponse>(409, CatalogResponseMessageDTO.CategoryNameExists);

        category.Name = name;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var all = await unitOfWork.CategoryRepository.GetAllAsync(includeInactive: true, cancellationToken);
        return HTTPResponse.Success(all.Single(c => c.Id == categoryId), CatalogResponseMessageDTO.UpdateCategorySuccess);
    }

    private static CategoryResponse Map(Category category, int productCount) =>
        new(category.Id, category.Name, category.SortOrder, category.IsActive, productCount);
}
