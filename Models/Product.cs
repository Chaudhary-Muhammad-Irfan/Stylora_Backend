using Microsoft.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Product
    {
        [Key]
        public int productId { get; set; }
        public string? productCode { get; set; }
        public int brandId { get; set; }
        public string? brandName { get; set; }
        public string productName { get; set; } 
        public string productCategory { get; set; }
        public string productDescription { get; set; }
        public string tagLine { get; set; }
        public int price { get; set; }
        public int discount { get; set; }
        public int stock { get; set; }
        [NotMapped]
        public IFormFile productThumbnail { get; set; }
        [NotMapped]
        public IFormFile sizeChart { get; set; }
        [NotMapped]
        public List<IFormFile> productImages { get; set; }
        public string productThumbnailURL { get; set; }
        public List<string> productImagesURL { get; set; }
        public string sizeChartURL { get; set; }
        public List<string> AvailableSizes { get; set; }
        public DateTime DateAdded { get; set; }
    }
}