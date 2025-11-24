using LoanAPI.Data.Models;

namespace LoanAPI.Models
{
    public class LoanUpdate
    {
        public LoanType LoanType { get; set; }
        public double Amount { get; set; }
        public Currency Currency { get; set; }
        public int LoanPeriod { get; set; }
        public LoanStatus? Status { get; set; }
    }
}
