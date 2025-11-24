using LoanAPI.Data.Models;
using LoanAPI.Domain;
using LoanAPI.Models;
using LoanAPI.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

            _service = new LoanService(_context);
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