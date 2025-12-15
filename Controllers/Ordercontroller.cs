using HealthCart.Data;
using HealthCart.Interfaces;
using HealthCart.Models.DomainModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCart.Models.ViewModels;
using HealthCart.Types;
using HealthCart.Models.JunctionModels;

namespace HealthCart.Controllers
{
    public class OrderController : Controller
    {
        // GET: OrderController
        private readonly SqlDbContext dbContext;
        private readonly RazorPayService razorpayService;
        private readonly IMailService mailService;


        public OrderController(SqlDbContext dbContext, IMailService mailService)
        {
            this.dbContext = dbContext;
            this.mailService = mailService;
            razorpayService = new RazorPayService();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckOut(Guid CartId)
        {

            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;


                var cart = await dbContext.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CartId == CartId); // finding cart of user 

                if (cart == null || cart.CartValue == 0)
                {
                    return RedirectToAction("Cart", "User");
                }


                var address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);


                var cartItems = await dbContext.CartItems
                .Include(cp => cp.Product)
                .Where(cp => cp.CartId == cart.CartId)
                .ToListAsync();

                var viewModel = new HybridViewModel
                {
                    CartItems = cartItems,
                    Cart = cart,
                    Address = address
                };

                return View(viewModel);

            }
            catch (System.Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(PaymentStatus paymentOption)
        {

            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                if (userId == null)
                {
                    return RedirectToAction("Login", "User"); // Or handle as appropriate
                }

                var address = await dbContext.Addresses.FirstOrDefaultAsync(u => u.UserId == userId);

                if (address == null)
                {
                    ViewBag.AddressErrorMessage = "Kindly Fill in Address or select any Address from the list";
                    return View("CheckOut");
                }

                var cart = await dbContext.Carts
                .Include(c => c.CartItems)
                .ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || cart.CartValue == 0)
                {
                    return RedirectToAction("Cart", "User");

                }

                // Convert CartProducts to OrderProducts
                var orderItems = cart.CartItems.Select(cp => new OrderItem
                {
                    ProductId = cp.ProductId,
                    Quantity = cp.Quantity,
                  

                }).ToList();

                var order = new Order
                {
                    OrderStatus = OrderStatus.Pending,
                    PaymentStatus = paymentOption,
                    AddressId = address.AddressId,
                    TotalPrice = cart.CartValue,
                    UserId = (Guid)userId,
                    OrderItems = orderItems
                };

                var createOrder = await dbContext.Orders.AddAsync(order);

                dbContext.CartItems.RemoveRange(cart.CartItems);
                cart.CartValue = 0;
                await dbContext.SaveChangesAsync();

                return RedirectToAction("Verify", new { order.OrderId });
            }
            catch (System.Exception ex)
            {

                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Verify(Guid OrderId)
        {
            Guid? userId = HttpContext.Items["UserId"] as Guid?;

            var order = await dbContext.Orders.Include(o => o.Address).Include(o => o.Buyer).FirstOrDefaultAsync(o => o.OrderId == OrderId); // finding order using orderId

            if (order == null)
            {
                ViewBag.CartEmpty = "No recent Orders";
                return View();
            }

            // for efficnecy used two queries // or maybe we can call a single query // will watch in future 

            var orderItems = await dbContext.OrderItems
            .Include(op => op.Product)
            .Where(op => op.OrderId == order.OrderId)
            .ToListAsync();

            // var address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);


            var viewModel = new HybridViewModel
            {
                OrderItems = orderItems,
                Order = order,
                Address = order.Address
            };


            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendEmail(Guid OrderId)
        {
            try
            {

                var order = await dbContext.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.Address)
                    .Include(o => o.Buyer)
                    .FirstOrDefaultAsync(o => o.OrderId == OrderId);


                var productRows = "";

                if (order?.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        productRows += $@"
                          <tr>
                             <td>{item?.Product?.Name}</td>
                             <td>{item?.Quantity}</td>
                             <td>$ {item?.Product?.Price:F2}</td>
                             <td>$ {item?.Quantity * item?.Product?.Price:F2}</td>
                         </tr>";
                    }
                }

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      background-color: #f8f9fa;
      margin: 0;
      padding: 20px;
    }}
    .container {{
      max-width: 700px;
      margin: auto;
      background-color: #fff;
      padding: 30px;
      border-radius: 10px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }}
    h2 {{
      text-align: center;
      margin-bottom: 30px;
      color: #333;
    }}
    table {{
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 20px;
    }}
    table, th, td {{
      border: 1px solid #ddd;
    }}
    th, td {{
      padding: 12px;
      text-align: left;
    }}
    th {{
      background-color: #007bff;
      color: white;
    }}
    .summary {{
      text-align: right;
      margin-top: 20px;
    }}
    .btn {{
      display: inline-block;
      padding: 12px 24px;
      border: 1px solid #007bff;
      color: white;
      text-decoration: none;
      border-radius: 5px;
      text-align: center;
    }}
    .footer {{
      font-size: 12px;
      color: #999;
      text-align: center;
      margin-top: 40px;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <h2>Order Invoice & Verification</h2>
    <p><strong>Customer:</strong> {order?.Address?.FirstName} {order?.Address?.LastName}<br/>
       <strong>Order ID:</strong> {order?.OrderId}<br/>
       <strong>Date:</strong> {order?.DateCreated?.ToString("dd MMM yyyy")}<br/>
       <strong>Address:</strong> {order?.Address?.Street}, {order?.Address?.City}, {order?.Address?.Pincode}, {order?.Address?.Country}
    </p>

    <table>
      <tr>
        <th>Product</th>
        <th>Qty</th>
        <th>Unit Price</th>
        <th>Total</th>
      </tr>
      {productRows}
    </table>

    <div class='summary'>
      <p><strong>Amount:</strong> $ {order?.TotalPrice:F2}</p>
      <p><strong>Shipping:</strong> $ 5.00 </p>
      <p><strong>Total Amount:</strong> $ {order?.TotalPrice + 5}</p>
    </div>

    <div style='text-align:center; margin-top: 30px;'>
      <a href='https://australasia-apparels.shop/order/verifiedByEmail?OrderId={OrderId}' class='btn'>Verify My Order</a>
    </div>

    <p class='footer'>If you didn’t place this order, you can safely ignore this email.</p>
  </div>
</body>
</html>";


                var email = order?.Buyer?.Email;

                if (!string.IsNullOrEmpty(email))
                {
                    await mailService.SendEmailAsync(email, "Order Verification", htmlBody, true);
                }



                TempData["Message"] = "Mail sent to your Mail Id . Kindly check Your mail box and search for our mail and press verify!";
                return RedirectToAction("Verify", new { order?.OrderId });

            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Verify", new { OrderId });

            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VerifiedByEMail(Guid OrderId)
        {
            try
            {
                var order = await dbContext.Orders.FindAsync(OrderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order Not Found!";
                    return RedirectToAction("Verify", new { OrderId });
                }

                order.OrderStatus = OrderStatus.Verified;
                await dbContext.SaveChangesAsync();

                TempData["Message"] = "Email is SuccesFully Verified you can pay Now for your Order!";
                return RedirectToAction("Verify", new { order?.OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Verify", new { OrderId });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(Guid OrderId)
        {
            try
            {
                var order = await dbContext.Orders.FindAsync(OrderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order Not Found!";
                    return RedirectToAction("Verify", new { OrderId });
                }

                order.OrderStatus = OrderStatus.InTransit;
                order.PaymentStatus = PaymentStatus.RazorPay;
                await dbContext.SaveChangesAsync();


                return RedirectToAction("Orders", "User");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Verify", new { OrderId });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Cancel(Guid OrderId)
        {
            try
            {
                var order = await dbContext.Orders.Include(o => o.Buyer).FirstOrDefaultAsync(o => o.OrderId == OrderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order Not Found!";
                    return RedirectToAction("Verify", new { OrderId });
                }

                string htmlBody = $@"
    <html>
        <body style='font-family: Arial, sans-serif;'>
            <h2 style='color: #d9534f;'>Order Cancellation Requested</h2>
            <p>Dear {order?.Buyer?.Username},</p>
            <p>We received a request to cancel your order with the ID <strong>{order?.OrderId}</strong>.</p>
            <p>If you initiated this cancellation, please confirm it by clicking the button below:</p>
            <p style='margin: 20px 0;'>
                <a href='https://australasia-apparels.shop/Order/ConfirmCancellation?OrderId={order?.OrderId}' 
                   style='display: inline-block; padding: 10px 20px; background-color: #d9534f; 
                          color: white; text-decoration: none; border-radius: 5px;'>
                    Confirm Cancellation
                </a>
            </p>
            <p>If you did not request this, you can safely ignore this email and your order will remain active.</p>
            <p>Thank you,<br/>Customer Support Team</p>
        </body>
    </html>";


                var email = order?.Buyer?.Email;

                if (!string.IsNullOrEmpty(email))
                {
                    await mailService.SendEmailAsync(email, "Verify Order cancellation ", htmlBody, true);
                }


                TempData["EmailMessage"] = "Your have request order cancellation . Kindly check your mail and verify order cancellation!";
                return RedirectToAction("Verify", new { OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Verify", new { OrderId });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ConfirmCancellation(Guid OrderId)
        {

            try
            {
                var order = await dbContext.Orders.FindAsync(OrderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order Not Found!";
                    return RedirectToAction("Verify", new { OrderId });
                }

                order.OrderStatus = OrderStatus.Cancelled;
                await dbContext.SaveChangesAsync();

                TempData["EmailMessage"] = "Your order has been succesfully cancelled!";
                return RedirectToAction("Verify", new { OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Verify", new { OrderId });
            }

        }

    }

   
}