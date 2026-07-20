using System.ComponentModel.DataAnnotations;

namespace StreamingApi.Models.Dtos;

public class CreateMovieRequest
{
    [Required(ErrorMessage = "Title wajib diisi")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title harus antara 1-200 karakter")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description maksimal 2000 karakter")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Genre maksimal 50 karakter")]
    public string? Genre { get; set; }

    [Range(1900, 2100, ErrorMessage = "ReleaseYear harus antara 1900-2100")]
    public int ReleaseYear { get; set; }

    [StringLength(500, ErrorMessage = "CoverImagePath maksimal 500 karakter")]
    public string? CoverImagePath { get; set; }

    [StringLength(100, ErrorMessage = "GoogleDriveFileId maksimal 100 karakter")]
    [RegularExpression(@"^[a-zA-Z0-9_-]*$",
        ErrorMessage = "GoogleDriveFileId hanya boleh berisi huruf, angka, garis bawah, dan strip")]
    public string? GoogleDriveFileId { get; set; }

    public bool IsSeries { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ParentId harus bernilai positif")]
    public int? ParentId { get; set; }

    [Range(1, 50, ErrorMessage = "SeasonNumber harus antara 1-50")]
    public int? SeasonNumber { get; set; }

    [Range(1, 9999, ErrorMessage = "EpisodeNumber harus antara 1-9999")]
    public int? EpisodeNumber { get; set; }
}

public class UpdateMovieRequest : CreateMovieRequest
{
    [Required(ErrorMessage = "Id wajib diisi")]
    [Range(1, int.MaxValue, ErrorMessage = "Id harus bernilai positif")]
    public int Id { get; set; }
}
