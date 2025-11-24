using FluentValidation;
using LoanAPI.Models;

namespace LoanAPI.Validators
{
    public class AddUserRequestValidator: AbstractValidator<UserRegister>
    {
        public AddUserRequestValidator()
        {
            RuleFor(user => user.FirstName).NotEmpty().MinimumLength(2).WithMessage("User Name Is Required");
            RuleFor(user => user.LastName).NotEmpty().MinimumLength(2).WithMessage("User Surname Is Required"); 
            RuleFor(user => user.UserName).NotEmpty().MinimumLength(6).WithMessage("Username Must Be At Least 6 Characters Long"); 
            RuleFor(user => user.Age).GreaterThanOrEqualTo(18).WithMessage("User Must Be More Than 18 Years Old"); 
            RuleFor(user => user.Salary).GreaterThanOrEqualTo(0).WithMessage("User Salary Is Required"); 
            RuleFor(user => user.Password).NotEmpty().MinimumLength(8).WithMessage("Password Must Be At Least 8 Characters Long"); 
            RuleFor(user => user.Role).IsInEnum().WithMessage("Please Select The Role"); 
        }
    }
}
