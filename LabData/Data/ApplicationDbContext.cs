using LabData.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LabData.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<PollCategory> PollCategories => Set<PollCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Poll>()
            .HasOne(poll => poll.Survey)
            .WithMany(survey => survey.Polls)
            .HasForeignKey(poll => poll.SurveyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PollOption>()
            .HasOne(option => option.Poll)
            .WithMany(poll => poll.Options)
            .HasForeignKey(option => option.PollId);

        modelBuilder.Entity<Vote>()
            .HasOne(vote => vote.Poll)
            .WithMany(poll => poll.Votes)
            .HasForeignKey(vote => vote.PollId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        modelBuilder.Entity<Vote>()
            .HasOne(vote => vote.PollOption)
            .WithMany(option => option.Votes)
            .HasForeignKey(vote => new { vote.PollId, vote.PollOptionId })
            .HasPrincipalKey(option => new { option.PollId, option.Id });

        modelBuilder.Entity<Vote>()
            .HasOne(vote => vote.Voter)
            .WithMany(user => user.Votes)
            .HasForeignKey(vote => vote.VoterId);

        modelBuilder.Entity<Vote>()
            .HasIndex(vote => new { vote.PollId, vote.VoterId })
            .IsUnique();

        modelBuilder.Entity<Poll>()
            .HasMany(poll => poll.Categories)
            .WithMany(category => category.Polls)
            .UsingEntity<Dictionary<string, object>>(
                "PollCategoryAssignments",
                category => category.HasOne<PollCategory>()
                    .WithMany()
                    .HasForeignKey("PollCategoryId"),
                poll => poll.HasOne<Poll>()
                    .WithMany()
                    .HasForeignKey("PollId"),
                join => join.HasKey("PollId", "PollCategoryId"));
    }
}