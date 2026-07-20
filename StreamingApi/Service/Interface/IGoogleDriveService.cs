using StreamingApi.Models.Dtos;

namespace StreamingApi.Service.Interface;

public interface IGoogleDriveService
{
    Task<List<GoogleDriveFileDto>> GetFilesAsync(string? mimeTypeFilter = null);
    Task<GoogleDriveFileDto?> GetFileAsync(string fileId);
    Task<string?> GetAccessTokenAsync();
    Task<GoogleDriveStreamResult> StreamFileAsync(string fileId, string? rangeHeader = null);
    Task<long> GetFileSizeAsync(string fileId);
    Task<GoogleDriveStructureDto> GetStructureAsync();
    Task<ImportFolderResultDto> ImportFolderAsync(ImportFolderRequest request);
}