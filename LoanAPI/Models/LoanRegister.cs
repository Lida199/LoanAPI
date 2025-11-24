using LoanAPI.Data.Models;

namespace LoanAPI.Models
{
    public class LoanRegister
    {
        public LoanType LoanType { get; set; }
        public double Amount { get; set; }
        public Currency Currency { get; set; }
        public int LoanPeriod { get; set; }
    }
}
