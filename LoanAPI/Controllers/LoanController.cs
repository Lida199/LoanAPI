using FluentValidation;
using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Services;
using LoanAPI.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoanAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LoanController: Controller
    {
        private readonly LoanContext _context;
        private readonly AppSettings _appSettings;
        private ILoanService _loanService;


        public LoanController(LoanContext context, ILoanService loanService, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _loanService = loanService;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLogin user)
        {
            var person = _context.Users.SingleOrDefault(x => x.UserName == user.UserName);
            if (person == null)
            {
                return Unauthorized("Invalid username or password");
            }
            else
            {
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(person,person.PasswordHash, user.Password);

                if (result == PasswordVerificationResult.Failed)
                {
                    return Unauthorized("Invalid username or password");
                }
                var tokenString = GenerateToken(person);
                 return Ok(new
                 {
                    Token = tokenString
                 });
            }
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


        [Authorize(Roles = "Accountant")]
        [HttpGet("users")]
        public  IActionResult GetUsers()
        {
            var users = _context.Users.Include(u => u.Loans).ToList();
            return Ok(users);
        }

        [Authorize(Roles = "Accountant")]
        [HttpGet("users/{id}")]
        public IActionResult GetUser([FromRoute] int id)
        {
            var user = _context.Users.Include(u => u.Loans).SingleOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound("User not found");
            return Ok(user);
        }

        [HttpGet("users/getCurrentUserInfo")]
        public IActionResult GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return NotFound("User ID Not Found");
            }

            var user = _context.Users
                .Where(u => u.Id == userId)
                .Include(u=> u.Loans)
                .FirstOrDefault();

            if (user == null)
                return NotFound("User Information Not Found");

            return Ok(user);
        }

        [HttpGet("loans")]
        public IActionResult GetLoans()
        {
            if (User.IsInRole("Accountant"))
            {
                var loans = _context.Loans.ToList();
                return Ok(loans);
            }
            else if (User.IsInRole("User"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return NotFound("User ID Not Found");
                }

                var loans = _context.Loans
                    .Where(l => l.UserId == userId)
                    .ToList();

                if (loans.Count == 0)
                    return NotFound("No loans found for this user.");

                return Ok(loans);
            }
            else
            {
                return Forbid("Not Allowed To Access Loans");
            }
        }

        [HttpGet("loans/{id}")]
        public IActionResult GetLoanById([FromRoute] int id)
        {
            var success = _loanService.GetLoanById(id, User, out string status, out string message, out Loan loan);

            //ლიდა აქედან გაიტანე საერთოში 
            if (!success)
            {
                if (status.StartsWith("NotFound"))
                    return NotFound(message);

                if (status.StartsWith("BadRequest"))
                    return BadRequest(message);

                if (status.StartsWith("Forbidden"))
                    return Forbid(message);

                if (status.StartsWith("Conflict"))
                    return Conflict(new { message = message });
            }

            return Ok(loan);
            //{
            //    var loan = _context.Loans.Find(id);
            //    if (loan == null)
            //        return NotFound("Loan not found.");

            //    if (User.IsInRole("User"))
            //    {
            //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //        if (!int.TryParse(userIdClaim, out var userId))
            //        {
            //            return NotFound("User ID not found.");
            //        }

            //        if (loan.UserId != userId)
            //        {
            //            return Forbid("You cannot access another user's loan.");
            //        }
            //    }
            //    return Ok(loan);
            //}
        }
      
        [AllowAnonymous]
        [HttpPost("addUser")]
        public IActionResult AddUser([FromBody] UserRegister userDetails)
        {
            var success = _loanService.AddUser(userDetails, User, out string status, out string message);

            //ლიდა აქედან გაიტანე საერთოში 
            if (!success)
            {
                if (status.StartsWith("NotFound"))
                    return NotFound(message);

                if (status.StartsWith("BadRequest"))
                    return BadRequest(message);

                if (status.StartsWith("Forbidden"))
                    return Forbid(message);

                if (status.StartsWith("Conflict"))
                    return Conflict(new { message = message });
            }

            return Ok(message);

            //var validator = new AddUserRequestValidator();
            //var hasher = new PasswordHasher<User>();
            //var result = validator.Validate(userDetails);
            //if (result.IsValid)
            //{
            //    if (_context.Users.Any(x => x.UserName == userDetails.UserName))
            //    {
            //        return Conflict(new { message = "Username already exists" });
            //    }

            //    var user = new User
            //    {
            //        FirstName = userDetails.FirstName,
            //        LastName = userDetails.LastName,
            //        UserName = userDetails.UserName,
            //        Age = userDetails.Age,
            //        Salary = userDetails.Salary,
            //        IsBlocked = false,
            //        Role = userDetails.Role,
            //    };

            //    user.PasswordHash = hasher.HashPassword(user, userDetails.Password);

            //    _context.Users.Add(user);
            //    _context.SaveChanges();
            //    return Ok("User added successfully!");
            //}
            //else
            //{
            //    var errors = result.Errors.Select(error => error.ErrorMessage).ToList();
            //    return BadRequest(errors);
            //}
        }


        [HttpPost("addLoan")]
        public IActionResult AddLoan([FromBody] LoanRegister loanDetails)
        {
            var success = _loanService.AddLoan(loanDetails, User, out string status, out string message);
            if (!success)
            {
                if (status.StartsWith("NotFound"))
                    return NotFound(message);

                if (status.StartsWith("BadRequest"))
                    return BadRequest(message);

                if (status.StartsWith("Forbidden"))
                    return Forbid(message);
            }

            return Ok(message);
            //var validator = new AddLoanRequestValidator();
            //var result = validator.Validate(loanDetails);
            //if (result.IsValid)
            //{
            //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //    if (!int.TryParse(userIdClaim, out var userId))
            //    {
            //        return NotFound("User ID Not Found");
            //    }

            //    var user = _context.Users.Find(userId);
            //    if(user == null)
            //    {
            //        return NotFound("User Not Found");
            //    }

            //    if (!user.IsBlocked)
            //    {
            //    var loan = new Loan
            //    {
            //        LoanType = loanDetails.LoanType,
            //        Amount = loanDetails.Amount,
            //        Currency = loanDetails.Currency,
            //        LoanPeriod = loanDetails.LoanPeriod,
            //        Status = LoanStatus.InProgress,
            //        UserId = userId,
            //    };

            //    _context.Loans.Add(loan);
            //    _context.SaveChanges();
            //    return Ok("Loan added successfully!");
            //    }

            //    return Forbid("User is blocked and cannot perform this action.");
            //}
            //else
            //{
            //    var errors = result.Errors.Select(error => error.ErrorMessage).ToList();
            //    return BadRequest(errors);
            //}
        }


        [HttpDelete("users/deleteUser/{id}")]
        public IActionResult DeleteUser([FromRoute] int id)
        {
            var success = _loanService.DeleteUser(id, User, out string status, out string message);
            if (!success)
            {
                if (status.StartsWith("NotFound"))
                    return NotFound(message);

                if (status.StartsWith("BadRequest"))
                    return BadRequest(message);

                if (status.StartsWith("Forbidden"))
                    return Forbid(message);
            }

            return Ok(message);
            //var user = _context.Users.Find(id);
            //if (user == null)
            //    return NotFound("No user with provided id.");

            //if (User.IsInRole("User"))
            //{
            //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //    if (!int.TryParse(userIdClaim, out var currentUserId))
            //        return NotFound("User ID Not Found.");

            //    if (currentUserId != id)
            //        return Forbid("You cannot delete another user's account.");
            //}

            //_context.Users.Remove(user);
            //_context.SaveChanges();
            //return Ok($"Successfully deleted user with id {id}");
        }


        [HttpDelete("loans/deleteLoan/{id}")]
        public IActionResult DeleteLoan([FromRoute] int id)
        {
            var success = _loanService.DeleteLoan(id, User, out string status, out string message);
            if (!success)
            {
                if (status.StartsWith("NotFound"))
                    return NotFound(message);

                if (status.StartsWith("BadRequest"))
                    return BadRequest(message);

                if (status.StartsWith("Forbidden"))
                    return Forbid(message);
            }

            return Ok(message);
            //var loan = _context.Loans.Find(id);
            //if (loan == null)
            //    return NotFound("No loan with provided id.");

            //if (User.IsInRole("User"))
            //{
            //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //    if (!int.TryParse(userIdClaim, out var currentUserId))
            //        return NotFound("User ID Not Found.");

            //    if (currentUserId != loan.UserId)
            //        return Forbid("You cannot delete another user's loan.");
            //    if (loan.Status != LoanStatus.InProgress)
            //        return Forbid("You cannot delete loan that is approved or declined.");
            //}

            //_context.Loans.Remove(loan);
            //_context.SaveChanges();

            //return Ok($"Successfully deleted loan with id {id}");
        }



        [HttpPut("loans/updateLoan/{id}")]
        public IActionResult UpdateLoan([FromBody] LoanUpdate loan, [FromRoute] int id)
        {
            var success = _loanService.UpdateLoan(id, loan, User, out string status,out string message);
            if (!success)
            {
                if (status.StartsWith("NotFound"))
                    return NotFound(message);

                if (status.StartsWith("BadRequest"))
                    return BadRequest(message);

                if (status.StartsWith("Forbidden"))
                    return Forbid(message);
            }

            return Ok(message);
        }

        // ლიდა ესაც გადააკეთე როგორც საჭიროა
        [Authorize(Roles = "Accountant")]
        [HttpPut("users/changeStatus/{id}")]
        public IActionResult ChangeUserStatus([FromRoute] int id, [FromQuery, BindRequired] bool isBlocked )
        {
            var success = _loanService.ChangeUserStatus(id, isBlocked, out string message);

            if (success)
                return Ok(message);

            return NotFound(message);
        }
    }
}
