using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile featuredImageFile, string folder);
    }
}
