using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure;

// Cấu hình EF viết tay. FoMartDbContext.cs là file scaffold: KHÔNG sửa, scaffold lại sẽ ghi đè.
// Thêm value converter, query filter... vào đây.
public partial class FoMartDbContext
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822",
        Justification = "Implements the instance partial method declared by the scaffolded DbContext.")]
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Scaffold EF Core 10 đã tự sinh HasTrigger(...) cho stock_moves và product_units.
    }
}
