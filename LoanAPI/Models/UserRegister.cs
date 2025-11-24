using LoanAPI.Data.Models;

namespace LoanAPI.Models
{
    public class UserRegister
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Age { get; set; }
        public double Salary { get; set; }
        public UserRole Role { get; set; }
    }
}
