using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace LoanAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LoanController: Controller
    {
        private readonly LoanContext _context;
        private ILoanService _loanService;


        public LoanController(LoanContext context, ILoanService loanService)
        {
            _context = context;
            _loanService = loanService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLogin user)
        {
            var success = _loanService.Login(user, out string token, out string status, out string message);

            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(new { Token = token });
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
            var success = _loanService.GetCurrentUser(User, out string status, out string message, out User currentUser);

            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(currentUser);
        }

        [HttpGet("loans")]
        public IActionResult GetLoans()
        {
            var success = _loanService.GetLoans(User, out string status, out string message, out List<Loan> loans);

            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(loans);
        }

        [HttpGet("loans/{id}")]
        public IActionResult GetLoanById([FromRoute] int id)
        {
            var success = _loanService.GetLoanById(id, User, out string status, out string message, out Loan loan);

            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(loan);
        }
      
        [AllowAnonymous]
        [HttpPost("addUser")]
        public IActionResult AddUser([FromBody] UserRegister userDetails)
        {
            var success = _loanService.AddUser(userDetails, out string status, out string message);

            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(message);
        }


        [HttpPost("addLoan")]
        public IActionResult AddLoan([FromBody] LoanRegister loanDetails)
        {
            var success = _loanService.AddLoan(loanDetails, User, out string status, out string message);
            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(message);
        }


        [HttpDelete("users/deleteUser/{id}")]
        public IActionResult DeleteUser([FromRoute] int id)
        {
            var success = _loanService.DeleteUser(id, User, out string status, out string message);
            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(message);
        }


        [HttpDelete("loans/deleteLoan/{id}")]
        public IActionResult DeleteLoan([FromRoute] int id)
        {
            var success = _loanService.DeleteLoan(id, User, out string status, out string message);
            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(message);
        }



        [HttpPut("loans/updateLoan/{id}")]
        public IActionResult UpdateLoan([FromBody] LoanUpdate loan, [FromRoute] int id)
        {
            var success = _loanService.UpdateLoan(id, loan, User, out string status,out string message);
            if (!success)
            {
                return HandleStatus(status, message);
            }

            return Ok(message);
        }

        [Authorize(Roles = "Accountant")]
        [HttpPut("users/changeStatus/{id}")]
        public IActionResult ChangeUserStatus([FromRoute] int id, [FromQuery, BindRequired] bool isBlocked )
        {
            var success = _loanService.ChangeUserStatus(id, isBlocked, out string message);

            if (success)
                return Ok(message);

            return NotFound(message);
        }


        private IActionResult HandleStatus(string status, string message)
        {
            if (status.StartsWith("NotFound"))
                return NotFound(message);

            if (status.StartsWith("BadRequest"))
                return BadRequest(message);

            if (status.StartsWith("Forbidden"))
                return Forbid(message);

            if (status.StartsWith("Conflict"))
                return Conflict(new { message });

            if (status.StartsWith("Unauthorized"))
                return Unauthorized(message);

            return BadRequest(message);
        }
    }
}
