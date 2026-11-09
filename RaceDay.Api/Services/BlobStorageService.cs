using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services
{
    // Azure Blob Storage file handling is kept in one service so controllers do not
    // contain storage-account or upload implementation details.
    // Reference: https://learn.microsoft.com/azure/storage/blobs/storage-quickstart-blobs-dotnet
    public sealed class BlobStorageService
    {
        private readonly IConfiguration _configuration;
        private const long MaximumFileSize = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        public BlobStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Choose an image file to upload.");
            }

            if (file.Length > MaximumFileSize)
            {
                throw new ArgumentException("Images must be 5 MB or smaller.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                throw new ArgumentException("Only JPG, PNG and WEBP images are accepted.");
            }

            string connectionString = _configuration["AzureBlobStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlobStorage__ConnectionString.");
            }

            try
            {
                BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
                await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

                string extension = file.ContentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".img"
                };

                // A generated blob name avoids trusting a user-supplied file name.
                string blobName = $"{Guid.NewGuid():N}{extension}";
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                await using Stream stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);
                return blobClient.Uri.ToString();
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Azure Blob Storage could not accept the image. Check the connection string and container access settings.", ex);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("The Azure Blob Storage connection string is invalid. Set a valid AzureBlobStorage__ConnectionString.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("The Azure Blob Storage connection string is invalid. Set a valid AzureBlobStorage__ConnectionString.", ex);
            }
        }
    }
}
