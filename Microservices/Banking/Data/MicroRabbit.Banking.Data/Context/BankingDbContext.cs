using MicroRabbit.Banking.Domain.Models;
using Microsoft.EntityFrameworkCore;



namespace MicroRabbit.Banking.Data.Context
{
    public class BankingDbContext : DbContext
    {
        public BankingDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .Property(a => a.AccountBalance)
                .HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
    }
}
