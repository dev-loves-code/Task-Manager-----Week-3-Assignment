using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace api.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }

        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<Models.Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Models.Task>()
                .HasMany(t => t.Notes)
                .WithOne(n => n.Task)
                .HasForeignKey(n => n.TaskId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // Index for TaskId in Notes
            modelBuilder.Entity<Note>()
                .HasIndex(n => n.TaskId)
                .HasDatabaseName("IX_Notes_TaskId");


            // Additional model configurations can go here
        }
    }

}