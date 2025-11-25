using FluentValidation;
using LoanAPI.Models;

namespace LoanAPI.Validators
{
    public class UpdateLoanRequestValidator: AbstractValidator<LoanUpdate>
    {
        public UpdateLoanRequestValidator()
        {
            RuleFor(loan => loan.LoanType).IsInEnum().WithMessage("Please Select The Correct Loan Type");
            RuleFor(loan => loan.Amount).GreaterThan(1000).WithMessage("Please Indicate The Amount > 1000");
            RuleFor(loan => loan.Currency).IsInEnum().WithMessage("Please Select The Correct Currency");
            RuleFor(loan => loan.LoanPeriod).GreaterThanOrEqualTo(1).WithMessage("Loan Period Must Be Greater Than 0");
            RuleFor(loan => loan.Status).IsInEnum().WithMessage("Please Select The Correct Status");
        }
    }
}
