using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public User() { }

        // For creating users in code and tests
        public User(int id, string firstname, string lastname,bool isBlocked, List<Loan> loans,double salary, UserRole role, string username, int age, string password)
        {
            Id = Id;
            FirstName = firstname;
            IsBlocked = isBlocked;
            LastName = lastname;
            UserName = username;
            Age = age;
            Salary = salary;
            PasswordHash =password;
            Role =role;
            Loans = loans;
    }
    }

    public enum UserRole
    {
        User = 1,
        Accountant = 2
    }
}
