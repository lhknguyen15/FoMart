namespace FoMart.Application.DTO;

// Khuôn phản hồi chung của mọi API, kể cả khi lỗi.
// Service trả về, Controller trả nguyên cho client với HTTP status = StatusCode.
public sealed record HTTPResponseData<T>
{
    public required T DataResponse { get; init; }

    public required string Message { get; init; }

    public int StatusCode { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
