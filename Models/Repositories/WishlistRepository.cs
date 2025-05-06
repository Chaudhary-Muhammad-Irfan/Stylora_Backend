using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;
namespace WebApplication1.Models.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
        public bool addToWishlist(Wishlist item)
        {
            const string query = @"
    INSERT INTO Wishlist (
        userId, 
        brandId, 
        brandName, 
        productId, 
        productName, 
        productThumbnailURL, 
        price, 
        stock
    )
    SELECT 
        @userId, 
        @brandId, 
        @brandName, 
        p.productId, 
        p.productName, 
        p.productThumbnailURL, 
        p.price, 
        p.stock
    FROM Product p
    WHERE p.productId = @productId
    AND NOT EXISTS (
        SELECT 1 FROM Wishlist 
        WHERE userId = @userId AND productId = @productId
    )";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", item.userId);
                command.Parameters.AddWithValue("@brandId", item.brandId);
                command.Parameters.AddWithValue("@brandName", item.brandName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@productId", item.productId);

                try
                {
                    connection.Open();
                    int affectedRows = command.ExecuteNonQuery();
                    return affectedRows > 0;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error adding to wishlist: {ex.Message}");
                    return false;
                }
            }
        } 
        public List<Product> GetWishlistProductsOfCurrentUser(string userId)
        {
            List<Product> wishlistProducts = new List<Product>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT productId,productName,productThumbnailURL,
                price,brandId,brandName,stock from Wishlist WHERE userId = @userId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Product product = new Product
                            {
                                productId = Convert.ToInt32(reader["productId"]),
                                productName = reader["productName"].ToString(),
                                productThumbnailURL = reader["productThumbnailURL"].ToString(),
                                price = Convert.ToInt32(reader["price"]),
                                brandId = Convert.ToInt32(reader["brandId"]),
                                brandName = reader["brandName"].ToString(),
                                stock = Convert.ToInt32(reader["stock"])
                            };

                            wishlistProducts.Add(product);
                        }
                    }
                }
            }
            return wishlistProducts;
        }
        public bool RemoveFromWishlist(string userId, int productId)
        {
            const string query = @"
        DELETE FROM Wishlist 
        WHERE userId = @userId AND productId = @productId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@productId", productId);

                try
                {
                    connection.Open();
                    int affectedRows = command.ExecuteNonQuery();
                    return affectedRows > 0;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error removing from wishlist: {ex.Message}");
                    return false;
                }
            }
        }
    }
}