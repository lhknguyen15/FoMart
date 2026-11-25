using System.Globalization;
using System.Security.Claims;

namespace FoMart.Api.Middleware;

// TẠM THỜI, chỉ đăng ký ở môi trường Development, cho tới khi có đăng nhập JWT (mảng Auth).
// Request chưa đăng nhập được gắn user giả lập DevelopmentUser:UserId (mặc định chủ tiệm id 1 của seed dev),
// để controller đọc user từ claim giống hệt khi có JWT. Làm xong Auth thì XÓA file này và dòng UseMiddleware.
public sealed class DevelopmentUserMiddleware(RequestDelegate next, IConfiguration configuration)
{
    // Gắn user giả lập nếu request chưa có user đăng nhập.
    public Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true
            && int.TryParse(configuration["DevelopmentUser:UserId"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
                    new Claim(ClaimTypes.Role, configuration["DevelopmentUser:Role"] ?? "owner"),
                ],
                authenticationType: "Development"));
        }
        return next(context);
    }
}
