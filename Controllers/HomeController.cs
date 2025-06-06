using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using System.Diagnostics;
using System.Security.Claims;
using WebApplication1.Models;
using WebApplication1.Models.Interfaces;  
using WebApplication1.Models.Repositories;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Controllers 
{
    public class HomeController : Controller 
    {
        private readonly ILogger<HomeController> _logger; 
        private readonly IProductRepository _repository;
        public HomeController(ILogger<HomeController> logger , IProductRepository repository)
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
            var brands = _repository.GetNewlyAddedBrands();
            ViewBag.Categories = categories;
            ViewBag.Products = newProducts;
            ViewBag.Brands = brands;
            var data = _repository.GetCountsSummary();
            ViewBag.Customer = data.NonShopkeeperCount;
            ViewBag.Product = data.ProductCount;
            ViewBag.Brand = data.BrandCount;
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
    }
}