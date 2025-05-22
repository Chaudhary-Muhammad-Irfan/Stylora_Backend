using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.Repositories;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Models.Interfaces;
using System.Security.Claims;
namespace WebApplication1.Controllers
{
    public class AdminController : Controller  
    {
        private readonly IBrandRepository _brandRepo;
        private readonly IContactRepository _contactRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IOrderRepository _orderRepository;

        public AdminController(IBrandRepository brandRepo, IContactRepository contactRepository, IAdminRepository adminRepository, IOrderRepository orderRepository)
        {
            _brandRepo = brandRepo;
            _contactRepository = contactRepository;
            _adminRepository = adminRepository;
            _orderRepository = orderRepository;
        }
        public IActionResult Index()
        {
            int unreadCount = _contactRepository.GetUnreadMessageCount();
            ViewBag.UnreadMessageCount = unreadCount;
            ViewBag.TotalSales = _adminRepository.GetTotalSales().ToString("N0") + " PKR";
            ViewBag.TotalOrders = _adminRepository.GetTotalOrders();
            ViewBag.TotalCustomers = _adminRepository.GetTotalCustomers();
            ViewBag.ApprovedBrands = _adminRepository.GetApprovedBrands();
            ViewBag.last24HoursOrders=_adminRepository.GetLast24HoursOrders();
            ViewBag.last24HoursBrands=_adminRepository.GetLast24HoursApprovedBrands();
            ViewBag.last24HoursCustomers=_adminRepository.GetLast24HoursCustomers();
            ViewBag.last24HoursSales = _adminRepository.GetLast24HoursSales();
            var reviewStats = _adminRepository.GetReviewStats();
            ViewBag.ReviewCount = reviewStats.Item1; 
            ViewBag.AverageRating = reviewStats.Item2;
            ViewBag.RecentOrders = _adminRepository.GetRecentOrders();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.adminName=_orderRepository.GetShopkeeperName(userId);
            return View();
        }
        public IActionResult getAllBrands()
        {
            var brands = _brandRepo.GetAllBrands();
            return View(brands);
        }
        public IActionResult ApproveBrandRegestrationRequest(int id ,string status="Approved")
        {
            _brandRepo.UpdateBrandStatus(id , status);
            return RedirectToAction("getAllApprovedBrands");
        }
        public IActionResult RejectBrandRegestrationRequest(int id , string status="Rejected")
        {
            _brandRepo.UpdateBrandStatus(id, status);
            return RedirectToAction("getAllRejectedBrands");
        }
        public IActionResult getAllPendingBrands()
        {
            var brands = _brandRepo.GetAllBrandsByStatus("Pending");
            return View(brands);
        }
        public IActionResult getAllRejectedBrands()
        {
            var brands = _brandRepo.GetAllBrandsByStatus("Rejected");
            return View(brands);
        }
        public IActionResult getAllApprovedBrands()
        {
            var brands = _brandRepo.GetAllBrandsByStatus("Approved");
            return View(brands);
        }
        public IActionResult Messages()
        {
            var contacts = _contactRepository.GetAllMessages();
            _contactRepository.MarkMessageAsRead();
            return View(contacts);
        }
        public IActionResult RegisteredBrands()
        {
            var brands = _brandRepo.GetAllBrandsByStatus("Approved");
            return View(brands);
        }
        public IActionResult BrandOwners()
        {
            var brands = _brandRepo.GetAllBrandsByStatus("Approved");
            return View(brands);
        }
        public IActionResult Customers()
        {
            var customers=_adminRepository.GetAllDistinctCustomers();
            return View(customers);
        }
        public IActionResult Orders()
        {
            var orders = _adminRepository.GetAllOrdersForAdmin();
            return View(orders);
        }
        public IActionResult detailsOfOrder(int id)
        {
            var products=_orderRepository.GetOrderedProductsByOrderId(id);
            return View(products);
        }
        public IActionResult Report()
        {
            int unreadCount = _contactRepository.GetUnreadMessageCount();
            ViewBag.UnreadMessageCount = unreadCount;
            ViewBag.TotalSales = _adminRepository.GetTotalSales().ToString("N0") + " PKR";
            ViewBag.TotalOrders = _adminRepository.GetTotalOrders();
            ViewBag.TotalCustomers = _adminRepository.GetTotalCustomers();
            ViewBag.ApprovedBrands = _adminRepository.GetApprovedBrands();
            ViewBag.last24HoursOrders = _adminRepository.GetLast24HoursOrders();
            ViewBag.last24HoursBrands = _adminRepository.GetLast24HoursApprovedBrands();
            ViewBag.last24HoursCustomers = _adminRepository.GetLast24HoursCustomers();
            ViewBag.last24HoursSales = _adminRepository.GetLast24HoursSales();
            var reviewStats = _adminRepository.GetReviewStats();
            ViewBag.ReviewCount = reviewStats.Item1;
            ViewBag.AverageRating = reviewStats.Item2;
            ViewBag.RecentOrders = _adminRepository.GetRecentOrders();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.adminName = _orderRepository.GetShopkeeperName(userId);
            return View();
        }
    }
}