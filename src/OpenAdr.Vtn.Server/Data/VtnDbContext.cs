using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.Server.Data;

public class VtnDbContext(DbContextOptions<VtnDbContext> options) : DbContext(options)
{
    public DbSet<ProgramDto> Programs => Set<ProgramDto>();
    public DbSet<EventDto> Events => Set<EventDto>();
    public DbSet<VenDto> Vens => Set<VenDto>();
    public DbSet<ReportDto> Reports => Set<ReportDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProgramDto>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.ProgramName).IsUnique();
            b.Property(p => p.IntervalPeriod).HasJsonConversion();
            b.Property(p => p.ProgramDescriptions).HasJsonConversion();
            b.Property(p => p.PayloadDescriptors).HasJsonConversion();
            b.Property(p => p.Attributes).HasJsonConversion();
            b.Property(p => p.Targets).HasJsonConversion();
        });

        modelBuilder.Entity<EventDto>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.ProgramId);
            b.Property(e => e.Targets).HasJsonConversion();
            b.Property(e => e.ReportDescriptors).HasJsonConversion();
            b.Property(e => e.PayloadDescriptors).HasJsonConversion();
            b.Property(e => e.IntervalPeriod).HasJsonConversion();
            b.Property(e => e.Intervals).HasJsonConversion();
        });

        modelBuilder.Entity<VenDto>(b =>
        {
            b.HasKey(v => v.Id);
            b.HasIndex(v => v.VenName).IsUnique();
            b.Property(v => v.Targets).HasJsonConversion();
            b.Property(v => v.Attributes).HasJsonConversion();
        });

        modelBuilder.Entity<ReportDto>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => new { r.EventId, r.ClientName, r.CreatedDateTime });
            b.Property(r => r.PayloadDescriptors).HasJsonConversion();
            b.Property(r => r.Resources).HasJsonConversion();
        });
    }
}
