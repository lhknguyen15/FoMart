namespace FoMart.Domain.Exceptions;

// Ném ra từ Domain/Rules khi vi phạm quy tắc nghiệp vụ (BR-xx).
// ApiExceptionHandler đổi thành HTTPResponseData 409 với Message là thông báo này.
public sealed class BusinessRuleException(string rule, string message) : Exception(message)
{
    // Mã quy tắc trong spec, ví dụ "BR-12".
    public string Rule { get; } = rule;
}
