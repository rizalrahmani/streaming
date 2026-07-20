using Microsoft.EntityFrameworkCore;
using StreamingApi.Data;
using StreamingApi.Exceptions;
using StreamingApi.Models;
using StreamingApi.Service.Interface;

namespace StreamingApi.Services;

public class MovieService : IMovieService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MovieService> _logger;

    public MovieService(AppDbContext context, ILogger<MovieService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Movie>> GetAllAsync()
    {
        return await _context.Movies
            .Where(m => m.ParentId == null)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Movie?> GetByIdAsync(int id)
    {
        return await _context.Movies
            .Include(m => m.Episodes)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Movie>> GetEpisodesAsync(int parentId)
    {
        // Validate parent existence up-front for clearer error than empty list
        var parentExists = await _context.Movies.AnyAsync(m => m.Id == parentId);
        if (!parentExists)
            throw new NotFoundException("Movie", parentId);

        return await _context.Movies
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SeasonNumber)
            .ThenBy(m => m.EpisodeNumber)
            .ToListAsync();
    }

    public async Task<Movie> CreateAsync(Movie movie)
    {
        ValidateBusinessRules(movie);

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created movie {MovieId} '{Title}'", movie.Id, movie.Title);
        return movie;
    }

    public async Task UpdateAsync(Movie movie)
    {
        ValidateBusinessRules(movie);

        var existing = await _context.Movies
            .Include(m => m.Episodes)
            .FirstOrDefaultAsync(m => m.Id == movie.Id);

        if (existing == null)
            throw new NotFoundException("Movie", movie.Id);

        // Episode integrity: parent harus IsSeries
        if (movie.IsSeries)
        {
            // Jika sebelumnya bukan series, hapus episode lama untuk menghindari orphan
            if (!existing.IsSeries && existing.Episodes.Any())
            {
                _context.Movies.RemoveRange(existing.Episodes);
                _logger.LogInformation("Removed {Count} orphaned episodes when promoting {MovieId} to series",
                    existing.Episodes.Count, movie.Id);
            }
        }
        else
        {
            if (existing.IsSeries && existing.Episodes.Any())
            {
                _context.Movies.RemoveRange(existing.Episodes);
                _logger.LogInformation("Removed {Count} orphaned episodes when demoting {MovieId} from series",
                    existing.Episodes.Count, movie.Id);
            }
        }

        var entry = _context.ChangeTracker.Entries<Movie>()
            .FirstOrDefault(e => e.Entity.Id == movie.Id);
        if (entry != null)
            entry.State = EntityState.Detached;

        _context.Movies.Update(movie);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated movie {MovieId}", movie.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.Episodes)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            throw new NotFoundException("Movie", id);

        if (movie.Episodes.Any())
        {
            _context.Movies.RemoveRange(movie.Episodes);
            _logger.LogInformation("Removing {Count} episodes of movie {MovieId}", movie.Episodes.Count, id);
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted movie {MovieId}", id);
    }

    private static void ValidateBusinessRules(Movie movie)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(movie.Title))
            errors["Title"] = new[] { "Title tidak boleh kosong" };

        if (movie.ReleaseYear < 1900 || movie.ReleaseYear > 2100)
            errors["ReleaseYear"] = new[] { "ReleaseYear harus antara 1900-2100" };

        // Episode harus punya parent yang IsSeries
        if (movie.ParentId.HasValue && movie.SeasonNumber == null)
            errors["SeasonNumber"] = new[] { "Episode harus memiliki SeasonNumber" };

        if (movie.ParentId.HasValue && movie.EpisodeNumber == null)
            errors["EpisodeNumber"] = new[] { "Episode harus memiliki EpisodeNumber" };

        if (movie.ParentId.HasValue && movie.IsSeries)
            errors["IsSeries"] = new[] { "Episode tidak boleh IsSeries=true" };

        if (!movie.IsSeries && string.IsNullOrWhiteSpace(movie.GoogleDriveFileId))
            errors["GoogleDriveFileId"] = new[] { "Movie harus memiliki GoogleDriveFileId (import via Google Drive flow)" };

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}