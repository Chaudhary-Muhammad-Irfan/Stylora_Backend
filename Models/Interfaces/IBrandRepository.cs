using Microsoft.Data.SqlClient;

namespace WebApplication1.Models.Interfaces
{
    public interface IBrandRepository
    {
        void Add(Brand brand);
        void UpdateBrandStatus(int brandId, string status);
        List<Brand> GetAllBrands();  
        List<Brand> GetAllBrandsByStatus(string status);
        public List<Brand> SearchBrandsByName(string searchTerm, string status);
        public (bool HasBrand, bool IsApproved, string Status) GetBrandStatus(string userId);
        public Brand GetBrandByOwnerId(string brandOwnerId);
        public void DeleteStoreAndUser(string userId);
    }
} 