namespace OrderService.Infrastructure.Options;

public class PaymentServiceOptions
{
    public const string SectionName = "PaymentService";

    public string BaseUrl { get; set; } = string.Empty;
}
