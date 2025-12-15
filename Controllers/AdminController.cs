using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HealthCart.Data;
using HealthCart.Interfaces;
using HealthCart.Models.DomainModels;
using HealthCart.Models.ViewModels;
using HealthCart.Types;

namespace HealthCart.Controllers
{
    public class AdminController : Controller
    {
        private readonly ITokenService tokenService;
        private readonly SqlDbContext dbContext;

        private readonly ICloudinaryService cloudinary;

        public AdminController(SqlDbContext dbContext, ITokenService tokenService, ICloudinaryService cloudinary)
        {
            this.tokenService = tokenService;
            this.dbContext = dbContext;
            this.cloudinary = cloudinary;
        }



        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Index()
        {

            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


                if (user?.Role == Role.User)
                {
                    return RedirectToAction("Login", "User");
                }

                var totalRevenue = await dbContext.Orders.SumAsync(o => o.TotalPrice);

                var usersCount = await dbContext.Users.CountAsync();
                var ordersCount = await dbContext.Orders.CountAsync();
                var productsCount = await dbContext.Products.CountAsync();


                ViewBag.TotalUsers = usersCount;
                ViewBag.TotalOrders = ordersCount;
                ViewBag.TotalProducts = productsCount;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.Username = user?.Username;

                return View();
            }
            catch (System.Exception)
            {

                TempData["ErrorMessage"] = "Something Went Wrong!";
                return View();

            }

        }


        [Authorize]
        [HttpGet]
        public async Task<ActionResult> CreateProduct()
        {

            Guid? userId = HttpContext.Items["UserId"] as Guid?;

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


            if (user?.Role == Role.User)
            {
                return RedirectToAction("Login", "User");
            }


            ViewBag.CategoryList = new SelectList(Enum.GetValues(typeof(ProductCategory)));
            // ViewBag.SizeList = new SelectList(Enum.GetValues(typeof(ProductSize)));
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreateProduct(Product product, IFormFile ImageFile)
        {

            try
            {

                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


                if (user?.Role == Role.User)
                {
                    return RedirectToAction("Login", "User");
                }


              
                ViewBag.CategoryList = new SelectList(Enum.GetValues(typeof(ProductCategory)));



                if (!ModelState.IsValid)
                {
                    ViewBag.ErrorMessage = "Invalid Product Data";
                    return View(product);
                }

                if (ImageFile != null && ImageFile.Length > 0)
                {

                    var uploadResult = await cloudinary.UploadImageAsync(ImageFile);
                    if (uploadResult != null)
                    {
                        product.ImageUrl = uploadResult;
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Image Upload Failed";
                        return View();
                    }

                }


                await dbContext.Products.AddAsync(product);
                await dbContext.SaveChangesAsync();
                TempData["Message"] = "Product Created Successfully";
                return RedirectToAction("ProductList");


            }
            catch (System.Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");

            }

        }

        [Authorize]

        [HttpGet]
        public async Task<ActionResult> ProductList()
        {
            try
            {

                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user?.Role == Role.User)
                {
                    return RedirectToAction("Login", "User");
                }


                var products = await dbContext.Products.ToListAsync();

                var viewModel = new ProductViewModel
                {
                    Products = products
                };

                // var productDateGroups = await dbContext.Products
                //     .Where(p => !p.IsDeleted)
                //     .GroupBy(p => p.CreatedAt.Date)
                //     .Select(g => new
                //     {
                //         Date = g.Key,
                //         Count = g.Count()
                //     })
                //     .OrderBy(g => g.Date)
                //     .ToListAsync();

                // ViewBag.ProductChartData = productDateGroups;


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
        public async Task<ActionResult> DeleteProduct(Guid ProductId)
        {

            Guid? userId = HttpContext.Items["UserId"] as Guid?;

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


            if (user?.Role == Role.User)
            {
                return RedirectToAction("Login", "User");
            }


            var product = await dbContext.Products.FindAsync(ProductId);
            if (product == null)
            {
                return NotFound();
            }


            if (product.IsActive == true && product.IsDeleted == false)
            {
                product.IsActive = false;
                product.IsDeleted = true;
            }
            else
            {
                product.IsActive = true;
                product.IsDeleted = false; 
            }


            product.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();


            TempData["Message"] = " Production deletion Succesfull!";
            return RedirectToAction(nameof(ProductList));

        }


        [Authorize]


        [HttpGet]
        public async Task<ActionResult> EditProduct(Guid ProductId)
        {

            Guid? userId = HttpContext.Items["UserId"] as Guid?;

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


            if (user?.Role == Role.User)
            {
                return RedirectToAction("Login", "User");
            }


            var product = await dbContext.Products.FindAsync(ProductId);


            ViewBag.CategoryList = new SelectList(Enum.GetValues(typeof(ProductCategory)));
            return View(product);
        }



        [Authorize]

        [HttpPost]
        public async Task<ActionResult> EditProduct(Product model, Guid ProductId)
        {


            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


                if (user?.Role == Role.User)
                {
                    return RedirectToAction("Login", "User");
                }



                var product = await dbContext.Products.FindAsync(ProductId);
                if (product == null)
                {
                    return NotFound();
                }

                // Update properties
                product.Name = model.Name;
                product.Brand = model.Brand;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Discount = model.Discount;
                product.Stock = model.Stock;
                product.Category = model.Category;
                product.SubCategory = model.SubCategory;
                product.ReasonForDiscount = model.ReasonForDiscount;
                product.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                TempData["Message"] = "Product editing successful!";
                return RedirectToAction(nameof(ProductList));
            }
            catch (System.Exception)
            {

                TempData["ErrorMessage"] = "Something Went Wrong";
                return RedirectToAction(nameof(ProductList));
            }

        }



        [Authorize]

        [HttpGet]
        public async Task<ActionResult> OrderList()
        {
            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


                if (user?.Role == Role.User)
                {
                    return RedirectToAction("Login", "User");
                }


                var orders = await dbContext.Orders.Include(o => o.Address).ToListAsync();

                var viewModel = new OrderViewModel
                {
                    Orders = orders
                };
                return View(viewModel);
            }
            catch (System.Exception)
            {
                TempData["ErrorMessage"] = "Something Went Wrong!";
                return View();

            }

        }



