using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly string _connectionString;
        public ContactRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public void AddContact(Contact contact)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Contacts 
                        (Name, Subject, Email, Phone, Message, createdAt, IsRead) 
                        VALUES 
                        (@Name, @Subject, @Email, @Phone, @Message, @CreatedAt, @IsRead)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", contact.Name);
                    cmd.Parameters.AddWithValue("@Subject", contact.Subject);
                    cmd.Parameters.AddWithValue("@Email", contact.Email);
                    cmd.Parameters.AddWithValue("@Phone", contact.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Message", contact.Message);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@IsRead", false);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public int GetUnreadMessageCount()
        {
            int count = 0;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Contacts WHERE IsRead = 0";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                count = (int)cmd.ExecuteScalar();
            }
            return count;
        } 
        public List<Contact> GetAllMessages()
        {
            List<Contact> contacts = new List<Contact>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Contacts ORDER BY IsRead ASC, createdAt DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    contacts.Add(new Contact
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["Name"].ToString(),
                        Subject = reader["Subject"].ToString(),
                        Email = reader["Email"].ToString(),
                        Phone = reader["Phone"].ToString(),
                        Message = reader["Message"].ToString(),
                        createdAt = Convert.ToDateTime(reader["createdAt"]),
                        IsRead = Convert.ToBoolean(reader["IsRead"])
                    });
                }
            }
            return contacts;
        }
        public void MarkMessageAsRead()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE Contacts SET IsRead = 1";
                SqlCommand cmd = new SqlCommand(query, conn);
               // cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

    }
}
