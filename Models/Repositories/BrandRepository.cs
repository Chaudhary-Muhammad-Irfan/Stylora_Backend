using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;
namespace WebApplication1.Models.Repositories 
{
    public class BrandRepository : IBrandRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
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
                string query = "SELECT brandId, brandName,niche, brandOwnerName, contact, brandRegistrationDate, brandRegistrationStatus , tagLine, brandLogoURL, description FROM Brand WHERE brandRegistrationStatus = @status";
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
                                description = reader.GetString(9)
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
    }
}