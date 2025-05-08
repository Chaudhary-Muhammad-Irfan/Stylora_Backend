using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;
namespace WebApplication1.Models.Repositories
{
    public class CartRepoitory : ICartRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
        public bool AddToCart(Cart item)
        {
            // Handle multiple sizes if provided (comma-separated)
            var sizes = string.IsNullOrEmpty(item.size)
                ? new[] { (string)null }
                : item.size.Split(',').Select(s => s.Trim()).ToArray();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        const string query = @"
    IF NOT EXISTS (
        SELECT 1 FROM Cart 
        WHERE userId = @userId 
        AND productId = @productId 
        AND (size = @size OR (size IS NULL AND @size IS NULL))
    )
    BEGIN
        INSERT INTO Cart (
            userId, 
            productId,
            brandName,
            productName,
            productThumbnailURL,
            price,
            quantity,
            subTotal,
            size,
            availableStock
        )
        SELECT 
            @userId,
            @productId,
            @brandName,
            @productName,
            @productThumbnailURL,
            @price,
            @quantity,
            @subTotal,
            @size,
            p.stock
        FROM Product p
        WHERE p.productId = @productId
    END
    ELSE
    BEGIN
        UPDATE Cart 
        SET quantity = quantity + @quantity,
            subTotal = subTotal + @subTotal
        WHERE userId = @userId 
        AND productId = @productId 
        AND (size = @size OR (size IS NULL AND @size IS NULL))
    END";
                        foreach (var size in sizes)
                        {
                            using (SqlCommand command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@userId", item.userId);
                                command.Parameters.AddWithValue("@productId", item.productId);
                                command.Parameters.AddWithValue("@brandName", item.brandName);
                                command.Parameters.AddWithValue("@productName", item.productName);
                                command.Parameters.AddWithValue("@productThumbnailURL", item.productThumbnailURL);
                                command.Parameters.AddWithValue("@price", item.price);
                                command.Parameters.AddWithValue("@quantity", item.quantity);
                                command.Parameters.AddWithValue("@subTotal", item.price * item.quantity); // Calculate subtotal
                                command.Parameters.AddWithValue("@size", string.IsNullOrEmpty(size) ? (object)DBNull.Value : size);

                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error adding to cart: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        public (int count , List<Cart> carts) GetCartProducts(string userId)
        {
            List<Cart> cartItems = new List<Cart>();

            const string query = @"
        SELECT 
            cardId,
            userId,
            productId,
            brandName,
            productName,size,
            productThumbnailURL,
            price,
            quantity,
            price * quantity AS subTotal
        FROM Cart
        WHERE userId = @userId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cartItems.Add(new Cart
                            {
                                cardId = reader.GetInt32(0),
                                userId = reader.GetString(1),
                                productId = reader.GetInt32(2),
                                brandName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                productName = reader.GetString(4),
                                size = reader.IsDBNull(5) ? null : reader.GetString(5),
                                productThumbnailURL = reader.GetString(6),
                                price = reader.GetInt32(7),
                                quantity = reader.GetInt32(8),
                                subTotal = reader.GetInt32(9)
                            });
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error getting cart items: {ex.Message}");
                    throw; 
                }
            }
            return (cartItems.Count,  cartItems);
        }
        public bool UpdateQuantity(int cartId, int newQuantity)
        {
            const string query = @"UPDATE Cart SET quantity = @quantity WHERE cardId = @cartId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@cartId", cartId);
                command.Parameters.AddWithValue("@quantity", newQuantity);

                connection.Open();
                int affectedRows = command.ExecuteNonQuery();
                return affectedRows > 0;
            }
        }
        public bool RemoveFromCart(string userId, int productId, string size)
        {
            const string query = @"
        DELETE FROM Cart 
        WHERE userId = @userId 
        AND productId = @productId and size=@size";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@productId", productId);
                command.Parameters.AddWithValue("@size", size);
                try
                {
                    connection.Open();
                    int affectedRows = command.ExecuteNonQuery();
                    return affectedRows > 0;
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error removing from cart: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
