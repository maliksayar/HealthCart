using HealthCart.Data;
using HealthCart.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthCart.Controllers
{
    [Route("[controller]")]
    [ApiController]


    public class PaymentApiController(SqlDbContext dbContext, ITokenService tokenService) : ControllerBase
    {
        // private readonly ILogger<OrderController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly SqlDbContext dbContext = dbContext;
        private readonly ITokenService tokenService = tokenService;
        private readonly RazorPayService razorpayService = new RazorPayService();

        [HttpPost("create/paymentIntent")]
        public IActionResult CreatePaymentIntent([FromBody] CreatePaymentIntentModel model)
        {
            if (model == null || model.Amount <= 0 || string.IsNullOrEmpty(model.Currency))
            {
                return BadRequest("Invalid payment details.");
            }
            try
            {
                var order = razorpayService.CreateOrder(model.Amount/100, model.Currency, model.OrderId);

                if (order == null)
                {
                    return StatusCode(500, "Failed to create payment order.");                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
                }
    
                return Ok(new
                {
                    orderId = order["id"].ToString(),
                    entity = order["entity"].ToString(),
                    amount = order["amount"],
                    amountPaid = order["amount_paid"],
                    amountDue = order["amount_due"],
                    currency = order["currency"].ToString(),
                    receipt = order["receipt"].ToString(),
                    status = order["status"].ToString(),
                    attempts = order["attempts"],
                    createdAt = order["created_at"]
                });
            }
            catch (Exception ex)
            {


                Console.WriteLine(ex.Message);
                return StatusCode(500, new{
                    message = "server Error",
                    Error = ex.Message
                } );
            }
        }

        public class CreatePaymentIntentModel
        {
            public int Amount { get; set; }
            public string? Currency { get; set; }
            public Guid OrderId { get; set; }
        }
    }



}

