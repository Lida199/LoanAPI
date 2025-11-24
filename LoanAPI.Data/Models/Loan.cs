using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanAPI.Data.Models
{
    public class Loan
    {
        [Key]
        public int Id { get; private set; }
        public LoanType LoanType { get; set; }
        public double Amount { get; set; }
        public Currency Currency { get; set; }
        public int LoanPeriod { get; set; }
        public LoanStatus Status { get; set; }
        public int UserId { get; set; }
    }

    public enum LoanType
    {
        Auto = 1,
        Rapid = 2,
        Insurance = 3
    }

    public enum Currency
    {
        GEL = 1,
        EUR = 2,
        USD = 3
    }

    public enum LoanStatus
    {
        InProgress = 1,
        Approved = 2,
        Declined = 3
    }
}
