using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoanAPI.Services
{
    public interface ILoanService
    {
        public bool Login(UserLogin loginRequest, out string token, out string status, out string message);
        bool ChangeUserStatus(int id, bool isBlocked, out string message);
        bool UpdateLoan(int id, LoanUpdate model, ClaimsPrincipal user, out string status, out string message);
        public bool DeleteLoan(int id, ClaimsPrincipal user, out string status, out string message);
        public bool DeleteUser(int id, ClaimsPrincipal user, out string status, out string message);
        public bool AddLoan(LoanRegister loanDetails, ClaimsPrincipal user, out string status, out string message);
        public bool AddUser(UserRegister userDetails, out string status, out string message);
        public bool GetLoanById(int id, ClaimsPrincipal user, out string status, out string message, out Loan loan);
        public bool GetLoans(ClaimsPrincipal user, out string status, out string message, out List<Loan> loansList);
        public bool GetCurrentUser(ClaimsPrincipal user, out string status, out string message, out User currentUser);
    }
    public class LoanService: ILoanService
    {

        private readonly LoanContext _context;
        private readonly AppSettings _appSettings;
        private readonly ILogger<LoanService> _logger;



        public LoanService(LoanContext context, IOptions<AppSettings> appSettings, ILogger<LoanService> logger)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        private string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                    new Claim(ClaimTypes.Name,user.UserName),
                    new Claim(ClaimTypes.Role,user.Role.ToString()),
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool Login(UserLogin loginRequest, out string token, out string status,out string message)
        {
            var person = _context.Users.SingleOrDefault(x => x.UserName == loginRequest.UserName);
            if (person == null)
            {
                token = null;
                status = "Unauthorized";
                message = "Invalid username or password";
                _logger.LogError($"User Login-{status}:{message}");
                return false;
            }
            else
            {
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(person, person.PasswordHash, loginRequest.Password);

                if (result == PasswordVerificationResult.Failed)
                {
                    token = null;
                    status = "Unauthorized";
                    message = "Invalid username or password";
                    _logger.LogError($"User Login-{status}:{message}");
                    return false;
                }
                var tokenString = GenerateToken(person);
                token = tokenString;
                status = "Success";
                message = "token Generated Successfully";
                _logger.LogInformation($"User Login-{status}:{message} for user-{person.UserName}");
                return true;
            }
        }

        public bool GetCurrentUser(ClaimsPrincipal user, out string status, out string message, out User currentUser)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                currentUser = null;
                status = "NotFound";
                message = "User ID Not Found";
                _logger.LogError($"Current User Details-{status}:{message}");
                return false;
            }

            var selectedUser = _context.Users
                .Where(u => u.Id == userId)
                .Include(u => u.Loans)
                .FirstOrDefault();

            if (selectedUser == null)
            {
                currentUser = null;
                status = "NotFound";
                message = "User Information Not Found";
                _logger.LogError($"Current User Details-{status}:{message}");
                return false;
            }

            currentUser = selectedUser;
            status = "Success";
            message = "User Information Successfully Loaded";
            _logger.LogInformation($"Current User Details-{status}:{message} for user {userId}");
            return true; 
        }

        public bool GetLoans(ClaimsPrincipal user, out string status, out string message, out List<Loan> loansList)
        {
            if (user.IsInRole("Accountant"))
            {
                var loans = _context.Loans.ToList();
                loansList = loans;
                message = "Loans Successfully Loaded";
                status = "Success";
                _logger.LogInformation($"Getting Loans-{status}:{message}");
                return true;
            }
            else if (user.IsInRole("User"))
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    loansList = null;
                    message = "User ID Not Found";
                    status = "NotFound";
                    _logger.LogError($"Getting Loans-{status}:{message}");
                    return false;
                }

                var loans = _context.Loans
                    .Where(l => l.UserId == userId)
                    .ToList();

                if (loans.Count == 0)
                {
                    loansList = null;
                    message = "No loans found for this user.";
                    status = "NotFound";
                    _logger.LogError($"Getting Loans-{status}:{message}");
                    return false;
                }

                loansList = loans;
                message = "Loans Successfully Loaded";
                status = "Success";
                _logger.LogInformation($"Getting Loans-{status}:{message}");
                return true;
            }
            else
            {
                loansList = null;
                message = "Not Allowed To Access Loans";
                status = "Forbidden";
                _logger.LogError($"Getting Loans-{status}:{message}");
                return false;
            }
        }

        public bool GetLoanById(int id, ClaimsPrincipal user, out string status, out string message, out Loan loan)
        {
            
        var selectedLoan = _context.Loans.Find(id);
        if (selectedLoan == null)
        {
            message = "Loan not found.";
            status = "NotFound";
            loan = null;
            _logger.LogError($"Getting Loans By ID-{status}:{message}");
            return false;
        }

        if (user.IsInRole("User"))
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                message = "User ID not found.";
                status = "NotFound";
                loan = null;
                _logger.LogError($"Getting Loans By ID-{status}:{message}");
                return false;
            }

            if (selectedLoan.UserId != userId)
            {
                message = "You cannot access another user's loan.";
                status = "Forbidden";
                loan = null;
                _logger.LogError($"Getting Loans By ID-{status}:{message}");
                return false;
            }
        }
        message = "Loan Identified Successfully!";
        status = "Success";
        loan = selectedLoan;
        _logger.LogInformation($"Getting Loans By ID-{status}:{message} with ID-{selectedLoan.Id}");
        return true;
        }

        public bool AddUser(UserRegister userDetails, out string status, out string message)
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
                    _logger.LogError($"Adding User-{status}:{message}");
                    return false;
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
                _logger.LogInformation($"Adding User-{status}:{message} with username: {userToAdd.UserName}");
                return true;
            }
            else
            {
                message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                status = "BadRequest";
                _logger.LogError($"Adding User-{status}:{message}");
                return false;
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
                    _logger.LogError($"Adding Loan-{status}:{message}");
                    return false;
                }

                var selectedUser = _context.Users.Find(userId);
                if (selectedUser == null)
                {
                    status = "NotFound";
                    message = "User Not Found";
                    _logger.LogError($"Adding Loan-{status}:{message}");
                    return false;
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
                    status = "Success";
                    message = "Loan added successfully!";
                    _logger.LogInformation($"Adding Loan-{status}:{message}");
                    return true;
                }

                status = "Forbidden";
                message = "User is blocked and cannot perform this action.";
                _logger.LogError($"Adding Loan-{status}:{message}");
                return false;
            }
            else
            {
                status = "BadRequest";
                message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                _logger.LogError($"Adding Loan-{status}:{message}");
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
            _logger.LogError($"Delete User-{status}:{message}");
            return false;
        }

        if (user.IsInRole("User"))
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                status = "NotFound";
                message = "User ID Not Found.";
                _logger.LogError($"Delete User-{status}:{message}");
                return false;
            }

            if (currentUserId != id)
            {
                status = "Forbidden";
                message = "You cannot delete another user's account.";
                _logger.LogError($"Delete User-{status}:{message}");
                return false;
            }
        }

        _context.Users.Remove(selectedUser);
        _context.SaveChanges();
        status = "Success";
        message = $"Successfully deleted user with id {id}";
        _logger.LogInformation($"Delete User-{status}:{message}");
        return true;
        }

        public bool DeleteLoan(int id, ClaimsPrincipal user, out string status, out string message)
        {
            var loan = _context.Loans.Find(id);
            if (loan == null)
            {
                status = "NotFound";
                message = "No loan with provided id.";
                _logger.LogError($"Delete Loan-{status}:{message}");
                return false;
            }

            if (user.IsInRole("User"))
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var currentUserId))
                {
                    status = "NotFound";
                    message = "User ID Not Found.";
                    _logger.LogError($"Delete Loan-{status}:{message}");
                    return false;
                }

                if (currentUserId != loan.UserId)
                {
                    status = "Forbidden";
                    message = "You cannot delete another user's loan.";
                    _logger.LogError($"Delete Loan-{status}:{message}");
                    return false;
                }

                if (loan.Status != LoanStatus.InProgress)
                {
                    status = "Forbidden";
                    message = "You cannot delete loan that is approved or declined.";
                    _logger.LogError($"Delete Loan-{status}:{message}");
                    return false;
                }
            }

            _context.Loans.Remove(loan);
            _context.SaveChanges();

            status = "Success";
            message = ($"Successfully deleted loan with id {id}");
            _logger.LogInformation($"Delete Loan-{status}:{message}");
            return true;
        }

        public bool UpdateLoan(int id, LoanUpdate model, ClaimsPrincipal user, out string status, out string message)
        {
            var selectedLoan = _context.Loans.Find(id);
            if (selectedLoan == null)
            {
                status = "NotFound";
                message = "No loan found to update.";
                _logger.LogError($"Update Loan-{status}:{message}");
                return false;
            }

            var validator = new UpdateLoanRequestValidator();
            var validationResult = validator.Validate(model);

            if (!validationResult.IsValid)
            {
                status = "BadRequest";
                message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogError($"Update Loan-{status}:{message}");
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
                    _logger.LogError($"Update Loan-{status}:{message}");
                    return false;
                }

                if (selectedLoan.Status != LoanStatus.InProgress)
                {
                    status = "Forbidden";
                    message = "You can only update loans that are InProgress.";
                    _logger.LogError($"Update Loan-{status}:{message}");
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
                    _logger.LogError($"Update Loan-{status}:{message}");
                    return false;
                }

                selectedLoan.Status = model.Status.Value;
            }

            _context.SaveChanges();
            status = "Success";
            message = $"Successfully updated loan with id {id}.";
            _logger.LogInformation($"Update Loan-{status}:{message}");
            return true;

        }

        public bool ChangeUserStatus(int id, bool isBlocked, out string message)
        {
            var user = _context.Users.Find(id);

            if (user == null)
            {
                message = "No User With Provided Id";
                _logger.LogError($"Change User Status-{message}");
                return false;
            }

            user.IsBlocked = isBlocked;
            _context.SaveChanges();

            message = "Successfully Updated The User Status";
            _logger.LogInformation($"Change User Status-{message}");
            return true;
        }
    }
}
