using System.Linq.Expressions;

namespace FoMart.Application.Interfaces.Repositories;

// Thao tác chung cho mọi repository. I<ChủĐề>Repository kế thừa và thêm truy vấn đặc thù.
public interface IRepositoryBase<TEntity> where TEntity : class
{
    // id là object vì khóa có nhiều kiểu: int (đa số), byte (stores), Guid (sales).
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    // Chỉ dùng cho danh mục và chứng từ còn nháp (chưa đổi tồn). Chứng từ đã hoàn tất không bao giờ xóa (BR-03).
    void Delete(TEntity entity);
}