        [Authorize]
        [HttpGet]
        public async Task<ActionResult> UserDb()
        {

            try
            {
                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);


                if (user?.Role == Role.User)
                {
                    return RedirectToAction("Login", "User");
                }


                var users = await dbContext.Users
                    .Include(u => u.Orders)
                    .Include(u => u.Cart)
                    .ToListAsync();

                var viewModel = new UserViewModel
                {
                    Users = users
                };
                return View(viewModel);
            }
            catch (System.Exception)
            {
                TempData["ErrorMessage"] = "Something Went Wrong!";
                return View();


            }

        }




        [Authorize]

        [HttpPost]
        public async Task<IActionResult> AddRemoveStoreKeeper(string Email)
        {

            try
            {

                Guid? userId = HttpContext.Items["UserId"] as Guid?;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user?.Role != Role.Admin)
                {
                    TempData["ErrorMessage"] = "Only Admin can Add/Remove shopkeeper ";
                    return RedirectToAction("UserDb");

                }


                if (string.IsNullOrWhiteSpace(Email))
                {
                    TempData["Message"] = "Email is required.";
                    return RedirectToAction("UserDb");
                }

                var storeKeeper = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == Email);

                if (storeKeeper == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserDb");
                }

                if (storeKeeper.Role == Role.StoreKeeper)
                {
                    storeKeeper.Role = Role.User;
                    TempData["Message"] = $"Store Keeper {storeKeeper.Username} Removed!";
                }
                else if (storeKeeper.Role == Role.User)
                {
                    storeKeeper.Role = Role.StoreKeeper;
                    TempData["Message"] = $"{storeKeeper.Username} promoted to Store Keeper.";
                }
                else if (storeKeeper.Role == Role.Admin)
                {
                    TempData["Message"] = "Admin already has store keeper rights";
                }

                storeKeeper.DateModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                return RedirectToAction("UserDb");
            }

            catch (System.Exception)
            {

                TempData["ErrorMessage"] = "Something Went Wrong!";
                return RedirectToAction("UserDb");
            }


        }


    }

}