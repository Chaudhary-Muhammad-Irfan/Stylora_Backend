using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.Repositories;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private readonly BrandRepository _brandRepo;
         
        public AdminController(BrandRepository brandRepo)
        {
            _brandRepo = brandRepo;
        }
        public IActionResult Index()
        {
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
    }
}