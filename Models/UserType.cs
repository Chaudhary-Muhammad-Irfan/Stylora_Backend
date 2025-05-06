using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class UserType:IdentityUser
    {
        public string name { get; set; }
    }
}