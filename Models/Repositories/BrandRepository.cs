﻿using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;
namespace WebApplication1.Models.Repositories 
{
    public class BrandRepository : IBrandRepository
    {
        private readonly string _connectionString;
        public BrandRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public void Add(Brand brand)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        @"INSERT INTO Brand 
                (brandName, niche, description, address, socialLink, BrandLogoURL, tagLine ,
                brandOwnerId, brandOwnerName, contact, cnic, bankName, accountHolderName, 
                accountNumber, brandRegistrationDate, brandRegistrationStatus)
                VALUES
                (@BrandName, @Niche, @Description, @Address, @SocialLink, @LogoPath, @tagLine,
                @OwnerId, @OwnerName, @Contact, @Cnic, @BankName, @AccountHolderName,
                @AccountNumber, @RegistrationDate, @Status)", conn);

                    // Add all required parameters
                    cmd.Parameters.AddWithValue("@BrandName", brand.brandName);
                    cmd.Parameters.AddWithValue("@Niche", brand.niche);
                    cmd.Parameters.AddWithValue("@Description", brand.description);
                    cmd.Parameters.AddWithValue("@Address", brand.address);
                    cmd.Parameters.AddWithValue("@SocialLink",brand.socialLink);
                    cmd.Parameters.AddWithValue("@LogoPath", brand.brandLogoURL);
                    cmd.Parameters.AddWithValue("@tagLine", brand.tagLine);
                    cmd.Parameters.AddWithValue("@OwnerId", brand.brandOwnerId);
                    cmd.Parameters.AddWithValue("@OwnerName", brand.brandOwnerName);
                    cmd.Parameters.AddWithValue("@Contact", brand.contact);
                    cmd.Parameters.AddWithValue("@Cnic", brand.cnic);
                    cmd.Parameters.AddWithValue("@BankName", brand.bankName);
                    cmd.Parameters.AddWithValue("@AccountHolderName", brand.accountHolderName);
                    cmd.Parameters.AddWithValue("@AccountNumber", brand.accountNumber);
                    cmd.Parameters.AddWithValue("@RegistrationDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Status", "Pending");
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex}");
                throw;
            }
        }
        public List<Brand> GetAllBrands()
        {
            List<Brand> brands = new List<Brand>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT brandId, brandName, niche , brandOwnerName, contact, brandRegistrationDate , brandRegistrationStatus FROM Brand";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            brands.Add(new Brand
                            {
                                brandId = reader.GetInt32(0),
                                brandName = reader.GetString(1),
                                niche = reader.GetString(2),
                                brandOwnerName = reader.GetString(3),
                                contact = reader.GetString(4),
                                brandRegistrationDate = reader.GetDateTime(5),
                                brandRegistrationStatus = reader.GetString(6)
                            });
                        }
                    }
                }
            }
            return brands;
        }
        public List<Brand> GetAllBrandsByStatus(string status)
        {
            List<Brand> brands = new List<Brand>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT brandId, brandName,niche, brandOwnerName, contact, brandRegistrationDate, brandRegistrationStatus , tagLine, brandLogoURL, description , socialLink,cnic , bankName,accountNumber,accountHolderName  FROM Brand WHERE brandRegistrationStatus = @status";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            brands.Add(new Brand
                            {
                                brandId = reader.GetInt32(0),
                                brandName = reader.GetString(1),
                                niche = reader.GetString(2),
                                brandOwnerName = reader.GetString(3),
                                contact = reader.GetString(4),
                                brandRegistrationDate = reader.GetDateTime(5),
                                brandRegistrationStatus = reader.GetString(6),
                                tagLine = reader.GetString(7),
                                brandLogoURL = reader.GetString(8),
                                description = reader.GetString(9),
                                socialLink=reader.GetString(10),
                                cnic= reader.GetString(11),
                                bankName= reader.GetString(12),
                                accountNumber=reader.GetString(13),
                                accountHolderName=reader.GetString(14)
                            });
                        }
                    }
                }
                conn.Close();
            }
            return brands;
        }
        public void UpdateBrandStatus(int brandId, string status)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "UPDATE Brand SET brandRegistrationStatus = @status WHERE brandId = @brandId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@brandId", brandId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<Brand> SearchBrandsByName(string searchTerm, string status)
        {
            Console.WriteLine($"Repository search: '{searchTerm}' with status '{status}'");

            var brands = new List<Brand>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Brand WHERE brandName LIKE @SearchTerm AND brandRegistrationStatus = @Status";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                cmd.Parameters.AddWithValue("@Status", status);

                Console.WriteLine("Opening connection...");
                con.Open();

                Console.WriteLine("Executing query...");
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    Console.WriteLine("Reading results...");
                    while (rdr.Read())
                    {
                        brands.Add(new Brand()
                        {
                            brandId = Convert.ToInt32(rdr["brandId"]),
                            brandName = rdr["brandName"].ToString(),
                            brandLogoURL = rdr["brandLogoURL"].ToString()
                        });
                    }
                }
            }

            Console.WriteLine($"Found {brands.Count} matching brands");
            return brands;
        }
        public (bool HasBrand, bool IsApproved, string Status) GetBrandStatus(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(
                    @"SELECT 
                COUNT(*) AS HasBrand,
                CASE WHEN brandRegistrationStatus = 'Approved' THEN 1 ELSE 0 END AS IsApproved,
                ISNULL(brandRegistrationStatus, 'No brand registered') AS Status
              FROM Brand 
              WHERE brandOwnerId = @userId
              GROUP BY brandRegistrationStatus",
                    connection);

                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            HasBrand: reader.GetInt32(0) > 0,
                            IsApproved: reader.GetInt32(1) == 1,  // ✅ Fix: Use GetInt32 then compare to 1
                            Status: reader.GetString(2)
                        );
                    }
                }
                return (false, false, "No brand registered");
            }
        }
        public Brand GetBrandByOwnerId(string brandOwnerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                const string query = @"
            SELECT 
                brandId,
                brandName,
                niche,
                description,
                brandLogoURL,
                tagLine,
                brandOwnerName,
                brandRegistrationDate
            FROM Brand
            WHERE brandOwnerId = @BrandOwnerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BrandOwnerId", brandOwnerId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Brand
                            {
                                brandId = reader.GetInt32(reader.GetOrdinal("brandId")),
                                brandName = reader.GetString(reader.GetOrdinal("brandName")),
                                niche = reader.GetString(reader.GetOrdinal("niche")),
                                description = reader.GetString(reader.GetOrdinal("description")),
                                brandLogoURL = reader.GetString(reader.GetOrdinal("brandLogoURL")),
                                tagLine = reader.GetString(reader.GetOrdinal("tagLine")),
                                brandOwnerName = reader.GetString(reader.GetOrdinal("brandOwnerName")),
                                brandRegistrationDate = reader.GetDateTime(reader.GetOrdinal("brandRegistrationDate"))
                            };
                        }
                    }
                }
            }

            return null; 
        }
        public void DeleteStoreAndUser(string userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Step 1: Delete store (Brand)
                string deleteBrandQuery = "DELETE FROM Brand WHERE BrandOwnerId = @UserId";
                using (SqlCommand cmd = new SqlCommand(deleteBrandQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }

                // Step 2: Delete login account (AspNetUsers)
                string deleteUserQuery = "DELETE FROM AspNetUsers WHERE Id = @UserId";
                using (SqlCommand cmd = new SqlCommand(deleteUserQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}