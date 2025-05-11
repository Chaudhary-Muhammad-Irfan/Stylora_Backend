using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
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
                        foreach (var size in sizes)
                        {
                            // First, get the available stock for this size
                            int availableStock = 0;
                            string getStockQuery = @"
                        SELECT p.stock, p.AvailableSizes
                        FROM Product p
                        WHERE p.productId = @productId";

                            using (SqlCommand stockCommand = new SqlCommand(getStockQuery, connection, transaction))
                            {
                                stockCommand.Parameters.AddWithValue("@productId", item.productId);
                                using (SqlDataReader reader = stockCommand.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        string stockJson = reader.GetString(0);
                                        string availableSizesJson = reader.GetString(1);

                                        // Parse the JSON arrays
                                        var sizesList = JsonConvert.DeserializeObject<List<string>>(availableSizesJson);
                                        var stockList = JsonConvert.DeserializeObject<List<string>>(stockJson);

                                        // Find the index of the selected size
                                        if (size != null)
                                        {
                                            int sizeIndex = sizesList.IndexOf(size);
                                            if (sizeIndex >= 0 && sizeIndex < stockList.Count)
                                            {
                                                if (int.TryParse(stockList[sizeIndex], out int stock))
                                                {
                                                    availableStock = stock;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // For products without sizes, use the first stock value
                                            if (stockList.Count > 0 && int.TryParse(stockList[0], out int stock))
                                            {
                                                availableStock = stock;
                                            }
                                        }
                                    }
                                }
                            }

                            // Now handle the cart operation
                            const string query = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM Cart
                        WHERE userId = @userId
                        AND productId = @productId
                        AND (size = @size OR (size IS NULL AND @size IS NULL))
                    )
                    BEGIN
                        -- Check stock
                        IF @quantity > @availableStock
                        BEGIN
                            ;THROW 51000, 'Requested quantity exceeds available stock', 1
                        END
                        
                        INSERT INTO Cart (
                            userId, productId, brandName, brandId, productName,
                            productThumbnailURL, price, quantity, subTotal, size, availableStock
                        )
                        VALUES (
                            @userId, @productId, @brandName, @brandId, @productName,
                            @productThumbnailURL, @price, @quantity, @subTotal, @size, @availableStock
                        )
                    END
                    ELSE
                    BEGIN
                        -- Check if updated quantity would exceed stock
                        DECLARE @currentQuantity INT;
                        SELECT @currentQuantity = quantity FROM Cart 
                        WHERE userId = @userId AND productId = @productId 
                        AND (size = @size OR (size IS NULL AND @size IS NULL));
                        
                        IF (@currentQuantity + @quantity) > @availableStock
                        BEGIN
                            ;THROW 51000, 'Requested quantity exceeds available stock', 1
                        END
                        
                        UPDATE Cart
                        SET quantity = quantity + @quantity,
                            subTotal = subTotal + @subTotal,
                            availableStock = @availableStock
                        WHERE userId = @userId
                        AND productId = @productId
                        AND (size = @size OR (size IS NULL AND @size IS NULL))
                    END";

                            using (SqlCommand command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@userId", item.userId);
                                command.Parameters.AddWithValue("@productId", item.productId);
                                command.Parameters.AddWithValue("@brandName", item.brandName);
                                command.Parameters.AddWithValue("@brandId", item.brandId);
                                command.Parameters.AddWithValue("@productName", item.productName);
                                command.Parameters.AddWithValue("@productThumbnailURL", item.productThumbnailURL);
                                command.Parameters.AddWithValue("@price", item.price);
                                command.Parameters.AddWithValue("@quantity", item.quantity);
                                command.Parameters.AddWithValue("@subTotal", item.price * item.quantity);
                                command.Parameters.AddWithValue("@size", string.IsNullOrEmpty(size) ? (object)DBNull.Value : size);
                                command.Parameters.AddWithValue("@availableStock", availableStock);

                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (SqlException ex) when (ex.Number == 51000)
                    {
                        transaction.Rollback();
                        throw new Exception(ex.Message);
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
        public (int count, List<int> availableQuantities, List<Cart> carts) GetCartProducts(string userId)
        {
            List<Cart> cartItems = new List<Cart>();
            List<int> availableQuantities = new List<int>();

            const string query = @"
SELECT 
    C.cardId,
    C.userId,
    C.productId,
    P.brandName AS BrandName,
    P.productName AS ProductName,
    C.size AS ProductSize,
    P.productThumbnailURL AS ProductThumbnailURL,
    P.price AS ProductPrice,
    C.quantity AS CartQuantity,
    P.price * C.quantity AS subTotal,
    P.stock AS ProductStock,
    P.AvailableSizes AS AvailableSizes,
    P.brandId AS brandId
FROM Cart C
INNER JOIN Product P ON C.productId = P.productId
WHERE C.userId = @userId";

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
                            // Parse the available stock for the specific size
                            int availableStock = 0;
                            string stockJson = reader.GetString(10);
                            string availableSizesJson = reader.IsDBNull(11) ? null : reader.GetString(11);
                            string selectedSize = reader.IsDBNull(5) ? null : reader.GetString(5);

                            if (!string.IsNullOrEmpty(stockJson) && !string.IsNullOrEmpty(availableSizesJson))
                            {
                                try
                                {
                                    var sizesList = JsonConvert.DeserializeObject<List<string>>(availableSizesJson);
                                    var stockList = JsonConvert.DeserializeObject<List<string>>(stockJson);

                                    if (selectedSize != null)
                                    {
                                        // Find the quantity for the selected size
                                        int sizeIndex = sizesList.IndexOf(selectedSize);
                                        if (sizeIndex >= 0 && sizeIndex < stockList.Count)
                                        {
                                            if (int.TryParse(stockList[sizeIndex], out int stock))
                                            {
                                                availableStock = stock;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // For products without sizes, use the first quantity
                                        if (stockList.Count > 0 && int.TryParse(stockList[0], out int stock))
                                        {
                                            availableStock = stock;
                                        }
                                    }
                                }
                                catch (Exception jsonEx)
                                {
                                    Console.WriteLine($"Error parsing stock JSON: {jsonEx.Message}");
                                }
                            }

                            var cartItem = new Cart
                            {
                                cardId = reader.GetInt32(0),
                                userId = reader.GetString(1),
                                productId = reader.GetInt32(2),
                                brandName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                productName = reader.GetString(4),
                                size = selectedSize,
                                productThumbnailURL = reader.GetString(6),
                                price = reader.GetInt32(7),
                                quantity = reader.GetInt32(8),
                                subTotal = reader.GetInt32(9),
                                availableStock = availableStock,
                                brandId = reader.GetInt32(12)
                            };

                            cartItems.Add(cartItem);
                            availableQuantities.Add(availableStock);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error getting cart items: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General error getting cart items: {ex.Message}");
                    throw;
                }
            }

            return (cartItems.Count, availableQuantities, cartItems);
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
        public void ClearCart(string userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "DELETE FROM Cart WHERE userId = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
 