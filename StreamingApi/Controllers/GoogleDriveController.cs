using Microsoft.AspNetCore.Mvc;
using StreamingApi.Exceptions;
using StreamingApi.Models;
using StreamingApi.Models.Dtos;
using StreamingApi.Service.Interface;

namespace StreamingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoogleDriveController : ControllerBase
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IMovieService _movieService;

    public GoogleDriveController(IGoogleDriveService googleDriveService, IMovieService movieService)
    {
        _googleDriveService = googleDriveService;
        _movieService = movieService;
    }

    [HttpGet("files")]
    public async Task<ActionResult<List<GoogleDriveFileDto>>> GetFiles([FromQuery] string? mimeType = null)
    {
        var files = await _googleDriveService.GetFilesAsync(mimeType);
        return Ok(files);
    }

    [HttpGet("files/{fileId}")]
    public async Task<ActionResult<GoogleDriveFileDto>> GetFile(string fileId)
    {
        var file = await _googleDriveService.GetFileAsync(fileId);
        if (file == null)
            throw new NotFoundException("Google Drive file", fileId);
        return Ok(file);
    }

    [HttpGet("structure")]
    public async Task<ActionResult<GoogleDriveStructureDto>> GetStructure()
    {
        return Ok(await _googleDriveService.GetStructureAsync());
    }

    [HttpPost("import")]
    public async Task<ActionResult<Movie>> Import([FromBody] ImportGoogleDriveRequest request)
    {
        var driveFile = await _googleDriveService.GetFileAsync(request.FileId);
        if (driveFile == null)
            throw new NotFoundException("File", request.FileId);

        var movie = new Movie
        {
            Title = request.Title,
            Description = request.Description,
            Genre = request.Genre,
            ReleaseYear = request.ReleaseYear,
            GoogleDriveFileId = request.FileId,
            IsSeries = request.IsSeries,
            ParentId = request.ParentId,
            SeasonNumber = request.SeasonNumber,
            EpisodeNumber = request.EpisodeNumber,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _movieService.CreateAsync(movie);
        return CreatedAtAction(nameof(GetFile), new { fileId = request.FileId }, created);
    }

    [HttpPost("import-folder")]
    public async Task<ActionResult<ImportFolderResultDto>> ImportFolder([FromBody] ImportFolderRequest request)
    {
        // ModelState divalidasi otomatis oleh [ApiController] -> 400 via ProblemDetails
        var result = await _googleDriveService.ImportFolderAsync(request);
        return Ok(result);
    }
}