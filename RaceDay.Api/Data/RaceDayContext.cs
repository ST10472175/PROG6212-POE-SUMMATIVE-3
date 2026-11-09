
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{//start of namespace
    public class RaceDayContext : DbContext
    {//start of class
        // Constructor that receives the connection settings from Program.cs
        public RaceDayContext(DbContextOptions<RaceDayContext> options)
            : base(options)
        {
        }

        // Each DbSet becomes a table in the database
        public DbSet<AppUser> Users { get; set; }
        public DbSet<EventOrganiser> EventOrganisers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names match the Part 1 ERD
            modelBuilder.Entity<EventOrganiser>().ToTable("EventOrganiser");
            modelBuilder.Entity<Participant>().ToTable("Participant");
            modelBuilder.Entity<Event>().ToTable("Events");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Assignment>().ToTable("Assignment");
            modelBuilder.Entity<Result>().ToTable("Results");
            modelBuilder.Entity<AppUser>().ToTable("Users");

            // Explicitly configure primary keys
            modelBuilder.Entity<AppUser>().HasKey(u => u.UserId);
            modelBuilder.Entity<EventOrganiser>().HasKey(o => o.OrganiserId);
            modelBuilder.Entity<Participant>().HasKey(p => p.ParticipantId);
            modelBuilder.Entity<Event>().HasKey(e => e.EventId);
            modelBuilder.Entity<Category>().HasKey(c => c.CategoryId);
            modelBuilder.Entity<Assignment>().HasKey(a => a.EntryId);
            modelBuilder.Entity<Result>().HasKey(r => r.ResultsId);

            // Every email can only be registered once
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // The distance needs decimal places
            modelBuilder.Entity<Event>()
                .Property(e => e.Distance)
                .HasPrecision(8, 2);

            // Store event type as text
            modelBuilder.Entity<Event>()
                .Property(e => e.EventType)
                .HasConversion<string>()
                .HasMaxLength(10);

            // A user links to one organiser OR one participant
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Organiser)
                .WithMany()
                .HasForeignKey(u => u.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Participant)
                .WithMany()
                .HasForeignKey(u => u.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            // One organiser creates many events
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One event has many categories
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // One event has many enrolments
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Event)
                .WithMany(e => e.Assignments)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // One participant has many enrolments
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Participant)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Avoid multiple cascade paths in SQL Server
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // A participant can only enter the same event once
            modelBuilder.Entity<Assignment>()
                .HasIndex(a => new { a.EventId, a.ParticipantId })
                .IsUnique();

            // One event has many results
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Results)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // One participant has many results
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Participant)
                .WithMany(p => p.Results)
                .HasForeignKey(r => r.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);

            // A participant only gets one result per event
            modelBuilder.Entity<Result>()
                .HasIndex(r => new { r.EventId, r.ParticipantId })
                .IsUnique();
        }
    }
}
