using System;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HealthCart.Interfaces;

namespace HealthCart.Services;

public class CloudinaryService : ICloudinaryService
{


   private readonly Cloudinary cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudinaryUrl = configuration["Cloudinary:Url"];

        if (string.IsNullOrEmpty(cloudinaryUrl))
        {
            throw new InvalidOperationException("Cloudinary URL is not configured.");
        }

        cloudinary = new Cloudinary(cloudinaryUrl) { Api = { Secure = true } };
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is invalid.");
        }

        using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true,
            Folder = "Australasia Apparels"
            // Transformation = new Transformation().Width(150).Height(150).Crop("fill")
        };

        var uploadResult = await cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }
}