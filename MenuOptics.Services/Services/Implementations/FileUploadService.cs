
namespace MenuOptics.Services
{

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> UploadFileAsync(IFormFile featuredImageFile, string folder = "uploads")
        {
            if (featuredImageFile == null || featuredImageFile.Length == 0)
            {
                throw new ArgumentException("Invalid file.");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, folder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(featuredImageFile.FileName)}";

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await featuredImageFile.CopyToAsync(fileStream);
            }

            return $"/{folder}/{uniqueFileName}";
        }
    }

}
