using Razorpay.Api;


public class RazorPayService
{
    private readonly string key = "rzp_test_Shf0DOhaG0kmvy";
    private readonly string secret = "DzN7vCP7M5Z3kv3DdfLXebdk";

    public Order? CreateOrder(int amount, string currency , Guid orderId)
    {
        try
        {
            RazorpayClient client = new(key, secret);

            Dictionary<string, object> options = new Dictionary<string, object>
            {
                { "amount", amount * 100 }, 
                { "currency", currency },
                { "receipt", orderId },
                { "payment_capture", 1 } 
            };

            Order order = client.Order.Create(options);
            return order;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }
}