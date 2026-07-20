using System.ComponentModel.DataAnnotations.Schema;

namespace StreamingApi.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public int ReleaseYear { get; set; }
    public string? CoverImagePath { get; set; }
    public string? GoogleDriveFileId { get; set; }
    public bool IsSeries { get; set; }
    public int? ParentId { get; set; }
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParentId))]
    public Movie? Parent { get; set; }
    public List<Movie> Episodes { get; set; } = new();
}
