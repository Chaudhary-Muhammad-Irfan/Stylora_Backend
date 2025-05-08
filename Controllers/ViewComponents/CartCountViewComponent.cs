using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Interfaces;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartRepository _cartRepository;

    public CartCountViewComponent(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public IViewComponentResult Invoke()
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int count = 0;

        if (!string.IsNullOrEmpty(userId))
        {
            var cartItems = _cartRepository.GetCartProducts(userId);
            count = cartItems.count;
        }

        return View(count); 
    }
}
