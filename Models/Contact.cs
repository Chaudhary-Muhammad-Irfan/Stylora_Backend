namespace WebApplication1.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Message { get; set; }
        public DateTime createdAt { get; set; }
        public bool IsRead { get; set; }

    }
}
