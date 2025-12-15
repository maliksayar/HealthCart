using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HealthCart.Models;
using HealthCart.Models.ViewModels;
using HealthCart.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthCart.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SqlDbContext dbContext;

    public HomeController(ILogger<HomeController> logger, SqlDbContext dbContext)
    {
        _logger = logger;
        this.dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // fetch products from the database
            var products = await dbContext.Products.Where(p => p.IsActive).ToListAsync();


            var viewModel = new ProductViewModel
            {
                Products = products
            };

            return View(viewModel);
        }
        catch (System.Exception ex)
        {
            // Log the exception
            ViewBag.ErrorMessage = ex.Message;
            return View("Error");

        }

    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> UserIndex()
    {
        try
        {
            Guid? userId = HttpContext.Items["UserId"] as Guid?;

            // fetch products from the database

            var user = await dbContext.Users.FindAsync(userId);


            var products = await dbContext.Products.Where(p => p.IsActive).ToListAsync();

            if (user == null || products == null)
            {

                ViewBag.ErrorMessage = "Something Went Wrong . Try again after Sometime";
                return View("Error");
            }


            var viewModel = new ProductViewModel
            {
                Products = products,
                User = user
            };

            return View(viewModel);
        }
        catch (System.Exception ex)
        {
            // Log the exception
            ViewBag.ErrorMessage = ex.Message;
            return View("Error");

        }

    }





    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Contact()
    {
        return View();
    }
    public IActionResult About()
    {
        return View();
    }

    public IActionResult ShippingInfo()
    {
        return View();
    }


    public IActionResult ReturnsAndExchange()
    {
        return View();
    }


    public IActionResult TermsConditions()
    {
        return View();
    }

    public IActionResult PrivacyPolicy()
    {
        return View();
    }




    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}