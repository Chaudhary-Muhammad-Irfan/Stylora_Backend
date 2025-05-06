namespace WebApplication1.Models.Interfaces
{
    public interface IBrandRepository
    {
        void Add(Brand brand);
        void UpdateBrandStatus(int brandId, string status);
        List<Brand> GetAllBrands();  
        List<Brand> GetAllBrandsByStatus(string status);
    }
}