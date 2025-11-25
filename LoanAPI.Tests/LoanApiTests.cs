using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LoanAPI.Tests
{
    public class LoanApiTests
    {
        private readonly LoanContext _context;
        private readonly ILoanService _service;

        public LoanApiTests()
        {
            var options = new DbContextOptionsBuilder<LoanContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())   
                .Options;


            _context = new LoanContext(options);
            var appSettings = new AppSettings
            {
                Secret = "secret for commschool final project number two"
            };

            var optionsMock = Microsoft.Extensions.Options.Options.Create(appSettings);

            _service = new LoanService(_context, optionsMock);
        }

        private ClaimsPrincipal CreateUser(int userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
            });

            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public void Login_ValidUser_ReturnsToken()
        {
            // Arrange
            var userToAdd = new User(
                1,
                "name",
                "lastname",
                true,
                [],
                5000,
                UserRole.Accountant,
                "username",
                20,
                null 
            );
            var hasher = new PasswordHasher<User>();
            userToAdd.PasswordHash = hasher.HashPassword(userToAdd, "strongpassword");

            _context.Users.Add(userToAdd);
            _context.SaveChanges();

            var loginRequest = new UserLogin
            {
                UserName = "username",
                Password = "strongpassword"
            };

            // Act
            var result = _service.Login(loginRequest, out string token, out string status, out string message);

            // Assert
            Assert.True(result);
            Assert.NotNull(token);
            Assert.Equal("Success", status);
            Assert.Equal("token Generated Successfully", message);
        }

        [Fact]
        public void GetCurrentUser_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var userToAdd = new User(1, "name", "lastname", true, [], 5000, UserRole.Accountant, "username", 20, "strongpassword");
            _context.Users.Add(userToAdd);
            _context.SaveChanges();
            var user = CreateUser(1, "Accountant");

            // Act
            var result = _service.GetCurrentUser(user, out string status, out string message, out User currentUser);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("User Information Successfully Loaded", message);
            Assert.NotNull(currentUser);
            Assert.Equal(1, currentUser.Id);
        }

        [Fact]
        public void Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var userToAdd = new User(
                1,
                "name",
                "lastname",
                true,
                [],
                5000,
                UserRole.Accountant,
                "username",
                20,
                null
            );

            var hasher = new PasswordHasher<User>();
            userToAdd.PasswordHash = hasher.HashPassword(userToAdd, "correctpassword");

            _context.Users.Add(userToAdd);
            _context.SaveChanges();

            var loginRequest = new UserLogin
            {
                UserName = "username",
                Password = "wrongpassword"
            };

            // Act
            var result = _service.Login(loginRequest, out string token, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Null(token);
            Assert.Equal("Unauthorized", status);
            Assert.Equal("Invalid username or password", message);
        }

        [Fact]
        public void GetCurrentUser_InvalidUserIdClaim_ReturnsNotFound()
        {
            // Arrange
            var user = CreateUser(1, "Accountant");

            // Act
            var result = _service.GetCurrentUser(user, out string status, out string message, out User currentUser);

            // Assert
            Assert.False(result);
            Assert.Equal("NotFound", status);
            Assert.Null(currentUser);
        }

        [Fact]
        public void GetLoans_Accountant_GetsAllLoans()
        {
            // Arrange
            var loanToAdd = new Loan(1, LoanType.Rapid, 5000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            var loanToAdd2 = new Loan(2, LoanType.Auto, 15000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            _context.Loans.Add(loanToAdd);
            _context.Loans.Add(loanToAdd2);
            _context.SaveChanges();
            var user = CreateUser(20, "Accountant");

            // Act
            var result = _service.GetLoans(user, out string status, out string message, out List<Loan> loansList);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("Loans Successfully Loaded", message);
            Assert.NotNull(loansList);
            Assert.Equal(2, loansList.Count);
        }

        [Fact]
        public void GetLoans_UserWithNoLoans_ReturnsNotFound()
        {
            // Arrange
            var user = CreateUser(20, "User");

            // Act
            var result = _service.GetLoans(user, out string status, out string message, out List<Loan> loansList);

            // Assert
            Assert.False(result);
            Assert.Equal("NotFound", status);
            Assert.Equal("No loans found for this user.", message);
            Assert.Null(loansList);
        }

        [Fact]
        public void GetLoanById_LoanNotFound_ReturnsNotFound()
        {
            // Arrange
            var user = CreateUser(3,"Admin");

            // Act
            var result = _service.GetLoanById(99, user, out string status, out string message, out Loan loan);

            // Assert
            Assert.False(result);
            Assert.Equal("NotFound", status);
            Assert.Equal("Loan not found.", message);
            Assert.Null(loan);
        }

        [Fact]
        public void GetLoanById_LoanOwnedByAnotherUser_ReturnsForbidden()
        {
            // Arrange
            var loanToAdd = new Loan(1, LoanType.Rapid, 5000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            _context.Loans.Add(loanToAdd);
            _context.SaveChanges();
            var user = CreateUser(20, "User");

            // Act
            var result = _service.GetLoanById(1, user, out string status, out string message, out Loan loan);

            // Assert
            Assert.False(result);
            Assert.Equal("Forbidden", status);
            Assert.Equal("You cannot access another user's loan.", message);
            Assert.Null(loan);
        }

        [Fact]
        public void GetLoanById_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var loanToAdd = new Loan(1, LoanType.Rapid, 5000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            _context.Loans.Add(loanToAdd);
            _context.SaveChanges();
            var user = CreateUser(12, "User");

            // Act
            var result = _service.GetLoanById(1, user, out string status, out string message, out Loan loan);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("Loan Identified Successfully!", message);
            Assert.NotNull(loan);
            Assert.Equal(1, loan.Id);
        }

        [Fact]
        public void AddUser_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var user = new UserRegister
            {

                FirstName = "1",
                LastName = "lastName1",
                UserName = "Username",
                Password = "NewPassword1",
                Age = 24,
                Salary = 8000,
                Role = UserRole.Accountant
            };

            // Act
            var result = _service.AddUser(user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("BadRequest", status);
        }

        [Fact]
        public void AddUser_UsernameExists_ReturnsConflict()
        {
            // Arrange
            var user = new User
            {

                FirstName = "name",
                LastName = "lastName",
                UserName = "Username",
                PasswordHash = "NewPassword",
                Age = 23,
                Salary = 8000,
                Role = UserRole.Accountant,
                IsBlocked = false
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var user2 = new UserRegister
            {

                FirstName = "name1",
                LastName = "lastName1",
                UserName = "Username",
                Password = "NewPassword1",
                Age = 24,
                Salary = 8000,
                Role = UserRole.Accountant
            };

            // Act
            var result = _service.AddUser(user2, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("Conflict", status);
            Assert.Equal("Username already exists", message);
        }

        [Fact]
        public void AddUser_ValidUser_ReturnsTrue()
        {
            // Arrange
            var user = new UserRegister{

                FirstName = "name",
                LastName = "lastName",
                UserName = "Username",
                Password = "NewPassword",
                Age = 23,
                Salary = 8000,
                Role = UserRole.Accountant
            };
                
            // Act
            var result = _service.AddUser(user, out string status, out string message);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("User added successfully!", message);
        }


        [Fact]
        public void AddLoan_UserBlocked_ShouldReturnForbidden()
        {
            // Arrange
            var userToAdd = new User(1, "name", "lastname", true, [], 5000, UserRole.Accountant, "username", 20, "strongpassword");
            _context.Users.Add(userToAdd);
            _context.SaveChanges();

            var user = CreateUser(1, "User");
            var loan = new LoanRegister { Amount = 10000, LoanType = LoanType.Auto, Currency = Currency.GEL, LoanPeriod = 12 };

            // Act
            var result = _service.AddLoan(loan, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("Forbidden", status);
            Assert.Equal("User is blocked and cannot perform this action.", message);
        }

        [Fact]
        public void AddLoan_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var userToAdd = new User(1, "name", "lastname", false, [], 5000, UserRole.Accountant, "username", 20, "strongpassword");
            _context.Users.Add(userToAdd);
            _context.SaveChanges();

            var user = CreateUser(1,"User");
            var loan = new LoanRegister { Amount = 10000, LoanType = LoanType.Insurance, Currency = Currency.EUR, LoanPeriod = 12 };

            // Act
            var result = _service.AddLoan(loan, user, out string status, out string message);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("Loan added successfully!", message);
        }

        [Fact]
        public void AddLoan_InvalidModel_ShouldReturnBadRequest()
        {
            // Arrange
            var user = CreateUser(1, "User");
            var loan = new LoanRegister { Amount = 100, LoanType = LoanType.Insurance, Currency = Currency.EUR, LoanPeriod = 12 };

            // Act
            var result = _service.AddLoan(loan, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("BadRequest", status);
            Assert.Contains("Amount", "Please Indicate The Amount > 1000;"); 
        }


        [Fact]
        public void DeleteUser_UserDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var user = CreateUser(1, "User");

            // Act
            var result = _service.DeleteUser(999, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("NotFound", status);
            Assert.Equal("No user with provided id.", message);
        }

        [Fact]
        public void DeleteUser_UserTriesToDeleteAnotherUsersAccount_ShouldReturnForbidden()
        {
            // Arrange
            var userToDelete = new User(1, "name", "lastname", false, [], 5000, UserRole.Accountant, "username", 20, "strongpassword");
            _context.Users.Add(userToDelete);
            _context.SaveChanges();

            var user = CreateUser(10, "User");

            // Act
            var result = _service.DeleteUser(1, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("Forbidden", status);
            Assert.Equal("You cannot delete another user's account.", message);
        }

        [Fact]
        public void DeleteUser_UserDeletesOwnAccount_ShouldReturnSuccess()
        {
            // Arrange
            var userToDelete = new User(1, "name", "lastname", false, [], 5000, UserRole.Accountant, "username", 20, "strongpassword");
            _context.Users.Add(userToDelete);
            _context.SaveChanges();

            var user = CreateUser(1, "User");

            // Act
            var result = _service.DeleteUser(1, user, out string status, out string message);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("Successfully deleted user with id 1", message);
            Assert.Null(_context.Users.Find(1));
        }

        [Fact]
        public void DeleteLoan_LoanDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var user = CreateUser(1, "User");

            // Act
            var result = _service.DeleteLoan(999, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("NotFound", status);
            Assert.Equal("No loan with provided id.", message);
        }

        [Fact]
        public void DeleteLoan_UserTriesToDeleteAnotherUsersLoan_ShouldReturnForbidden()
        {
            // Arrange
            var loan = new Loan(1, LoanType.Rapid, 5000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            _context.Loans.Add(loan);
            _context.SaveChanges();

            var user = CreateUser(1, "User"); 

            // Act
            var result = _service.DeleteLoan(1, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("Forbidden", status);
            Assert.Equal("You cannot delete another user's loan.", message);
        }

        [Fact]
        public void DeleteLoan_UserDeletesOwnLoanInProgress_ShouldReturnSuccess()
        {
            // Arrange
            var loan = new Loan(1, LoanType.Rapid, 5000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            _context.Loans.Add(loan);
            _context.SaveChanges();

            var user = CreateUser(12, "User");

            // Act
            var result = _service.DeleteLoan(1, user, out string status, out string message);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("Successfully deleted loan with id 1", message);
            Assert.Null(_context.Loans.Find(1));
        }

        [Fact]
        public void UpdateLoan_LoanDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = CreateUser(1, "User");

            // Act
            var result = _service.UpdateLoan(999, new LoanUpdate(), user,
                                            out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("NotFound", status);
            Assert.Equal("No loan found to update.", message);
        }

        [Fact]
        public void UpdateLoan_UserUpdatesOwnLoan_Success()
        {
            //Arange
            var loan = new Loan(1, LoanType.Rapid, 5000, Currency.GEL, 12, LoanStatus.InProgress, 12);
            
            _context.Loans.Add(loan);
            _context.SaveChanges();

            var user = CreateUser(12, "User");

            var model = new LoanUpdate
            {
                LoanType = LoanType.Auto,
                Amount = 20000,
                Currency = Currency.USD,
                LoanPeriod = 10
            };

            // Act
            var result = _service.UpdateLoan(1, model, user, out string status, out string message);

            // Assert
            Assert.True(result);
            Assert.Equal("Success", status);
            Assert.Equal("Successfully updated loan with id 1.", message);

            var updated = _context.Loans.Find(1);
            Assert.Equal(20000, updated.Amount);
            Assert.Equal(Currency.USD, updated.Currency);
        }

        [Fact]
        public void UpdateLoan_UserUpdatesOtherUsersLoan_Forbidden()
        {
            // Arrange
            var loan = new Loan(1, LoanType.Rapid, 3000, Currency.GEL, 12, LoanStatus.InProgress, 12);

            _context.Loans.Add(loan);
            _context.SaveChanges();

            var user = CreateUser(5, "User");

            var model = new LoanUpdate
            {
                LoanType = LoanType.Auto,
                Amount = 2000,
                Currency = Currency.USD,
                LoanPeriod = 10
            };

            // Act
            var result = _service.UpdateLoan(1, model, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("Forbidden", status);
            Assert.Equal("You cannot update another user's loan.", message);
        }

        [Fact]
        public void UpdateLoan_UserUpdatesLoanNotInProgress_Forbidden()
        {
            // Arrange
            var loan = new Loan(1, LoanType.Rapid, 3000, Currency.GEL, 12, LoanStatus.Declined, 12);

            _context.Loans.Add(loan);
            _context.SaveChanges();

            var user = CreateUser(12, "User");

            var model = new LoanUpdate
            {
                LoanType = LoanType.Rapid,
                Amount = 6000,
                Currency = Currency.GEL,
                LoanPeriod = 6
            };

            // Act
            var result = _service.UpdateLoan(1, model, user, out string status, out string message);

            // Assert
            Assert.False(result);
            Assert.Equal("Forbidden", status);
            Assert.Equal("You can only update loans that are InProgress.", message);
        }

        [Fact]
        public void ChangeUserStatus_WithValidId_ShouldSucceed()
        {
            // Arrange
            var user = new User(1, "name", "lastname", false, [], 5000, UserRole.Accountant, "username", 20, "strongpassword");
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _service.ChangeUserStatus(1, true, out string message);

            // Assert
            Assert.True(result);
            Assert.True(_context.Users.First().IsBlocked);
        }

        [Fact]
        public void ChangeUserStatus_WithInvalidId_ShouldFail()
        {
            // Act
            var result = _service.ChangeUserStatus(99, false, out string message);

            // Assert
            Assert.False(result);
        }
    }
}