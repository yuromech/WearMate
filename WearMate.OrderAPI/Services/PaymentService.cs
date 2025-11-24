namespace WearMate.OrderAPI.Services;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> ProcessMomoPaymentAsync(Guid orderId, decimal amount)
    {
        _logger.LogInformation("Processing Momo payment for order {OrderId}", orderId);
        await Task.Delay(100);
        return $"https://payment.momo.vn/checkout/{orderId}";
    }

    public async Task<string?> ProcessVnPayPaymentAsync(Guid orderId, decimal amount)
    {
        _logger.LogInformation("Processing VNPay payment for order {OrderId}", orderId);
        await Task.Delay(100);
        return $"https://payment.vnpay.vn/checkout/{orderId}";
    }

    public async Task<bool> ProcessCODPaymentAsync(Guid orderId)
    {
        _logger.LogInformation("Order {OrderId} will be paid by COD", orderId);
        await Task.Delay(10);
        return true;
    }

    public async Task<bool> HandlePaymentCallbackAsync(string provider, Dictionary<string, string> parameters)
    {
        _logger.LogInformation("Payment callback from {Provider}", provider);
        await Task.Delay(10);
        return true;
    }
}