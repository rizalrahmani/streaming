using System.ComponentModel.DataAnnotations;

namespace StreamingApi.Models.Dtos;

public class GoogleDriveFileDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? Size { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime? ModifiedTime { get; set; }
    public List<string> Parents { get; set; } = new();
}

public class GoogleDriveFolderDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<GoogleDriveFileDto> Files { get; set; } = new();
}

public class GoogleDriveStructureDto
{
    public List<GoogleDriveFolderDto> Folders { get; set; } = new();
    public List<GoogleDriveFileDto> LooseFiles { get; set; } = new();
}

public class ImportFolderRequest
{
    [Required(ErrorMessage = "FolderId wajib diisi")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "FolderId harus antara 1-100 karakter")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "FolderId hanya boleh berisi huruf, angka, garis bawah, dan strip")]
    public string FolderId { get; set; } = string.Empty;

    [StringLength(200, MinimumLength = 1,
        ErrorMessage = "FolderName harus antara 1-200 karakter")]
    public string? FolderName { get; set; }

    [StringLength(50, ErrorMessage = "Genre maksimal 50 karakter")]
    public string? Genre { get; set; }

    [Range(1900, 2100, ErrorMessage = "ReleaseYear harus antara 1900-2100")]
    public int? ReleaseYear { get; set; }
}

public class ImportFolderResultDto
{
    public Movie Series { get; set; } = null!;
    public List<Movie> Episodes { get; set; } = new();
    public int SkippedCount { get; set; }
}

public class ImportGoogleDriveRequest
{
    [Required(ErrorMessage = "FileId wajib diisi")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "FileId harus antara 1-100 karakter")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "FileId hanya boleh berisi huruf, angka, garis bawah, dan strip")]
    public string FileId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title wajib diisi")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title harus antara 1-200 karakter")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description maksimal 2000 karakter")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Genre maksimal 50 karakter")]
    public string? Genre { get; set; }

    [Range(1900, 2100, ErrorMessage = "ReleaseYear harus antara 1900-2100")]
    public int ReleaseYear { get; set; }

    public bool IsSeries { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ParentId harus bernilai positif")]
    public int? ParentId { get; set; }

    [Range(1, 50, ErrorMessage = "SeasonNumber harus antara 1-50")]
    public int? SeasonNumber { get; set; }

    [Range(1, 9999, ErrorMessage = "EpisodeNumber harus antara 1-9999")]
    public int? EpisodeNumber { get; set; }
}