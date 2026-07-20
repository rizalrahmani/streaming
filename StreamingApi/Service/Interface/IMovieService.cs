using StreamingApi.Models;

namespace StreamingApi.Service.Interface;

public interface IMovieService
{
    Task<List<Movie>> GetAllAsync();
    Task<Movie?> GetByIdAsync(int id);
    Task<List<Movie>> GetEpisodesAsync(int parentId);
    Task<Movie> CreateAsync(Movie movie);
    Task UpdateAsync(Movie movie);
    Task DeleteAsync(int id);
}