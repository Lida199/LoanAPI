using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanAPI.Data.Models
{
    public class User
    {
        [Key]
        public int Id { get; private set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public int Age { get; set; }
        public double Salary { get; set; }
        public bool IsBlocked { get; set; }
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; }
        public List<Loan>? Loans { get; set; }
    }

    public enum UserRole
    {
        User = 1,
        Accountant = 2
    }
}
