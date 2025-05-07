using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
        public void AddContact(Contact contact)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO Contacts (FirstName, LastName, Email, Phone, Message) VALUES (@FirstName, @LastName, @Email, @Phone, @Message)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", contact.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", contact.LastName);
                    cmd.Parameters.AddWithValue("@Email", contact.Email);
                    cmd.Parameters.AddWithValue("@Phone", contact.Phone);
                    cmd.Parameters.AddWithValue("@Message", contact.Message);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
