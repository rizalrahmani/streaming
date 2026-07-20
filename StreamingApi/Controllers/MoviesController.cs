using Microsoft.AspNetCore.Mvc;
using StreamingApi.Exceptions;
using StreamingApi.Models;
using StreamingApi.Models.Dtos;
using StreamingApi.Service.Interface;

namespace StreamingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IGoogleDriveService _googleDriveService;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(
        IMovieService movieService,
        IGoogleDriveService googleDriveService,
        ILogger<MoviesController> logger)
    {
        _movieService = movieService;
        _googleDriveService = googleDriveService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Movie>>> GetAll()
    {
        var movies = await _movieService.GetAllAsync();
        return Ok(movies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Movie>> GetById(int id)
    {
        var movie = await _movieService.GetByIdAsync(id);
        if (movie == null)
            throw new NotFoundException("Movie", id);
        return Ok(movie);
    }

    [HttpGet("{id}/episodes")]
    public async Task<ActionResult<List<Movie>>> GetEpisodes(int id)
    {
        // MovieService.GetEpisodesAsync throws NotFoundException jika parent missing
        var episodes = await _movieService.GetEpisodesAsync(id);
        return Ok(episodes);
    }

    [HttpPost]
    public async Task<ActionResult<Movie>> Create([FromBody] CreateMovieRequest request)
    {
        // [ApiController] auto-validates DataAnnotations -> 400 via ProblemDetails
        var movie = new Movie
        {
            Title = request.Title,
            Description = request.Description,
            Genre = request.Genre,
            ReleaseYear = request.ReleaseYear,
            CoverImagePath = request.CoverImagePath,
            GoogleDriveFileId = request.GoogleDriveFileId,
            IsSeries = request.IsSeries,
            ParentId = request.ParentId,
            SeasonNumber = request.SeasonNumber,
            EpisodeNumber = request.EpisodeNumber,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _movieService.CreateAsync(movie);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMovieRequest request)
    {
        if (id != request.Id)
            throw new ValidationException("Id di URL dan body tidak cocok");

        var movie = new Movie
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            Genre = request.Genre,
            ReleaseYear = request.ReleaseYear,
            CoverImagePath = request.CoverImagePath,
            GoogleDriveFileId = request.GoogleDriveFileId,
            IsSeries = request.IsSeries,
            ParentId = request.ParentId,
            SeasonNumber = request.SeasonNumber,
            EpisodeNumber = request.EpisodeNumber
        };

        await _movieService.UpdateAsync(movie);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // MovieService.DeleteAsync throws NotFoundException jika movie missing
        await _movieService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/stream")]
    public async Task<IActionResult> Stream(int id)
    {
        var movie = await _movieService.GetByIdAsync(id);
        if (movie == null)
            throw new NotFoundException("Movie", id);

        if (string.IsNullOrEmpty(movie.GoogleDriveFileId))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["GoogleDriveFileId"] = new[] { "Movie ini belum terhubung ke Google Drive" }
            });

        return await StreamFromGoogleDrive(movie.GoogleDriveFileId);
    }

    private async Task<IActionResult> StreamFromGoogleDrive(string fileId)
    {
        // Biarkan exception naik ke GlobalExceptionMiddleware agar user lihat 502 yang jelas
        var rangeHeader = Request.Headers["Range"].FirstOrDefault();
        using var result = await _googleDriveService.StreamFileAsync(fileId, rangeHeader);

        Response.StatusCode = result.StatusCode;
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.ContentType = result.ContentType;

        if (result.ContentRange != null)
            Response.Headers["Content-Range"] = result.ContentRange;

        await result.Stream.CopyToAsync(Response.Body);
        return new EmptyResult();
    }
}
