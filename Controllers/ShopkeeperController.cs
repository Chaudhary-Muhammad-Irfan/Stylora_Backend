using AspNetCoreGeneratedDocument;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client; 
using System.Configuration;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.Models.Repositories;

namespace WebApplication1.Controllers 
{
    [Authorize(Policy = "ShopkeeperOnly")]
    public class ShopkeeperController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly BrandRepository _brandRepository;
        private readonly ProductRepository _productRepository;
        private readonly UserManager<UserType> _userManager;
        public ShopkeeperController(
             IWebHostEnvironment env,
             BrandRepository brandRepository,
             ProductRepository productRepository,
             UserManager<UserType> userManager)
        {
            _env = env;
            _brandRepository = brandRepository;
            _productRepository = productRepository;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.isShopkeeper)
            {
                // This shouldn't happen because of our policy, but just in case
                return RedirectToAction("AccessDenied", "Account");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var brandStatus = _brandRepository.GetBrandStatus(userId);
            int count = _productRepository.CountUnreadReviews(userId);
            ViewBag.Count = count;
            ViewBag.HasApprovedBrand = brandStatus.IsApproved;
            ViewBag.BrandStatus = brandStatus.Status;
            return View();
        }
        [HttpGet]
        public IActionResult RegisterBrand()
        {
            return View(new Brand());
        }
        private static bool _isProcessing = false;
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterBrand(Brand brand, IFormFile brandLogo)
        {
            if (_isProcessing)
            {
                return Json(new { success = false, message = "Processing request, please wait." });
            }
            _isProcessing = true;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _isProcessing = false;
                return Json(new { success = false, message = "User not authenticated" });
            }
            brand.brandOwnerId = userId;
            if (brandLogo != null)
            {
                brand.brandLogoURL = SaveImage(brandLogo);
            }
            else
            {
                _isProcessing = false;
                return Json(new { success = false, message = "Brand logo is required" });
            }
            ModelState.Clear();
            TryValidateModel(brand);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                Console.WriteLine("Validation failed: " + string.Join(", ", errors));
                _isProcessing = false;
                return Json(new { success = false, message = "Validation failed", errors });
            }
            try
            {
                _brandRepository.Add(brand);

                _isProcessing = false;
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                _isProcessing = false;
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult registerProduct()
        {
            return View(new Product());
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult registerProduct(Product product, IFormFile productThumbnail, List<IFormFile> productImages, IFormFile sizeChart)
        {
            try
            {
                // Get user/brand info
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var brandDetails = _productRepository.GetBrandInfoOfCurrentShopkeeper(userId);
                // Assign brand details to product
                product.AvailableSizes = Request.Form["AvailableSizes"].ToList();
                product.brandId = brandDetails.BrandId;
                product.brandName = brandDetails.BrandName;
                // Process thumbnail
                if (productThumbnail != null && productThumbnail.Length > 0)
                {
                    product.productThumbnailURL = SaveImage(productThumbnail);
                    ModelState.Remove("productThumbnailURL");
                }
                var brandSubstring = product.brandName.Length >= 5 ? product.brandName.Substring(0, 5) : product.brandName;
                var productNameSubstring = product.productName.Length >= 5 ? product.productName.Substring(0, 5) : product.productName;

                // Creating product code
                Random random = new Random();
                int uniqueNumber = random.Next(1, 99999);  // Generates a random number between 1 and 99999
                var ProductCode = $"{brandSubstring}-{productNameSubstring}-{uniqueNumber}";
                product.productCode = ProductCode;
                Console.WriteLine($"Product Code: {product.productCode}");
                // Process gallery images
                if (productImages != null && productImages.Count > 0)
                {
                    product.productImagesURL = ImagesToUrls(productImages);
                    ModelState.Remove("productImagesURL");
                }

                // Process size chart image (NEW)
                if (sizeChart != null && sizeChart.Length > 0)
                {
                    product.sizeChartURL = SaveImage(sizeChart);
                    ModelState.Remove("sizeChartURL");
                }
                // Handle sizes
                product.AvailableSizes = Request.Form["AvailableSizes"].ToList();

                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {x.Value.Errors.First().ErrorMessage}");
                    Console.WriteLine("Validation Errors:\n" + string.Join("\n", errors));
                    return View(product);
                }

                // Add to database
                _productRepository.addProduct(product);
                // Success - redirect to confirmation or listing
                return RedirectToAction("getAllProducts"); // Fixed action name
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
                ModelState.AddModelError("", "An error occurred while saving the product.");
                return View(product);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int productId)
        {
            _productRepository.deleteProduct(productId);
            return RedirectToAction("GetAllProducts"); 
        }
        public IActionResult GetAllProducts()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var products = _productRepository.GetAllProducts(userId);
            return View(products);
        }
        // A function to save the single image and return the URL
        private string SaveImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("brandLogo", "Please upload a logo");
                return "";
            }
            else
            {
                // Generate unique filename
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string uploadsFolder = Path.Combine(_env.WebRootPath, "img", "UploadedImages");
                // Create directory if it doesn't exist
                Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                return Path.Combine("img", "UploadedImages", fileName).Replace("\\", "/");
            }
        }
        // A function to convert multiple images to the URLs
        private List<string> ImagesToUrls(List<IFormFile> images)
        {
            var urls = new List<string>();
            foreach (var image in images)
            {
                urls.Add(SaveImage(image));
            }
            return urls;
        }
        public IActionResult AllProductReviews()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            DataTable reviews = _productRepository.GetReviewsOfShopkeeperProducts(userId);
            _productRepository.MarkReviewsAsRead(userId);
            return View(reviews);
        }

    }
}