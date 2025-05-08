using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Interfaces;

public class WishListViewComponent : ViewComponent
{
    private readonly IWishlistRepository _wishRepository;

    public WishListViewComponent(IWishlistRepository wishRepository)
    {
        _wishRepository = wishRepository;
    }

    public IViewComponentResult Invoke()
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int count = 0;

        if (!string.IsNullOrEmpty(userId))
        {
            var products=_wishRepository.GetWishlistProductsOfCurrentUser(userId);
            count = products.Count; 
        }
        return View(count);
    }
}