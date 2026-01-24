using Azure.Storage.Blobs;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using System.Text.Json;
using static LanguageExt.Prelude;

namespace EvMarketplace.Functions.ImageProcessor;

/// <summary>
/// Azure Function for processing vehicle images
/// Uses functional programming with LanguageExt for error handling
/// </summary>
public class ImageProcessorFunction
{
    private readonly ILogger<ImageProcessorFunction> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "vehicle-images";

    public ImageProcessorFunction(
        ILogger<ImageProcessorFunction> logger,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    [Function("ProcessVehicleImage")]
    public async Task<HttpResponseData> ProcessImage(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Processing vehicle image upload");

        var result = await ProcessImagePipeline(req);

        return await result.Match(
            Right: async processedImages =>
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    images = processedImages
                });
                return response;
            },
            Left: async error =>
            {
                _logger.LogError("Image processing failed: {Error}", error);
                var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(new { error });
                return response;
            }
        );
    }

    [Function("ProcessImageQueue")]
    public async Task ProcessImageFromQueue(
        [QueueTrigger("image-processing-queue")] string queueMessage)
    {
        _logger.LogInformation("Processing image from queue: {Message}", queueMessage);

        var messageResult = ParseQueueMessage(queueMessage);

        await messageResult.Match(
            Right: async msg =>
            {
                var result = await ProcessImageFromUrl(msg.ImageUrl, msg.ListingId);
                result.Match(
                    Right: urls => _logger.LogInformation("Successfully processed image for listing {ListingId}", msg.ListingId),
                    Left: error => _logger.LogError("Failed to process image: {Error}", error)
                );
            },
            Left: error => _logger.LogError("Invalid queue message: {Error}", error)
        );
    }

    /// <summary>
    /// Functional pipeline for image processing
    /// </summary>
    private async Task<Either<string, ProcessedImages>> ProcessImagePipeline(HttpRequestData req)
    {
        return await (
            from imageData in ReadImageFromRequest(req)
            from processedSizes in ProcessImageSizes(imageData)
            from uploadResults in UploadProcessedImages(processedSizes)
            select uploadResults
        );
    }

    /// <summary>
    /// Read image data from HTTP request
    /// </summary>
    private Either<string, byte[]> ReadImageFromRequest(HttpRequestData req)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            req.Body.CopyTo(memoryStream);
            var imageData = memoryStream.ToArray();

            return imageData.Length > 0
                ? imageData
                : "No image data provided";
        }
        catch (Exception ex)
        {
            return $"Failed to read image: {ex.Message}";
        }
    }

    /// <summary>
    /// Process image into multiple sizes (thumbnail, card, full)
    /// </summary>
    private Either<string, Dictionary<ImageSize, byte[]>> ProcessImageSizes(byte[] originalImage)
    {
        try
        {
            var results = new Dictionary<ImageSize, byte[]>();

            using var image = Image.Load(originalImage);

            // Define sizes
            var sizes = new Dictionary<ImageSize, (int width, int height)>
            {
                { ImageSize.Thumbnail, (200, 150) },
                { ImageSize.Card, (400, 300) },
                { ImageSize.Full, (1200, 900) }
            };

            foreach (var (size, dimensions) in sizes)
            {
                var processedImage = ProcessImageSize(image, dimensions.width, dimensions.height);
                results[size] = processedImage;
            }

            return results;
        }
        catch (Exception ex)
        {
            return $"Image processing failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Resize and convert image to WebP format
    /// </summary>
    private byte[] ProcessImageSize(Image image, int width, int height)
    {
        var clone = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            });
        });

        using var outputStream = new MemoryStream();
        clone.Save(outputStream, new WebpEncoder
        {
            Quality = 85
        });

        return outputStream.ToArray();
    }

    /// <summary>
    /// Upload processed images to Azure Blob Storage
    /// </summary>
    private async Task<Either<string, ProcessedImages>> UploadProcessedImages(
        Dictionary<ImageSize, byte[]> processedImages)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var imageId = Guid.NewGuid().ToString();
            var urls = new Dictionary<ImageSize, string>();

            foreach (var (size, imageData) in processedImages)
            {
                var blobName = $"{imageId}-{size.ToString().ToLower()}.webp";
                var blobClient = containerClient.GetBlobClient(blobName);

                using var stream = new MemoryStream(imageData);
                await blobClient.UploadAsync(stream, overwrite: true);

                urls[size] = blobClient.Uri.ToString();
            }

            return new ProcessedImages(
                ImageId: imageId,
                ThumbnailUrl: urls[ImageSize.Thumbnail],
                CardUrl: urls[ImageSize.Card],
                FullUrl: urls[ImageSize.Full]
            );
        }
        catch (Exception ex)
        {
            return $"Upload failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Process image from URL (for queue trigger)
    /// </summary>
    private async Task<Either<string, ProcessedImages>> ProcessImageFromUrl(string imageUrl, string listingId)
    {
        try
        {
            using var httpClient = new HttpClient();
            var imageData = await httpClient.GetByteArrayAsync(imageUrl);

            return await (
                from processedSizes in ProcessImageSizes(imageData)
                from uploadResults in UploadProcessedImages(processedSizes)
                select uploadResults
            );
        }
        catch (Exception ex)
        {
            return $"Failed to process image from URL: {ex.Message}";
        }
    }

    /// <summary>
    /// Parse queue message
    /// </summary>
    private Either<string, QueueMessage> ParseQueueMessage(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<QueueMessage>(json);
            return message ?? Left<string, QueueMessage>("Invalid message format");
        }
        catch (Exception ex)
        {
            return $"Failed to parse message: {ex.Message}";
        }
    }
}

// Types
public enum ImageSize
{
    Thumbnail,
    Card,
    Full
}

public record ProcessedImages(
    string ImageId,
    string ThumbnailUrl,
    string CardUrl,
    string FullUrl
);

public record QueueMessage(
    string ImageUrl,
    string ListingId
);
