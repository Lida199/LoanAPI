using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Validators;
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
    }
    public class LoanService: ILoanService
    {

        private readonly LoanContext _context;

        public LoanService(LoanContext context)
        {
            _context = context;
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
