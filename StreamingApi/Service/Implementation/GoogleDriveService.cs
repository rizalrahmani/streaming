using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using StreamingApi.Data;
using StreamingApi.Exceptions;
using StreamingApi.Models;
using StreamingApi.Models.Dtos;
using StreamingApi.Service.Interface;

namespace StreamingApi.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<GoogleDriveService> _logger;
    private readonly string? _credentialsPath;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private ServiceAccountCredential? _cachedCredential;

    public GoogleDriveService(
        IConfiguration configuration,
        HttpClient httpClient,
        AppDbContext context,
        ILogger<GoogleDriveService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _credentialsPath = _configuration["GoogleDrive:CredentialsPath"];
    }

    private async Task<string> GetCredentialsPathAsync()
    {
        if (_credentialsPath == null)
            throw new InvalidOperationException("GoogleDrive:CredentialsPath not configured in appsettings.json");
        if (File.Exists(_credentialsPath)) return _credentialsPath;

        var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (envPath != null && File.Exists(envPath)) return envPath;

        throw new FileNotFoundException("Google Drive credentials file not found");
    }

    private async Task<ServiceAccountCredential> GetCredentialAsync()
    {
        if (_cachedCredential != null) return _cachedCredential;

        var credPath = await GetCredentialsPathAsync();
        var json = await File.ReadAllTextAsync(credPath);
        var keyData = JsonSerializer.Deserialize<JsonElement>(json);

        var clientEmail = keyData.GetProperty("client_email").GetString()
            ?? throw new InvalidOperationException("client_email not found");
        var privateKey = keyData.GetProperty("private_key").GetString()
            ?? throw new InvalidOperationException("private_key not found");

        var initializer = new ServiceAccountCredential.Initializer(clientEmail)
        {
            Scopes = new[] { DriveService.ScopeConstants.DriveReadonly }
        };

        _cachedCredential = new ServiceAccountCredential(initializer.FromPrivateKey(privateKey));
        return _cachedCredential;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry) return _cachedToken;

        var credential = await GetCredentialAsync();
        var token = await credential.GetAccessTokenForRequestAsync();
        _cachedToken = token;
        _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
        return token;
    }

    private DriveService CreateDriveService(ServiceAccountCredential credential)
    {
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _configuration["GoogleDrive:ApplicationName"] ?? "StreamingApp"
        });
    }

    public async Task<List<GoogleDriveFileDto>> GetFilesAsync(string? mimeTypeFilter = null)
    {
        var credential = await GetCredentialAsync();
        var service = CreateDriveService(credential);

        var request = service.Files.List();
        request.Q = "trashed = false";
        request.Fields = "files(id, name, size, mimeType, modifiedTime, parents)";
        request.OrderBy = "modifiedTime desc";
        request.PageSize = 1000;

        var allFiles = new List<Google.Apis.Drive.v3.Data.File>();
        string? pageToken = null;
        do
        {
            request.PageToken = pageToken;
            var result = await ExecuteDriveAsync(() => request.ExecuteAsync(), nameof(GetFilesAsync));
            if (result.Files != null)
                allFiles.AddRange(result.Files);
            pageToken = result.NextPageToken;
        }
        while (pageToken != null);

        if (!string.IsNullOrEmpty(mimeTypeFilter))
            allFiles = allFiles.Where(f => f.MimeType == mimeTypeFilter).ToList();

        return allFiles.Select(MapToDto).ToList();
    }

    public async Task<GoogleDriveFileDto?> GetFileAsync(string fileId)
    {
        var credential = await GetCredentialAsync();
        var service = CreateDriveService(credential);

        var request = service.Files.Get(fileId);
        request.Fields = "id, name, size, mimeType, modifiedTime, parents";

        var file = await ExecuteDriveAsync(() => request.ExecuteAsync(), nameof(GetFileAsync));
        return file == null ? null : MapToDto(file);
    }

    private async Task<T> ExecuteDriveAsync<T>(Func<Task<T>> action, string operation)
    {
        try
        {
            return await action();
        }
        catch (GoogleApiException gex)
        {
            _logger.LogError(gex, "Google Drive API error during {Operation}: {Reason}", operation, gex.Error?.Message);
            throw new ExternalServiceException("GoogleDrive", $"API call '{operation}' gagal: {gex.Error?.Message ?? gex.Message}", gex);
        }
        catch (HttpRequestException hex)
        {
            _logger.LogError(hex, "Network error during {Operation}", operation);
            throw new ExternalServiceException("GoogleDrive", $"Tidak dapat menghubungi Google Drive saat '{operation}'", hex);
        }
    }

    private static GoogleDriveFileDto MapToDto(Google.Apis.Drive.v3.Data.File f)
    {
        return new GoogleDriveFileDto
        {
            Id = f.Id,
            Name = f.Name,
            Size = f.Size,
            MimeType = f.MimeType,
            ModifiedTime = f.ModifiedTime,
            Parents = f.Parents?.ToList() ?? new List<string>()
        };
    }

    public async Task<GoogleDriveStructureDto> GetStructureAsync()
    {
        var allFiles = await GetFilesAsync();

        var videoFiles = allFiles.Where(f =>
            !f.MimeType.Contains("folder") &&
            !f.MimeType.StartsWith("vnd.google-apps")).ToList();

        var folderFiles = videoFiles
            .Where(f => f.Parents.Any(p => p != "root"))
            .ToList();

        var looseFiles = videoFiles
            .Where(f => f.Parents.Contains("root"))
            .ToList();

        // Jika GetFilesAsync TIDAK mengembalikan folder, ambil folder saja dalam 1 call terpisah
        // (tetap O(1) round-trip, bukan O(N))
        var folderLookUp = (await GetFolderLookUpAsync())
            .ToDictionary(f => f.Id, f => f.Name);

        var folders = folderFiles
            .SelectMany(f => f.Parents)
            .Where(pid => pid != "root")
            .Distinct()
            .Select(folderId => new GoogleDriveFolderDto
            {
                Id = folderId,
                Name = folderLookUp.GetValueOrDefault(folderId, "(unknown)"),
                Files = folderFiles
                    .Where(f => f.Parents.Contains(folderId))
                    .OrderBy(f => ExtractEpisodeNumber(f.Name))
                    .ToList()
            })
            .OrderBy(f => f.Name)
            .ToList();

        return new GoogleDriveStructureDto
        {
            Folders = folders,
            LooseFiles = looseFiles.OrderBy(f => f.Name).ToList()
        };
    }

    private async Task<List<GoogleDriveFileDto>> GetFolderLookUpAsync()
    {
        var credential = await GetCredentialAsync();
        var service = CreateDriveService(credential);

        var request = service.Files.List();
        request.Q = "trashed = false and mimeType ='application/vnd.google-apps.folder'";
        request.Fields = "files(id, name)";
        request.PageSize = 1000;

        var folders = new List<Google.Apis.Drive.v3.Data.File>();
        string? pageToken = null;
        do
        {
            request.PageToken = pageToken;
            var result = await ExecuteDriveAsync(() => request.ExecuteAsync(), nameof(GetFolderLookUpAsync));
            if (result.Files != null)
                folders.AddRange(result.Files);
            pageToken = result.NextPageToken;
        }
        while (pageToken != null);

        return folders.Select(MapToDto).ToList();
    }

    private static readonly Regex EpisodeRegex = new(
        @"(?:S\d+E|E)(\d+)|[Ee]pisode\s*(\d+)|[\s\-_](\d{1,3})(?:\.|$)",
        RegexOptions.Compiled);

    private static int ExtractEpisodeNumber(string fileName)
    {
        var match = EpisodeRegex.Match(fileName);
        if (!match.Success) return 999;

        for (int i = 1; i < match.Groups.Count; i++)
        {
            if (match.Groups[i].Success && int.TryParse(match.Groups[i].Value, out var n))
                return n;
        }
        return 999;
    }

    private static string CleanFileName(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        return Regex.Replace(nameWithoutExt, @"\[[^\]]*\]", "").Trim();
    }

    public async Task<ImportFolderResultDto> ImportFolderAsync(ImportFolderRequest request)
    {
        var credential = await GetCredentialAsync();
        var service = CreateDriveService(credential);

        var folderMeta = await ExecuteDriveAsync(
            () => service.Files.Get(request.FolderId).ExecuteAsync(),
            nameof(ImportFolderAsync));

        if (folderMeta == null)
            throw new NotFoundException("Folder", request.FolderId);

        var folderName = request.FolderName ?? folderMeta.Name;
        var folderDescription = folderMeta.Description;

        var filesRequest = service.Files.List();
        filesRequest.Q = $"'{request.FolderId}' in parents and trashed = false and mimeType contains 'video'";
        filesRequest.Fields = "files(id, name, size, mimeType, modifiedTime, parents)";
        filesRequest.OrderBy = "name";

        var files = (await ExecuteDriveAsync(() => filesRequest.ExecuteAsync(), nameof(ImportFolderAsync)))
            .Files ?? new List<Google.Apis.Drive.v3.Data.File>();

        _logger.LogInformation("Importing folder {FolderId} '{FolderName}' with {FileCount} video(s)",
            request.FolderId, folderName, files.Count);

        var existingEpisodes = _context.Movies
            .Where(m => m.GoogleDriveFileId != null
                     && files.Select(f => f.Id).Contains(m.GoogleDriveFileId))
            .Select(m => m.GoogleDriveFileId)
            .ToList();

        var series = new Movie
        {
            Title = folderName,
            Description = folderDescription,
            Genre = request.Genre,
            ReleaseYear = request.ReleaseYear ?? DateTime.UtcNow.Year,
            IsSeries = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Movies.Add(series);
        await _context.SaveChangesAsync();

        var episodes = new List<Movie>();
        int episodeNum = 1;

        foreach (var file in files.OrderBy(f => ExtractEpisodeNumber(f.Name)))
        {
            if (existingEpisodes.Contains(file.Id)) continue;

            var ep = new Movie
            {
                Title = $"Episode {episodeNum} - {CleanFileName(file.Name)}",
                Genre = request.Genre,
                ReleaseYear = request.ReleaseYear ?? DateTime.UtcNow.Year,
                GoogleDriveFileId = file.Id,
                IsSeries = false,
                ParentId = series.Id,
                SeasonNumber = 1,
                EpisodeNumber = episodeNum,
                CreatedAt = DateTime.UtcNow
            };
            _context.Movies.Add(ep);
            episodes.Add(ep);
            episodeNum++;
        }

        await _context.SaveChangesAsync();

        return new ImportFolderResultDto
        {
            Series = series,
            Episodes = episodes,
            SkippedCount = files.Count - episodes.Count
        };
    }

    public async Task<GoogleDriveStreamResult> StreamFileAsync(string fileId, string? rangeHeader = null)
    {
        var token = await GetAccessTokenAsync();
        if (token == null)
            throw new ExternalServiceException("GoogleDrive", "Gagal mendapatkan access token");

        var fileMeta = await GetFileAsync(fileId);
        if (fileMeta == null)
            throw new NotFoundException("File", fileId);

        var contentType = GetContentType(fileMeta.Name);

        var httpRequest = new HttpRequestMessage(HttpMethod.Get,
            $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (rangeHeader != null)
            httpRequest.Headers.TryAddWithoutValidation("Range", rangeHeader);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (HttpRequestException hex)
        {
            _logger.LogError(hex, "Network error streaming file {FileId}", fileId);
            throw new ExternalServiceException("GoogleDrive", $"Tidak dapat mengunduh file '{fileId}'", hex);
        }

        var responseMediaType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google Drive returned {Status} for file {FileId}: {Body}",
                    (int)response.StatusCode, fileId, errorBody);
                throw new ExternalServiceException("GoogleDrive",
                    $"Google Drive mengembalikan status {(int)response.StatusCode} untuk file '{fileId}'");
            }
        }

        if (responseMediaType.Contains("html"))
        {
            using (response)
            {
                throw new ExternalServiceException("GoogleDrive",
                    "Google Drive meminta konfirmasi unduhan (virus scan warning)");
            }
        }

        var stream = await response.Content.ReadAsStreamAsync();

        string? contentRange = null;
        if (response.Content.Headers.TryGetValues("Content-Range", out var crv))
            contentRange = crv.FirstOrDefault();

        return new GoogleDriveStreamResult(response)
        {
            Stream = stream,
            StatusCode = (int)response.StatusCode,
            ContentType = contentType,
            ContentRange = contentRange
        };
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".m4v" => "video/mp4",
            ".webm" => "video/webm",
            ".mkv" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".ts" => "video/mp2t",
            _ => "video/mp4"
        };
    }

    public async Task<long> GetFileSizeAsync(string fileId)
    {
        var file = await GetFileAsync(fileId);
        return file?.Size ?? 0;
    }
}