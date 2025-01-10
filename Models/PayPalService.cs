using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class PayPalService
{
    private readonly IConfiguration _configuration;

    public PayPalService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private PayPalEnvironment Environment => new SandboxEnvironment(
        _configuration["PayPal:ClientId"],
        _configuration["PayPal:ClientSecret"]);

    private PayPalHttpClient Client => new PayPalHttpClient(Environment);

    public async Task<string> CreateOrder(decimal amount, string currency = "USD")
    {
        var orderRequest = new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = currency,
                        Value = amount.ToString("F2")
                    }
                }
            },
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = "https://localhost:5001/Home/PaymentSuccess",
                CancelUrl = "https://localhost:5001/Home/PaymentCancel"
            }
        };

        var request = new OrdersCreateRequest();
        request.Prefer("return=representation");
        request.RequestBody(orderRequest);

        var response = await Client.Execute(request);
        var result = response.Result<Order>();
        return result.Links[1].Href; // PayPal Approval Link
    }

    public async Task<bool> CaptureOrder(string orderId)
    {
        var request = new OrdersCaptureRequest(orderId);
        request.RequestBody(new OrderActionRequest());

        var response = await Client.Execute(request);
        return response.StatusCode == System.Net.HttpStatusCode.Created;
    }
}
