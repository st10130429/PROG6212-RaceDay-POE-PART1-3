using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<RouteInformation> RouteInformation => Set<RouteInformation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Events: Users (Organiser) 1:N Events
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(u => u.EventsOrganised)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categories: Events 1:N Categories
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // RouteInformation: the ERD names the key "RouteId", which EF's naming convention
        // (Id / RouteInformationId) doesn't recognise, so it must be declared explicitly.
        modelBuilder.Entity<RouteInformation>()
            .HasKey(r => r.RouteId);

        // RouteInformation: Categories 1:1 RouteInformation
        modelBuilder.Entity<RouteInformation>()
            .HasOne(r => r.Category)
            .WithOne(c => c.RouteInformation)
            .HasForeignKey<RouteInformation>(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrolments: Users (Participant) 1:N Enrolments, Categories 1:N Enrolments
        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(en => en.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(en => en.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // A participant can only enrol once per category
        modelBuilder.Entity<Enrolment>()
            .HasIndex(en => new { en.ParticipantId, en.CategoryId })
            .IsUnique();

        // Results: Enrolments 1:1 Results
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(en => en.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Decimal precision
        modelBuilder.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(6, 2);
        modelBuilder.Entity<Category>().Property(c => c.Price).HasPrecision(8, 2);
        modelBuilder.Entity<RouteInformation>().Property(r => r.DistanceKm).HasPrecision(6, 2);
        modelBuilder.Entity<RouteInformation>().Property(r => r.ElevationGain).HasPrecision(7, 2);
    }
}
