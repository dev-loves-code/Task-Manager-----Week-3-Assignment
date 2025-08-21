using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace api.Data
{
    public class ApplicationDBContext : IdentityDbContext<AppUser>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }

        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Tasks)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.Task>()
                .HasMany(t => t.Notes)
                .WithOne(n => n.Task)
                .HasForeignKey(n => n.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for TaskId in Notes
            modelBuilder.Entity<Note>()
                .HasIndex(n => n.TaskId)
                .HasDatabaseName("IX_Notes_TaskId");


            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole { Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "b2c3d4e5-f6g7-8901-bcde-f23456789012", Name = "User", NormalizedName = "USER" }
            };

            modelBuilder.Entity<IdentityRole>().HasData(roles);



        }
    }

}