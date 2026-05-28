using Microsoft.EntityFrameworkCore;
using SB_BusinessObjects.Entities;

namespace SB_BusinessObjects
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ExpensePayer> ExpensePayers { get; set; } = null!;
        public DbSet<ExpenseSlice> ExpenseSlices { get; set; } = null!;
        public DbSet<SettleTransaction> SettleTransactions { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<GroupInvite> GroupInvites { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình bảng Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique(); // Chỉ cho phép email là duy nhất
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.BankCode).HasMaxLength(20);
                entity.Property(e => e.BankAccountNo).HasMaxLength(50);
                entity.Property(e => e.BankAccountName).HasMaxLength(150);
                entity.Property(e => e.BankVerificationProvider).HasMaxLength(50);
            });

            // 2. Cấu hình bảng Groups
            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("Groups");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Description).HasMaxLength(500);

                // Quan hệ Group - User (Người tạo)
                entity.HasOne(g => g.CreatedBy)
                    .WithMany(u => u.CreatedGroups)
                    .HasForeignKey(g => g.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 3. Cấu hình bảng GroupMembers
            modelBuilder.Entity<GroupMember>(entity =>
            {
                entity.ToTable("GroupMembers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nickname).IsRequired().HasMaxLength(100);

                // Quan hệ Member - Group (Cascade delete khi xóa Group)
                entity.HasOne(m => m.Group)
                    .WithMany(g => g.Members)
                    .HasForeignKey(m => m.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ Member - User (Liên kết tài khoản thật)
                entity.HasOne(m => m.User)
                    .WithMany(u => u.GroupMemberships)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 4. Cấu hình bảng Expenses
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.ToTable("Expenses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(250);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.SplitMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Category).HasMaxLength(50).HasDefaultValue("Other");

                // Quan hệ Expense - Group (Cascade delete)
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Expenses)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ Expense - User (Người nhập bill)
                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 5. Cấu hình bảng ExpensePayers (Composite Key)
            modelBuilder.Entity<ExpensePayer>(entity =>
            {
                entity.ToTable("ExpensePayers");
                entity.HasKey(e => new { e.ExpenseId, e.MemberId });
                entity.Property(e => e.AmountPaid).HasPrecision(18, 2);

                entity.HasOne(e => e.Expense)
                    .WithMany(p => p.Payers)
                    .HasForeignKey(e => e.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Member)
                    .WithMany(m => m.ExpensesPaid)
                    .HasForeignKey(e => e.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 6. Cấu hình bảng ExpenseSlices (Composite Key)
            modelBuilder.Entity<ExpenseSlice>(entity =>
            {
                entity.ToTable("ExpenseSlices");
                entity.HasKey(e => new { e.ExpenseId, e.MemberId });
                entity.Property(e => e.AmountOwed).HasPrecision(18, 2);

                entity.HasOne(e => e.Expense)
                    .WithMany(s => s.Slices)
                    .HasForeignKey(e => e.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Member)
                    .WithMany(m => m.ExpensesOwed)
                    .HasForeignKey(e => e.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 7. Cấu hình bảng SettleTransactions
            modelBuilder.Entity<SettleTransaction>(entity =>
            {
                entity.ToTable("SettleTransactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TransferReference).HasMaxLength(50);
                entity.HasIndex(e => e.TransferReference).IsUnique();

                // Quan hệ Transaction - Group (Cascade delete)
                entity.HasOne(t => t.Group)
                    .WithMany(g => g.SettleTransactions)
                    .HasForeignKey(t => t.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ Transaction - Debtor (Người nợ)
                // Phải dùng Restrict/NoAction để tránh lỗi "multiple cascade paths" trong MySQL
                entity.HasOne(t => t.Debtor)
                    .WithMany(m => m.SettleTransactionsAsDebtor)
                    .HasForeignKey(t => t.DebtorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Quan hệ Transaction - Creditor (Người nhận)
                entity.HasOne(t => t.Creditor)
                    .WithMany(m => m.SettleTransactionsAsCreditor)
                    .HasForeignKey(t => t.CreditorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 8. Cấu hình bảng Notifications
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(500);

                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 9. Cấu hình bảng GroupInvites
            modelBuilder.Entity<GroupInvite>(entity =>
            {
                entity.ToTable("GroupInvites");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(128);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.MaxUses).IsRequired();
                entity.Property(e => e.UsedCount).IsRequired();

                entity.HasOne(i => i.Group)
                    .WithMany(g => g.GroupInvites)
                    .HasForeignKey(i => i.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.CreatedByUser)
                    .WithMany(u => u.CreatedGroupInvites)
                    .HasForeignKey(i => i.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
