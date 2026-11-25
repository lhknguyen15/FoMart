using FoMart.Application.DTO;
using FoMart.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Api.Middleware;

// Lưới an toàn cuối: đổi mọi exception chưa xử lý thành HTTPResponseData như các API khác.
// Lỗi nghiệp vụ dự kiến do Service trả qua StatusCode, không ném exception.
public sealed partial class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    // Chọn StatusCode + Message theo loại lỗi rồi ghi phản hồi JSON.
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        var (status, message) = (exception, sql?.Number) switch
        {
            (BusinessRuleException rule, _) => (409, rule.Message),
            (DbUpdateConcurrencyException, _) => (409, "Dữ liệu đã bị người khác thay đổi. Vui lòng tải lại."),
            (_, 2601 or 2627) => (409, "Dữ liệu bị trùng (ví dụ mã vạch đã thuộc sản phẩm khác)."),
            (_, 547) => (409, "Dữ liệu vi phạm ràng buộc."),
            (_, 1205) => (409, "Hệ thống đang bận, vui lòng thử lại."),
            (_, 50001) => (409, "Sổ kho chỉ được thêm, không được sửa hoặc xóa (BR-03)."),
            (_, 50002) => (409, "Không sửa được hệ số quy đổi khi đã có giao dịch. Hãy tạo đơn vị mới (BR-12)."),
            (BadHttpRequestException bad, _) => (bad.StatusCode, "Yêu cầu không hợp lệ."),
            _ => (500, "Có lỗi khi xử lý yêu cầu."),
        };

        if (status == 500) LogUnhandled(logger, exception, httpContext.Request.Path);
        else LogRejected(logger, exception, status, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new HTTPResponseData<object?> { DataResponse = null, Message = message, StatusCode = status },
            cancellationToken);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Request failed: {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected with {Status}: {Path}")]
    private static partial void LogRejected(ILogger logger, Exception exception, int status, string path);
}
