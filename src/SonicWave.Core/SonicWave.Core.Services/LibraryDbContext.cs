using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

public class LibraryDbContext : DbContext
{
	public string DbPath { get; }

	public DbSet<Track> Tracks => Set<Track>();

	public DbSet<Playlist> Playlists => Set<Playlist>();

	public LibraryDbContext(string dbPath)
	{
		DbPath = dbPath;
	}

	protected override void OnConfiguring(DbContextOptionsBuilder options)
	{
		options.UseSqlite("Data Source=" + DbPath);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity((EntityTypeBuilder<Track> e) =>
		{
			e.HasIndex((Track t) => t.FilePath).IsUnique();
			e.HasIndex((Track t) => t.Artist);
			e.HasIndex((Track t) => t.Album);
			e.HasIndex((Track t) => t.Title);
		});
		modelBuilder.Entity((EntityTypeBuilder<Playlist> e) =>
		{
			e.Property((Playlist p) => p.TrackIds).HasColumnType("TEXT");
		});
	}
}
