using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using WebApplication1.Models; 
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
        public void addProduct(Product product)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO Product (brandId, brandName,productCode, productName, productCategory, productDescription, 
        tagLine, price, discount, stock, productThumbnailURL, 
        productImagesURL, sizeChartURL, AvailableSizes , DateAdded)
        VALUES 
       (@brandId, @brandName, @productCode, @productName, @productCategory, @productDescription,
        @tagLine, @price, @discount, @stock, @productThumbnailURL, 
        @productImagesURL, @sizeChartURL, @AvailableSizes , GETDATE());
        SELECT SCOPE_IDENTITY();";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@brandId", product.brandId);
                        command.Parameters.AddWithValue("@brandName", product.brandName);
                        command.Parameters.AddWithValue("@productCode", product.productCode);
                        command.Parameters.AddWithValue("@productName", product.productName);
                        command.Parameters.AddWithValue("@productCategory", product.productCategory);
                        command.Parameters.AddWithValue("@productDescription", product.productDescription);
                        command.Parameters.AddWithValue("@tagLine", (object)product.tagLine ?? DBNull.Value);
                        command.Parameters.AddWithValue("@price", product.price);
                        command.Parameters.AddWithValue("@discount", product.discount);
                        command.Parameters.AddWithValue("@stock", product.stock);
                        command.Parameters.AddWithValue("@productThumbnailURL", (object)product.productThumbnailURL ?? DBNull.Value);
                        command.Parameters.AddWithValue("@sizeChartURL", (object)product.sizeChartURL ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateAdded", DateTime.Now);
                        // Handle list serialization
                        var imagesJson = product.productImagesURL != null ?
                            JsonConvert.SerializeObject(product.productImagesURL) :
                            (object)DBNull.Value;
                        command.Parameters.AddWithValue("@productImagesURL", imagesJson);
                        var sizesJson = product.AvailableSizes != null ?
                            JsonConvert.SerializeObject(product.AvailableSizes) :
                            (object)DBNull.Value;
                        command.Parameters.AddWithValue("@AvailableSizes", sizesJson);
                        // command.ExecuteNonQuery();

                        var newId = command.ExecuteScalar();
                        Console.WriteLine($"New product ID: {newId}");


                        Console.WriteLine("Product added to database");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
                throw new ApplicationException("Database operation failed. See inner exception.", ex);
            }
        }
        public void deleteProduct(int productId)
        {
            string sql = "DELETE FROM Product WHERE productId = @productId";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@productId", productId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
        public (int BrandId, string BrandName) GetBrandInfoOfCurrentShopkeeper(string userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @"SELECT brandId, brandName FROM Brand WHERE brandOwnerId = @UserId";
                command.Parameters.AddWithValue("@UserId", userId);
                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int brandId = reader.GetInt32(0);
                            string brandName = reader.GetString(1);
                            return (brandId, brandName);
                        }
                        else
                        {
                            throw new ArgumentException("No brand found for the given user.");
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new ApplicationException("Database error occurred", ex);
                }
            }
        }
        public int GetBrandIdOfCurrentShopkeeper(string userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @" SELECT brandId FROM Brand WHERE brandOwnerId = @UserId";
                command.Parameters.AddWithValue("@UserId", userId);
                try
                {
                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result == null)
                        throw new ArgumentException("User not found");
                    if (result == DBNull.Value)
                        throw new InvalidOperationException("User is not associated with any brand");
                    return Convert.ToInt32(result);
                }
                catch (SqlException ex)
                {
                    throw new ApplicationException("Database error occurred", ex);
                }
            }
        }
        // Getting Prodcuts of currently login shopkeeper
        public List<Product> GetAllProducts(string userId)
        {
            List<Product> products = new List<Product>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"select productName,productCode , productCategory,stock,productThumbnailURL from Product
            WHERE brandId = (select top 1 brandId from Brand WHERE brandOwnerId = @userId order by brandId)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Product product = new Product
                                {
                                    productName = reader.GetString(0),
                                    productCode = reader.GetString(1),
                                    productCategory = reader.GetString(2),
                                    stock = reader.GetInt32(3),
                                    productThumbnailURL=reader.GetString(4)
                                    
                                };
                                products.Add(product);
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"ERROR: {ex}");
                        throw new ApplicationException("Error fetching shopkeeper's products", ex);
                    }
                }
            }

            return products;
        }
        // Getting products of current Brand
        public List<Product> GetAllProductsOfCurrentBrand(int brandId)
        {
            List<Product> products = new List<Product>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT productId, productCode, brandId, brandName, productName, productCategory, 
                productDescription, tagLine, price, discount, stock, productThumbnailURL, 
                productImagesURL, sizeChartURL, AvailableSizes
                FROM Product
                WHERE brandId = @brandId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@brandId",brandId);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Safe JSON deserialization
                                List<string> images = new List<string>();
                                if (!reader.IsDBNull(12))
                                {
                                    try
                                    {
                                        images = JsonConvert.DeserializeObject<List<string>>(reader.GetString(12));
                                    }
                                    catch
                                    {
                                        images = new List<string>();
                                    }
                                }
                                List<string> sizes = new List<string>();
                                if (!reader.IsDBNull(14))
                                {
                                    try
                                    {
                                        sizes = JsonConvert.DeserializeObject<List<string>>(reader.GetString(14));
                                    }
                                    catch
                                    {
                                        sizes = new List<string>();
                                    }
                                }
                                Product product = new Product
                                {
                                    productId = reader.GetInt32(0),
                                    productCode = reader.GetString(1),
                                    brandId = reader.GetInt32(2),
                                    brandName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    productName = reader.GetString(4),
                                    productCategory = reader.GetString(5),
                                    productDescription = reader.GetString(6),
                                    tagLine = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    price = reader.GetInt32(8),
                                    discount = reader.GetInt32(9),
                                    stock = reader.GetInt32(10),
                                    productThumbnailURL = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    productImagesURL = images,
                                    sizeChartURL = reader.IsDBNull(13) ? null : reader.GetString(13),
                                    AvailableSizes = sizes
                                };
                                products.Add(product);
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"ERROR: {ex}");
                        throw new ApplicationException("Error fetching shopkeeper's products", ex);
                    }
                }
            }
            return products;
        }
        public Product getProductByID(int productId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
        SELECT 
            productId,              -- 0
            productCode,            -- 1
            brandId,               -- 2
            brandName,             -- 3
            productName,           -- 4
            productCategory,       -- 5
            productDescription,    -- 6
            tagLine,               -- 7
            price,                 -- 8
            discount,              -- 9
            stock,                 -- 10
            productThumbnailURL,   -- 11
            productImagesURL,      -- 12 (JSON)
            sizeChartURL,          -- 13
            AvailableSizes         -- 14 (JSON)
        FROM Product 
        WHERE productId = @productId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@productId", productId);

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Handle product images
                                List<string> images = new List<string>();
                                if (!reader.IsDBNull(12))
                                {
                                    string imagesJson = reader.GetString(12).Trim();
                                    if (!string.IsNullOrEmpty(imagesJson))
                                    {
                                        try
                                        {
                                            images = JsonConvert.DeserializeObject<List<string>>(imagesJson);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error deserializing images: {ex.Message}");
                                        }
                                    }
                                }

                                // Handle available sizes
                                List<string> sizes = new List<string>();
                                if (!reader.IsDBNull(14))
                                {
                                    string sizesJson = reader.GetString(14).Trim();
                                    if (!string.IsNullOrEmpty(sizesJson))
                                    {
                                        try
                                        {
                                            sizes = JsonConvert.DeserializeObject<List<string>>(sizesJson);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error deserializing sizes: {ex.Message}");
                                        }
                                    }
                                }

                                return new Product
                                {
                                    productId = reader.GetInt32(0),
                                    productCode = reader.GetString(1),
                                    brandId = reader.GetInt32(2),
                                    brandName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    productName = reader.GetString(4),
                                    productCategory = reader.GetString(5),
                                    productDescription = reader.GetString(6),
                                    tagLine = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    price = reader.GetInt32(8),
                                    discount = reader.GetInt32(9),
                                    stock = reader.GetInt32(10),
                                    productThumbnailURL = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    productImagesURL = images ?? new List<string>(),
                                    sizeChartURL = reader.IsDBNull(13) ? null : reader.GetString(13),
                                    AvailableSizes = sizes ?? new List<string>()
                                };
                            }
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving product: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public void AddReview(Review review)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO Review (ProductId, Name, Email, Comment, Rating, CreatedAt) " +
                               "VALUES (@ProductId, @Name, @Email, @Comment, @Rating, @CreatedAt)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductId", review.ProductId);
                cmd.Parameters.AddWithValue("@Name", review.Name);
                cmd.Parameters.AddWithValue("@Email", review.Email);
                cmd.Parameters.AddWithValue("@Comment", review.Comment);
                cmd.Parameters.AddWithValue("@Rating", review.Rating);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public List<Review> GetReviewsByProductId(int productId)
        {
            List<Review> reviews = new List<Review>();

            // Define the SQL query to fetch reviews based on ProductId
            string query = "SELECT ReviewId, ProductId, Name, Email, Comment, Rating, CreatedAt FROM Review WHERE ProductId = @ProductId";

            // Establish connection to the database
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProductId", productId);
                connection.Open();
                // Execute the query and read the results
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Review review = new Review
                        {
                            ReviewId = Convert.ToInt32(reader["ReviewId"]),
                            ProductId = Convert.ToInt32(reader["ProductId"]),
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            Comment = reader["Comment"].ToString(),
                            Rating = Convert.ToInt32(reader["Rating"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };
                        reviews.Add(review);
                    }
                }
            }
            return reviews;
        }
        public DataTable GetReviewsOfShopkeeperProducts(string shopkeeperId)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT 
                p.productCode,
                p.productName,
                r.Name,
                r.Email,
                r.Comment,
                r.Rating,
                r.isRead
            FROM Brand b
            INNER JOIN Product p ON b.brandId = p.brandId
            INNER JOIN Review r ON p.productId = r.ProductId
            WHERE b.brandOwnerId = @shopkeeperId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@shopkeeperId", shopkeeperId);
                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            return dataTable;
        }
        public int CountUnreadReviews(string shopkeeperId)
        {
            int unreadCount = 0;
            string query = @"
        SELECT COUNT(r.ReviewId) 
        FROM Review r
        JOIN Product p ON r.ProductId = p.ProductId
        JOIN Brand b ON p.BrandId = b.BrandId
        WHERE b.BrandOwnerId = @ShopkeeperId 
        AND r.IsRead = 0";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);

                    try
                    {
                        connection.Open();
                        unreadCount = (int)command.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error counting unread reviews: {ex.Message}");
                        throw;
                    }
                }
            }

            return unreadCount;
        }
        public void MarkReviewsAsRead(string shopkeeperId, List<int> reviewIds = null)
        {
            string query;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (reviewIds != null && reviewIds.Any())
                {
                    // Mark specific reviews by ID (if they belong to this shopkeeper)
                    query = @"
                UPDATE r
                SET r.isRead = 1
                FROM Review r
                JOIN Product p ON r.productId = p.productId
                JOIN Brand b ON p.brandId = b.brandId
                WHERE b.brandOwnerId = @ShopkeeperId
                AND r.reviewId IN ({0})";

                    string idList = string.Join(",", reviewIds);
                    query = string.Format(query, idList);
                }
                else
                {
                    query = @"
                UPDATE r
                SET r.isRead = 1
                FROM Review r
                JOIN Product p ON r.productId = p.productId
                JOIN Brand b ON p.brandId = b.brandId
                WHERE b.brandOwnerId = @ShopkeeperId
                AND r.isRead = 0";
                }
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);
                    try
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Marked {rowsAffected} reviews as read");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error marking reviews as read: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public List<Product> GetProductsByBrandAndCategory(int brandId, string productCategory,int productId)
        {
            List<Product> products = new List<Product>();

            // Define the SQL query to fetch products based on BrandId and ProductCategory
            string query = "SELECT productId, productName, productCategory, price,discount,productThumbnailURL FROM Product WHERE (brandId = @BrandId AND productCategory = @ProductCategory) and productId!=@productId";

            // Establish connection to the database
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@BrandId", brandId);
                command.Parameters.AddWithValue("@ProductCategory", productCategory);
                command.Parameters.AddWithValue("@productId", productId);
                connection.Open();

                // Execute the query and read the results
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product product = new Product
                        {
                            productId = Convert.ToInt32(reader["ProductId"]),
                            productName = reader["ProductName"].ToString(),
                            productCategory = reader["ProductCategory"].ToString(),
                            price = Convert.ToInt32(reader["Price"]),
                            discount = Convert.ToInt32(reader["Discount"]),
                            productThumbnailURL = reader["ProductThumbnailURL"].ToString()
                        };
                        products.Add(product);
                    }
                }
            }

            return products;
        }
        public List<Product> GetDistinctCategoriesWithThumbnails()
        {
            var products = new List<Product>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // Modified query to ensure we get valid results
                    string query = @"
            SELECT TOP 5 productId, productCategory, productThumbnailURL
            FROM Product
            WHERE productCategory IS NOT NULL 
            AND productThumbnailURL IS NOT NULL
            AND productThumbnailURL <> ''
            ORDER BY productCategory";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                productId = reader.GetInt32(0),
                                productCategory = reader.GetString(1).Trim(),
                                productThumbnailURL = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Debug.WriteLine($"Repository error: {ex.Message}");
            }

            return products;
        }
        public List<Product> GetNewArrivals()
        {
            List<Product> products = new List<Product>();
            string query = @"SELECT * FROM Product 
                     WHERE DateAdded >= DATEADD(day, -7, GETDATE())";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // Log the values of the product to verify
                    Console.WriteLine($"Product ID: {reader["productId"]}, Date Added: {reader["DateAdded"]}");

                    Product product = new Product
                    {
                        productId = Convert.ToInt32(reader["productId"]),
                        brandId = Convert.ToInt32(reader["brandId"]),
                        productName = reader["productName"].ToString(),
                        productCategory = reader["productCategory"].ToString(),
                        productThumbnailURL = reader["productThumbnailURL"].ToString(),
                        discount = Convert.ToInt32(reader["discount"]),
                        price = Convert.ToInt32(reader["price"]),
                        DateAdded = Convert.ToDateTime(reader["DateAdded"])
                    };
                    products.Add(product);
                }
                conn.Close();
            }

            return products;
        }
        public List<Brand> GetNewlyAddedBrands()
        {
            var newlyAddedBrands = new List<Brand>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                DateTime sevenWeeksAgo = DateTime.Now.AddDays(-49); 
                string query = @" SELECT 
            brandId, 
            brandName, 
            brandLogoURL, 
            brandRegistrationDate,
            description,
            tagLine,
            niche
        FROM Brand
        WHERE brandRegistrationDate >= @SevenWeeksAgo
        AND brandRegistrationStatus = 'Approved'
        ORDER BY brandRegistrationDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SevenWeeksAgo", sevenWeeksAgo);

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        newlyAddedBrands.Add(new Brand
                        {
                            brandId = Convert.ToInt32(reader["brandId"]),
                            brandName = reader["brandName"].ToString(),
                            brandLogoURL = reader["brandLogoURL"].ToString(),
                            brandRegistrationDate = Convert.ToDateTime(reader["brandRegistrationDate"]),
                            description = reader["description"].ToString(),
                            tagLine = reader["tagLine"].ToString(),
                            niche = reader["niche"].ToString()
                        });
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting newly added brands: {ex.Message}");
                    throw;
                }
            }
            return newlyAddedBrands;
        }
        public (int ReviewCount, decimal AverageRating) GetShopkeeperReviewStats(string brandOwnerId)
        {
            int reviewCount = 0;
            decimal averageRating = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                const string query = @"
                SELECT 
                    COUNT(r.ReviewId) AS ReviewCount,
                    AVG(CAST(r.Rating AS DECIMAL(10,2))) AS AverageRating
                FROM Review r
                INNER JOIN Product p ON r.ProductId = p.productId
                INNER JOIN Brand b ON p.brandId = b.brandId
                WHERE b.brandOwnerId = @BrandOwnerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BrandOwnerId", brandOwnerId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reviewCount = reader.GetInt32(reader.GetOrdinal("ReviewCount"));
                            if (!reader.IsDBNull(reader.GetOrdinal("AverageRating")))
                            {
                                averageRating = reader.GetDecimal(reader.GetOrdinal("AverageRating"));
                            }
                        }
                    }
                }
            }

            return (reviewCount, averageRating);
        }
    }
}