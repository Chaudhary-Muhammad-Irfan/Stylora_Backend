using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.Repositories;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Models.Interfaces;
namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private readonly BrandRepository _brandRepo;
        private readonly IContactRepository _contactRepository;

        public AdminController(BrandRepository brandRepo, IContactRepository contactRepository)
        {
            _brandRepo = brandRepo;
            _contactRepository = contactRepository;
        }
        public IActionResult Index()
        {
            int unreadCount = _contactRepository.GetUnreadMessageCount();
            ViewBag.UnreadMessageCount = unreadCount;
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
        public IActionResult RegisteredUsers()
        {
            return View();
        }
    }
}