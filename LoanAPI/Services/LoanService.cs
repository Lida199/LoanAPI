using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LoanAPI.Services
{
    public interface ILoanService
    {
        bool ChangeUserStatus(int id, bool isBlocked, out string message);
        bool UpdateLoan(int id, LoanUpdate model, ClaimsPrincipal user, out string status, out string message);
        public bool DeleteLoan(int id, ClaimsPrincipal user, out string status, out string message);
        public bool DeleteUser(int id, ClaimsPrincipal user, out string status, out string message);
        public bool AddLoan(LoanRegister loanDetails, ClaimsPrincipal user, out string status, out string message);
        public bool AddUser(UserRegister userDetails, ClaimsPrincipal user, out string status, out string message);
        public bool GetLoanById(int id, ClaimsPrincipal user, out string status, out string message, out Loan loan);

    }
    public class LoanService: ILoanService
    {

        private readonly LoanContext _context;

        public LoanService(LoanContext context)
        {
            _context = context;
        }

        public bool GetLoanById(int id, ClaimsPrincipal user, out string status, out string message, out Loan loan)
        {
            
            var selectedLoan = _context.Loans.Find(id);
            if (selectedLoan == null)
            {
                message = "Loan not found.";
                status = "NotFound";
                loan = null;
                return false;
            }
                //return NotFound("Loan not found.");

            if (user.IsInRole("User"))
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                message = "User ID not found.";
                status = "NotFound";
                loan = null;
                return false;
                //return NotFound("User ID not found.");
                }

                if (selectedLoan.UserId != userId)
                {
                message = "You cannot access another user's loan.";
                status = "Forbidden";
                loan = null;
                return false;
                //return Forbid("You cannot access another user's loan.");
                }
            }
            message = "Loan Identified Successfully!";
            status = "Success";
            loan = selectedLoan;
            return true;
            //return Ok(selectedLoan);

        }

        public bool AddUser(UserRegister userDetails, ClaimsPrincipal user, out string status, out string message)
        {
            var validator = new AddUserRequestValidator();
            var hasher = new PasswordHasher<User>();
            var result = validator.Validate(userDetails);
            if (result.IsValid)
            {
                if (_context.Users.Any(x => x.UserName == userDetails.UserName))
                {
                    status = "Conflict";
                    message = "Username already exists";
                    return false;
                    //return Conflict(new { message = "Username already exists" });
                }

                var userToAdd = new User
                {
                    FirstName = userDetails.FirstName,
                    LastName = userDetails.LastName,
                    UserName = userDetails.UserName,
                    Age = userDetails.Age,
                    Salary = userDetails.Salary,
                    IsBlocked = false,
                    Role = userDetails.Role,
                };

                userToAdd.PasswordHash = hasher.HashPassword(userToAdd, userDetails.Password);

                _context.Users.Add(userToAdd);
                _context.SaveChanges();
                status = "Success";
                message = "User added successfully!";
                return true;
                //return Ok("User added successfully!");
            }
            else
            {
                message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                status = "BadRequest";
                return false;
                //var errors = result.Errors.Select(error => error.ErrorMessage).ToList();
                //return BadRequest(errors);
            }
        }

            public bool AddLoan(LoanRegister loanDetails, ClaimsPrincipal user, out string status, out string message)
        {
            var validator = new AddLoanRequestValidator();
            var result = validator.Validate(loanDetails);
            if (result.IsValid)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    status = "NotFound";
                    message = "User ID Not Found";
                    return false;
                }

                var selectedUser = _context.Users.Find(userId);
                if (selectedUser == null)
                {
                    status = "NotFound";
                    message = "User Not Found";
                    return false;
                    //return NotFound("User Not Found");
                }

                if (!selectedUser.IsBlocked)
                {
                    var loan = new Loan
                    {
                        LoanType = loanDetails.LoanType,
                        Amount = loanDetails.Amount,
                        Currency = loanDetails.Currency,
                        LoanPeriod = loanDetails.LoanPeriod,
                        Status = LoanStatus.InProgress,
                        UserId = userId,
                    };

                    _context.Loans.Add(loan);
                    _context.SaveChanges();
                    //return Ok("Loan added successfully!");
                    status = "Success";
                    message = "Loan added successfully!";
                    return true;
                }

                status = "Forbidden";
                message = "User is blocked and cannot perform this action.";
                return false;
                //return Forbid("User is blocked and cannot perform this action.");
            }
            else
            {
                //var errors = result.Errors.Select(error => error.ErrorMessage).ToList();
                //return BadRequest(errors);
                status = "BadRequest";
                message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                return false;
            }
        }

            public bool DeleteUser(int id, ClaimsPrincipal user, out string status, out string message)
        {
            var selectedUser = _context.Users.Find(id);
            if (selectedUser == null)
            {
                status = "NotFound";
                message = "No user with provided id.";
                return false;
            }
                //return NotFound("No user with provided id.");

            if (user.IsInRole("User"))
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var currentUserId))
                {
                    status = "NotFound";
                    message = "User ID Not Found.";
                    return false;
                }
                //return NotFound("User ID Not Found.");

                if (currentUserId != id)
                {
                    status = "Forbidden";
                    message = "You cannot delete another user's account.";
                    return false;
                }
                //return Forbid("You cannot delete another user's account.");
            }

            _context.Users.Remove(selectedUser);
            _context.SaveChanges();
            status = "Success";
            message = $"Successfully deleted user with id {id}";
            return true;
        }

        public bool DeleteLoan(int id, ClaimsPrincipal user, out string status, out string message)
        {
            var loan = _context.Loans.Find(id);
            if (loan == null)
            {
                status = "NotFound";
                message = "No loan with provided id.";
                return false;
            }

            if (user.IsInRole("User"))
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var currentUserId))
                {
                    status = "NotFound";
                    message = "User ID Not Found.";
                    return false;
                }

                if (currentUserId != loan.UserId)
                {
                    status = "Forbidden";
                    message = "You cannot delete another user's loan.";
                    return false;
                }

                if (loan.Status != LoanStatus.InProgress)
                {
                    status = "Forbidden";
                    message = "You cannot delete loan that is approved or declined.";
                    return false;
                }
            }

            _context.Loans.Remove(loan);
            _context.SaveChanges();

            status = "Success";
            message = ($"Successfully deleted loan with id {id}");
            return true;
        }

        public bool UpdateLoan(int id, LoanUpdate model, ClaimsPrincipal user, out string status, out string message)
        {
            var selectedLoan = _context.Loans.Find(id);
            if (selectedLoan == null)
            {
                status = "NotFound";
                message = "No loan found to update.";
                return false;
            }

            var validator = new UpdateLoanRequestValidator();
            var validationResult = validator.Validate(model);

            if (!validationResult.IsValid)
            {
                status = "BadRequest";
                message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return false;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var currentUserId);

    
            if (user.IsInRole("User"))
            {
                if (selectedLoan.UserId != currentUserId)
                {
                    status = "Forbidden";
                    message = "You cannot update another user's loan.";
                    return false;
                }

                if (selectedLoan.Status != LoanStatus.InProgress)
                {
                    status = "Forbidden";
                    message = "You can only update loans that are InProgress.";
                    return false;
                }
            }
            selectedLoan.LoanType = model.LoanType;
            selectedLoan.Amount = model.Amount;
            selectedLoan.Currency = model.Currency;
            selectedLoan.LoanPeriod = model.LoanPeriod;

            if (user.IsInRole("Accountant"))
            {
                if (!model.Status.HasValue)
                {
                    status = "BadRequest";
                    message = "Status is required for updating a loan.";
                    return false;
                }

                selectedLoan.Status = model.Status.Value;
            }

            _context.SaveChanges();
            status = "Success";
            message = $"Successfully updated loan with id {id}.";
            return true;

        }

        public bool ChangeUserStatus(int id, bool isBlocked, out string message)
        {
            var user = _context.Users.Find(id);

            if (user == null)
            {
                message = "No User With Provided Id";
                return false;
            }

            user.IsBlocked = isBlocked;
            _context.SaveChanges();

            message = "Successfully Updated The User Status";
            return true;
        }
    }
}
