﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;
using WebApplication1.Models.Interfaces;
using WebApplication1.Models.Repositories;
using WebApplication1.Models.ViewModel;

namespace WebApplication1.Controllers 
{
    public class CustomerController : Controller
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly OrderRepository _orderRepository1;
        public CustomerController(IBrandRepository brandRepository, IProductRepository productRepository, IWishlistRepository wishlistRepository, ICartRepository cartRepoitory, IContactRepository contactRepository, IOrderRepository orderRepository,OrderRepository order)
        {
            _brandRepository = brandRepository;
            _productRepository = productRepository;
            _wishlistRepository = wishlistRepository;
            _cartRepository = cartRepoitory;
            _contactRepository = contactRepository;
            _orderRepository = orderRepository;
            _orderRepository1 = order;
        }
        public IActionResult getAllRegisteredBrands(string status = "Approved")
        {
            var brands = _brandRepository.GetAllBrandsByStatus(status);
            return View(brands);
        }
        [HttpGet]
        public IActionResult Search(string searchTerm, string status = "Approved")
        {
            Console.WriteLine($"Search endpoint hit with term: '{searchTerm}' and status: '{status}'");

            try
            {
                List<Brand> brands;

                if (string.IsNullOrEmpty(searchTerm))
                {
                    brands = _brandRepository.GetAllBrandsByStatus(status);
                    Console.WriteLine($"Returning all {brands.Count} brands (no search term)");
                }
                else
                {
                    brands = _brandRepository.SearchBrandsByName(searchTerm, status);
                    Console.WriteLine($"Returning {brands.Count} filtered brands");
                }

                return PartialView("_BrandsListPartial", brands);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex}");
                return StatusCode(500, "Internal server error");
            }
        }
        public IActionResult GetAllProductsOfCurrentBrand(int brandId, int page = 1)
        {
            int pageSize = 9; // Number of products per page
            var products = _productRepository.GetAllProductsOfCurrentBrand(brandId);

            // Calculate total pages
            var totalProducts = products.Count();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Get the products for the current page
            var paginatedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Pass the paginated data and total pages to the view
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(paginatedProducts);
        }
        [Authorize]
        [HttpGet]
        public IActionResult AddToWishlist(int productId)
        {
            try
            {
                Console.WriteLine($"Product ID is : {productId}");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Get the complete product details
                var product = _productRepository.getProductByID(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction("Index", "Home");
                }

                // Create the Wishlist item with all required properties
                var wishlistItem = new Wishlist
                {
                    userId = userId,
                    productId = product.productId,
                    productName = product.productName,
                    productThumbnailURL = product.productThumbnailURL,
                    price = product.price,
                    brandId = product.brandId,
                    brandName = product.brandName
                };

                bool success = _wishlistRepository.addToWishlist(wishlistItem);
                return RedirectToAction("Wishlist", "Customer");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                TempData["ErrorMessage"] = "Error adding to wishlist";
                return RedirectToAction("Wishlist", "Customer");
            }
        }
        [Authorize]
        [HttpGet]
        public IActionResult RemoveFromWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool success = _wishlistRepository.RemoveFromWishlist(userId, productId);
                return RedirectToAction("Wishlist");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                TempData["ErrorMessage"] = "Error removing from wishlist";
                return RedirectToAction("Wishlist");
            }
        }
        [Authorize]
        public ActionResult Wishlist()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var wishlistItems = _wishlistRepository.GetWishlistProductsOfCurrentUser(userId);
            return View(wishlistItems.Products);
        }
        [HttpGet]
        public IActionResult DetailsOfProduct(int productId)
        {
            var product = _productRepository.getProductByID(productId);
            var reviews = _productRepository.GetReviewsByProductId(productId);
            var relatedProducts = _productRepository.GetProductsByBrandAndCategory(product.brandId, product.productCategory, product.productId);
            var productDetailsViewModel = new Models.ViewModel.ProductDetailsViewModel
            {
                Product = product,
                Reviews = reviews,
                RelatedProducts = relatedProducts
            };
            return View(productDetailsViewModel);
        }
        [HttpPost]
        public ActionResult AddReview(Review review, int productId)
        {
            if (ModelState.IsValid)
            {
                _productRepository.AddReview(review);
                ViewBag.Message = "✅ Your review has been submitted successfully!";
            }
            return RedirectToAction("DetailsOfProduct", new { productId = productId });
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity = 1, string selectedSizes = "")
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Please login to add items to cart";
                    return RedirectToAction("Login", "Account");
                }

                var product = _productRepository.getProductByID(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction("DetailsOfProduct", new { productId });
                }

                // Validate size selection for products that require it
                if (product.AvailableSizes != null && product.AvailableSizes.Count > 0 && string.IsNullOrEmpty(selectedSizes))
                {
                    TempData["ErrorMessage"] = "Please select a size for this product";
                    return RedirectToAction("DetailsOfProduct", new { productId });
                }

                // Handle stock validation
                int availableQuantity = 0;
                bool sizeExists = false;

                if (!string.IsNullOrEmpty(selectedSizes))
                {
                    // Split sizes if multiple are selected (comma-separated)
                    var sizes = selectedSizes.Split(',').Select(s => s.Trim()).ToList();

                    // For now, we'll just use the first size for quantity validation
                    // You might want to handle multiple sizes differently
                    var firstSize = sizes.First();

                    int sizeIndex = product.AvailableSizes?.IndexOf(firstSize) ?? -1;
                    if (sizeIndex >= 0 && sizeIndex < product.stock.Count)
                    {
                        sizeExists = true;
                        if (int.TryParse(product.stock[sizeIndex], out int sizeQuantity))
                        {
                            availableQuantity = sizeQuantity;
                        }
                    }
                }
                else
                {
                    // For products without sizes, use the first stock value
                    if (product.stock.Count > 0 && int.TryParse(product.stock[0], out int firstStock))
                    {
                        availableQuantity = firstStock;
                        sizeExists = true;
                    }
                }

                if (!sizeExists)
                {
                    TempData["ErrorMessage"] = "Selected size is not available";
                    return RedirectToAction("DetailsOfProduct", new { productId });
                }

                // Validate quantity
                if (quantity <= 0)
                {
                    quantity = 1;
                }

                if (quantity > availableQuantity)
                {
                    TempData["ErrorMessage"] = $"Only {availableQuantity} items available in selected size";
                    return RedirectToAction("DetailsOfProduct", new { productId });
                }

                var cartItem = new Cart
                {
                    userId = userId,
                    productId = product.productId,
                    brandName = product.brandName,
                    brandId = product.brandId,
                    productName = product.productName,
                    productThumbnailURL = product.productThumbnailURL,
                    price = product.price,
                    quantity = quantity,
                    subTotal = product.price * quantity,
                    availableStock = availableQuantity,
                    size = selectedSizes
                };

                bool success = _cartRepository.AddToCart(cartItem);

                if (success)
                {
                    TempData["SuccessMessage"] = "Product added to cart successfully";
                }
                else
                {
                    TempData["WarningMessage"] = "Product quantity updated in your cart";
                }

                return RedirectToAction("ViewCart");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                TempData["ErrorMessage"] = "Error adding product to cart. Please try again.";
                return RedirectToAction("DetailsOfProduct", new { productId });
            }
        }
        [Authorize]
        public IActionResult ViewCart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }
                var cartItems = _cartRepository.GetCartProducts(userId);
                ViewBag.availableQuantities = cartItems.availableQuantities;
                return View(cartItems.carts);
            }
            catch (Exception ex)
            {
                // Log the error (you might want to use a proper logging framework)
                Console.WriteLine($"Error in ViewCart: {ex.Message}");

                // Return error view or redirect with error message
                TempData["ErrorMessage"] = "An error occurred while loading your cart";
                return RedirectToAction("Index", "Home");
            }
        }
        [Authorize]
        public IActionResult MoveToCartFromWishlist(int productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var product = _productRepository.getProductByID(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return RedirectToAction("Wishlist");
            }

            // Check if product requires size selection
            if (product.AvailableSizes != null && product.AvailableSizes.Count > 0)
            {
                // Redirect to product details page to select size
                TempData["FromWishlist"] = true;
                return RedirectToAction("DetailsOfProduct", new { productId });
            }
            else
            {
                // For products without sizes, add directly to cart
                var cartItem = new Cart
                {
                    userId = userId,
                    productId = product.productId,
                    brandName = product.brandName,
                    productName = product.productName,
                    productThumbnailURL = product.productThumbnailURL,
                    price = product.price,
                    quantity = 1,
                    size = null // No size needed
                };

                _cartRepository.AddToCart(cartItem);
                _wishlistRepository.RemoveFromWishlist(userId, productId);

                TempData["SuccessMessage"] = "Product added to cart";
                return RedirectToAction("ViewCart");
            }
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCartQuantities([FromBody] List<CartQuantityUpdate> updates)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                foreach (var update in updates)
                {
                    _cartRepository.UpdateQuantity(update.cartId, update.quantity);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quantities: {ex.Message}");
                return StatusCode(500);
            }
        }
        public class CartQuantityUpdate
        {
            public int cartId { get; set; }
            public int quantity { get; set; }
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId, string size)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Login", "Account");
                }

                bool success = _cartRepository.RemoveFromCart(userId, productId, size);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to remove item from cart";
                }
                else
                {
                    TempData["SuccessMessage"] = "Item removed from cart successfully";
                }

                return RedirectToAction("ViewCart");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from cart: {ex.Message}");
                TempData["ErrorMessage"] = "Error removing item from cart";
                return RedirectToAction("ViewCart");
            }
        }
        [HttpGet]
        public IActionResult Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartDetails = _cartRepository.GetCartProducts(userId);
            if (cartDetails.carts == null || !cartDetails.carts.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Calculate totals
            decimal subtotal = cartDetails.carts.Sum(i => i.subTotal);
            decimal shipping = subtotal * 0.05m;
            decimal total = subtotal + shipping;

            var model = new CheckoutViewModel
            {
                CartItems = cartDetails.carts,
                Subtotal = subtotal,
                Shipping = shipping,
                Total = total
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartDetails = _cartRepository.GetCartProducts(userId);
            if (cartDetails.carts == null || !cartDetails.carts.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Create order object
            Order order = new Order
            {
                CustomerName = model.CustomerName,
                Address = model.Address,
                City = model.City,
                Country = model.Country,
                PostCode = model.PostCode,
                Phone = model.Phone,
                Email = model.Email,
                OrderNote = model.OrderNote,
                PaymentMethod = "COD",
                Subtotal = model.Subtotal,
                Shipping = model.Shipping,
                Total = model.Total,
                OrderItems = cartDetails.carts,
                UserId=userId
                
            };

            // Save order to database
            //OrderRepository orderRepo = new OrderRepository("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True");
            var id=_orderRepository1.PlaceOrder(order);
            //int orderId = orderRepo.PlaceOrder(order);
            order.OrderId = id;
            // Clear cart
            _cartRepository.ClearCart(userId);

            // Redirect to confirmation
            return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
        }
        public IActionResult OrderConfirmation(int id)
        {
            var order = _orderRepository.GetOrderById(id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order); 
        }

        public IActionResult BankTransfer()
        {
            return View();
        }
        public IActionResult JazzCash()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SubmitContact(Contact contact)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _contactRepository.AddContact(contact);
                    return Json(new { success = true, message = "Your message has been submitted successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error submitting your message: " + ex.Message });
                }
            }
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = "Validation errors", errors = errors });
        }
        [Authorize]
        public IActionResult MyAccount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            ViewBag.CustomerName= _orderRepository.GetShopkeeperName(userId);
            var orders = _orderRepository.GetOrdersByUserId(userId);
            return View(orders);
        }
        public IActionResult detailsOfOrder(int orderId)
        {
            var products = _orderRepository.GetOrderedProductsByOrderId(orderId);
            return View(products);
        }
    }
}