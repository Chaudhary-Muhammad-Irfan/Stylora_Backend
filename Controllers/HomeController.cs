using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using System.Diagnostics;
using System.Security.Claims;
using WebApplication1.Models;
using WebApplication1.Models.Repositories;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProductRepository _repository;
        public HomeController(ILogger<HomeController> logger , ProductRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        public IActionResult Index()
        {
            if (User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "chirfan2002muhammad@gmail.com"))
            {
                return RedirectToAction("Index", "Admin");
            }
            var categories = _repository.GetDistinctCategoriesWithThumbnails();
            var newProducts = _repository.GetNewArrivals();
            ViewBag.Categories = categories;
            ViewBag.Products = newProducts;
            Console.WriteLine($"New products count: {newProducts.Count()}");
            return View();
        }
        [Authorize]
        public IActionResult MyAccount()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [Authorize]
        public IActionResult Cart()
        {
            return View();
        }
        public IActionResult About()
        {
           
            return View();
        }
        public IActionResult DeliveryInformation()
        {
            return View();
        }
        public IActionResult PrivacyPolicy()
        {
            return View();
        }
        public IActionResult TermsAndConditions()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        [Authorize]
        public IActionResult TrackOrder()
        {
            return View();
        }
        public IActionResult Help()
        {
            return View();
        }
        public IActionResult Order()
        {
            return View();
        }
        //public IActionResult CategoriesPartial()
        //{
        //    Console.WriteLine("In partial view's action method.");
        //    List<Product> products = _repository.GetDistinctCategoriesWithThumbnails();
        //    return PartialView("_categories", products);
        //}
    }
}