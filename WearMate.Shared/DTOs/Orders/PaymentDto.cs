namespace WearMate.Shared.DTOs.Orders;

/// <summary>
/// Payment request DTO
/// </summary>
public class PaymentRequestDto
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "momo"; // momo, vnpay, cod
    public string? Description { get; set; }
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Payment response DTO
/// </summary>
public class PaymentResponseDto
{
    public Guid OrderId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending"; // pending, success, failed
    public string Message { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payment callback data
/// </summary>
public class PaymentCallbackDto
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> ExtraData { get; set; } = new();
}

/// <summary>
/// COD payment (Cash on Delivery)
/// </summary>
public class CodPaymentDto
{
    public Guid OrderId { get; set; }
}

/// <summary>
/// Momo payment details
/// </summary>
public class MomoPaymentDto
{
    public Guid OrderId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// VNPay payment details
/// </summary>
public class VnPayPaymentDto
{
    public Guid OrderId { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
