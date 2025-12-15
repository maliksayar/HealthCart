using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCart.Data;
using HealthCart.Interfaces;
using HealthCart.Models;
using HealthCart.Models.DomainModels;
using HealthCart.Models.ViewModels;
using HealthCart.Types;

namespace HealthCart.Controllers
{
    public class UserController : Controller
    {

        private readonly SqlDbContext dbContext;    // encapsulated feilds
        private readonly ITokenService tokenService;
        private readonly IMailService emailService;

        public UserController(SqlDbContext dbContext, ITokenService tokenService, IMailService emailService)
        {
            this.dbContext = dbContext;
            this.tokenService = tokenService;
            this.emailService = emailService;
        }


        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Register(User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.errorMessage = "All details Required!";
                    return View();
                }
                //   var existingUser = await sqlDbContext.Users.FindAsync(user.UserId);   // findAsync is for PK

                var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);   // findAsync is for PK



                if (existingUser != null)
                {

                    ViewBag.errorMessage = "User Already Exists";
                    return View();

                }

                var encryptPass = BCrypt.Net.BCrypt.HashPassword(user.Password);

                user.Password = encryptPass;



                var newUser = await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();


                // ViewBag.successMessage = "User Created Succefully!";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.errorMessage = ex.Message;
                return View("Error");
            }


        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginView user)
        {

            try
            {

                if (!ModelState.IsValid)
                {
                    ViewBag.errorMessage = "All credentials Required!";
                    return View();
                }

                var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);


                if (existingUser == null)
                {

                    ViewBag.errorMessage = "User not Found!";
                    return View();

                }

                var checkPass = BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password);

                if (checkPass)
                {

                    var token = tokenService.CreateToken(existingUser.UserId, user.Email, existingUser.Username, 60 * 24);

                    //    Console.WriteLine(token);

                    HttpContext.Response.Cookies.Append("AuthorizationToken", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(72)
                    });


                    var returnUrl = HttpContext.Session.GetString("ReturnUrl");


                    HttpContext.Session.Remove("ReturnUrl");
                    HttpContext.Session.SetString("UserId", existingUser.UserId.ToString());



                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }


                    else
                    {

                        return RedirectToAction("UserIndex", "Home");
                    }
                }
                else
                {
                    ViewBag.errorMessage = "PassWord incorrect!";
                    return View();
                }




            }
            catch (Exception ex)
            {

                ViewBag.errorMessage = ex.Message;
                return View("Error");
            }




        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Cart()
        {
            try
            {

                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var cart = await dbContext.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId); // finding cart of user 

                var viewModel = new CartViewModel();


                if (cart == null || cart.CartItems.Count == 0)
                {
                    ViewBag.CartEmpty = "Your Cart is Empty";    // used in if condition
                    return View(viewModel);
                }

                // for efficency there is serperated cart profucts db query
                var cartItems = await dbContext.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cart.CartId)
                .ToListAsync();


                viewModel.CartItems = cartItems;
                viewModel.Cart = cart;


                return View(viewModel);
            }
            catch (System.Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");

            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Orders(OrderStatus OrderFilter)
        {


            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                // var orders = await dbContext.Orders.Where(o => o.BuyerId == userId).ToListAsync();// finding order using orderId

                //  var orderproducts = await dbContext.OrderProducts
                // .Include(op => op.Product)
                // .Where(op => orders.Select(o=>o.OrderId).Contains(op.OrderId))
                // .ToListAsync();
                if (OrderFilter == OrderStatus.Cancelled)
                {
                    var orders = await dbContext.Orders
                                      .Include(o => o.OrderItems)  // Include OrderProducts
                                      .ThenInclude(op => op.Product)  // Include related Product
                                      .Where(o => o.UserId == userId && o.OrderStatus == OrderFilter)
                                      .ToListAsync();

                    var viewModel = new OrderViewModel
                    {
                        Orders = orders
                    };
                    ViewBag.OrderFilter = "Cancelled";
                    return View(viewModel);
                }
                else if (OrderFilter == OrderStatus.InTransit)
                {

                    var orders = await dbContext.Orders
                                  .Include(o => o.OrderItems)  // Include OrderProducts
                                  .ThenInclude(op => op.Product)  // Include related Product
                                  .Where(o => o.UserId == userId && o.OrderStatus == OrderFilter)
                                  .ToListAsync();

                    var viewModel = new OrderViewModel
                    {
                        Orders = orders
                    };
                    ViewBag.OrderFilter = "In Transit";
                    return View(viewModel);
                }
                else
                {
                    var orders = await dbContext.Orders
                                 .Include(o => o.OrderItems)  // Include OrderProducts
                                 .ThenInclude(op => op.Product)  // Include related Product
                                 .Where(o => o.UserId == userId && o.OrderStatus != OrderStatus.Cancelled)
                                 .ToListAsync();

                    var viewModel = new OrderViewModel
                    {
                        Orders = orders
                    };
                    ViewBag.OrderFilter = "Recent";
                    return View(viewModel);
                }

            }
            catch (System.Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
                throw;
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAddress(Address address , Guid CartId)
        {

            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }


                if (!ModelState.IsValid)
                {

                    TempData["ErrorMessage"] = "All the  feilds with the * are required ";
                    return RedirectToAction("CheckOut", "Order" , new{CartId});

                }

                var existingAddress = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);

                if (existingAddress == null)
                {
                    address.UserId = (Guid)userId;
                    await dbContext.Addresses.AddAsync(address);
                    await dbContext.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Address created !";
                    return RedirectToAction("CheckOut", "Order" , new{CartId});
                }
                else
                {
                    // update 
                    existingAddress.FirstName = address.FirstName;
                    existingAddress.LastName = address.LastName;
                    existingAddress.Street = address.Street;
                    existingAddress.City = address.City;
                    existingAddress.State = address.State;
                    existingAddress.Country = address.Country;
                    existingAddress.Pincode = address.Pincode;
                    existingAddress.Phone = address.Phone;

                    await dbContext.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Address updation successfull !";
                    return RedirectToAction("CheckOut", "Order" ,  new {CartId});
                }

            }
            catch (System.Exception ex)
            {

                Console.WriteLine(ex.Message);
                TempData["ErrorMessage"] = "Something Went Wrong try Agin After Sometime !";
                return RedirectToAction("CheckOut", "Order" , new {CartId});

            }

        }


        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> ForgotPassword(ForgotPassView model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.ErrorMessage = "Please provide a valid email address.";
                    return View();
                }

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ViewBag.ErrorMessage = "No user found with the provided email address.";
                    return View();
                }

                var resetToken = Guid.NewGuid().ToString();

                user.ResetPassToken = resetToken;
                user.ResetPassTokenExpiry = DateTime.UtcNow.AddHours(1);
                await dbContext.SaveChangesAsync();



                await emailService.SendEmailAsync(model.Email,
      "Reset Your Password - HealthCart",
      $@"
        <html>
            <body>
                <p>Dear user,</p>
                <p>We received a request to reset your password. Please click the link below to reset it:</p>
                <p>
                    <a href='https://127.0.0.1:5036/user/verifyPasswordReset?token={resetToken}'>
                        Reset Your Password
                    </a>
                </p>
                <p>If you did not request a password reset, please ignore this email or contact our support team.</p>
                <br />
                <p>Best regards,<br />HealthCart Team</p>
            </body>
        </html>", true);


                ViewBag.SuccessMessage = "A password reset link has been sent to your email.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<ActionResult> VerifyPasswordReset(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.ErrorMessage = "Invalid or missing token.";
                return View("ForgotPassword");
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.ResetPassToken == token);

            if (user == null)
            {
                ViewBag.ErrorMessage = "This password reset link has expired. Kindly Try again ! ";
                return View("ForgotPassword");
            }

            ViewBag.Token = token;
            return View(); // Return the view with a reset password form
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(PasswordResetView model, string token)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Password doesnot match!";
                return View("VerifyPasswordReset");
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.ResetPassToken == token);

            if (user == null)
            {
                ViewBag.ErrorMessage = "This password reset link is invalid or has expired.";
                return View("VerifyPasswordReset");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            user.ResetPassToken = null;
            user.ResetPassTokenExpiry = null;
            await dbContext.SaveChangesAsync();

            TempData["successMessage"] = "Your password has been successfully reset.";
            return RedirectToAction("Login"); // Or redirect to login page
        }

        [HttpPost]
        public async Task<ActionResult> Subscribe(NewsLetter model)
        {


            await dbContext.NewsLetters.AddAsync(model);
            await dbContext.SaveChangesAsync();

            TempData["Message"] = "You have been succesfully added to newsletter subscription!";
            return RedirectToAction("Index", "Home");

        }

        [HttpGet]
        public ActionResult Logout()
        {
            HttpContext.Response.Cookies.Delete("AuthorizationToken");
            HttpContext.Session.Clear();

            TempData["Message"] = "You have been logged out successfully!";
            return RedirectToAction("Index", "Home");
        }


    }

}