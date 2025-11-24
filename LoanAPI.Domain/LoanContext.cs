using LoanAPI.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanAPI.Domain
{
    public class LoanContext: DbContext
    {

            public LoanContext(DbContextOptions<LoanContext> options)
                  : base(options)
            {}

        public DbSet<User> Users { get; set; }
        public DbSet<Loan> Loans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Loan>()
                .Property(l => l.LoanType)
                .HasConversion<string>();

            modelBuilder.Entity<Loan>()
                .Property(l => l.Currency)
                .HasConversion<string>();

            modelBuilder.Entity<Loan>()
                .Property(l => l.Status)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(r => r.Role)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .HasMany(u => u.Loans)
                .WithOne()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}


