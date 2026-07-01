using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.Storage;
using Windows.Storage.Streams;

namespace StoryCADLib.Services;

/// <summary>
///     Imports local image files and renders stored images for display.
///     Pictures are embedded in the .stbx outline as Base64 (see
///     <see cref="StoryImage"/>). To keep outlines from ballooning into
///     gigabytes, imports are capped at a viewing resolution: an image whose
///     long edge exceeds <see cref="MaxImportDimension"/> is downscaled and
///     re-encoded as WebP; smaller images (and anything that can't be decoded)
///     are stored unchanged. Gallery tiles request an even lighter decoded
///     thumbnail via <see cref="ToImageSourceAsync"/>.
/// </summary>
public class ImageService
{
    /// <summary>
    ///     Maximum length (pixels) of the longer edge for an imported image.
    ///     Larger images are downscaled to this before storage so the embedded
    ///     payload stays small; 2048px is visually near-indistinguishable from
    ///     the original on screen while leaving headroom for a future zoom view.
    /// </summary>
    private const int MaxImportDimension = 2048;

    /// <summary>Quality (0–100) used when re-encoding a downscaled import as WebP.</summary>
    private const int ImportImageQuality = 85;

    private readonly Windowing _windowing;
    private readonly ILogService _logger;

    public ImageService(Windowing windowing, ILogService logger)
    {
        _windowing = windowing;
        _logger = logger;
    }

    /// <summary>
    ///     Prompts the user to pick an image file and returns it as a
    ///     <see cref="StoryImage"/> holding the full-resolution original bytes.
    ///     Returns null if the user cancels.
    /// </summary>
    public async Task<StoryImage> PickImageAsync()
    {
        StorageFile file = await _windowing.ShowImagePicker();
        if (file == null)
        {
            return null;
        }

        IBuffer buffer = await FileIO.ReadBufferAsync(file);
        byte[] bytes = new byte[buffer.Length];
        using (DataReader reader = DataReader.FromBuffer(buffer))
        {
            reader.ReadBytes(bytes);
        }

        _logger.Log(LogLevel.Info, $"Imported image {file.Name} ({bytes.Length} bytes)");
        return CreateFromBytes(bytes, file.Name);
    }

    /// <summary>
    ///     Like <see cref="PickImageAsync"/>, but also decodes a gallery thumbnail
    ///     for immediate display, reusing a single SkiaSharp decode of the picked
    ///     bytes for both the storage-sizing check and the thumbnail rather than
    ///     decoding twice. Returns (null, null) if the user cancels; the thumbnail
    ///     is null (but the image is still returned) if it can't be decoded or
    ///     built. Must be called on the UI thread (thumbnail building requires it;
    ///     see <see cref="ToImageSourceAsync"/>).
    /// </summary>
    public async Task<(StoryImage Image, ImageSource Thumbnail)> PickImageWithThumbnailAsync(int thumbnailDecodeWidth)
    {
        StorageFile file = await _windowing.ShowImagePicker();
        if (file == null)
        {
            return (null, null);
        }

        IBuffer buffer = await FileIO.ReadBufferAsync(file);
        byte[] bytes = new byte[buffer.Length];
        using (DataReader reader = DataReader.FromBuffer(buffer))
        {
            reader.ReadBytes(bytes);
        }

        _logger.Log(LogLevel.Info, $"Imported image {file.Name} ({bytes.Length} bytes)");

        SKBitmap decoded = null;
        try
        {
            decoded = SKBitmap.Decode(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Warn, ex,
                $"Failed to decode picked image ({bytes.Length} bytes); storing without a thumbnail");
        }

        using (decoded)
        {
            // Always returns the StoryImage even if decoding/thumbnailing below fails,
            // so a bad decode never drops the picked picture.
            StoryImage image = CreateFromBytes(bytes, file.Name, decoded);

            ImageSource thumbnail = null;
            if (decoded != null)
            {
                try
                {
                    thumbnail = await BuildImageSourceAsync(decoded, thumbnailDecodeWidth);
                }
                catch (Exception ex)
                {
                    _logger.LogException(LogLevel.Warn, ex,
                        $"Failed to build thumbnail for picked image ({bytes.Length} bytes)");
                }
            }

            return (image, thumbnail);
        }
    }

    /// <summary>
    ///     Builds a <see cref="StoryImage"/> from raw image bytes, capping the
    ///     stored resolution: an image whose long edge exceeds
    ///     <see cref="MaxImportDimension"/> is downscaled and re-encoded as WebP
    ///     so embedded outlines stay small. Smaller images, and bytes that can't
    ///     be decoded, are stored unchanged (lossless passthrough). Pure;
    ///     headless-testable (SkiaSharp decode/resize/encode needs no UI thread).
    /// </summary>
    public StoryImage CreateFromBytes(byte[] bytes, string fileName) => CreateFromBytes(bytes, fileName, null);

    /// <summary>
    ///     As <see cref="CreateFromBytes(byte[], string)"/>, but reuses an
    ///     already-decoded <paramref name="preDecoded"/> bitmap (owned by the
    ///     caller) instead of decoding <paramref name="bytes"/> again.
    /// </summary>
    private StoryImage CreateFromBytes(byte[] bytes, string fileName, SKBitmap preDecoded)
    {
        byte[] storedBytes = bytes;
        string contentType = DetectContentType(fileName, bytes);

        byte[] reduced = ReduceForStorage(bytes, preDecoded);
        if (reduced != null)
        {
            storedBytes = reduced;
            contentType = "image/webp";
        }

        return new StoryImage(
            Guid.NewGuid(),
            Convert.ToBase64String(storedBytes),
            contentType,
            fileName);
    }

    /// <summary>
    ///     Downscales an over-sized image to <see cref="MaxImportDimension"/> on
    ///     its long edge and re-encodes it as WebP (smaller than JPEG at the same
    ///     quality, and keeps transparency). Returns null (store the original
    ///     bytes) when the image is already within the cap or cannot be decoded,
    ///     so a small image, an unknown format, or non-image bytes are never
    ///     altered or lost. Pass <paramref name="preDecoded"/> to reuse a bitmap
    ///     the caller already decoded from <paramref name="bytes"/> instead of
    ///     decoding again; ownership/disposal of it stays with the caller.
    /// </summary>
    private byte[] ReduceForStorage(byte[] bytes, SKBitmap preDecoded = null)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            SKBitmap decoded = preDecoded ?? SKBitmap.Decode(bytes);
            try
            {
                if (decoded is null)
                {
                    return null; // not a decodable image — store as-is
                }

                int longEdge = Math.Max(decoded.Width, decoded.Height);
                if (longEdge <= MaxImportDimension)
                {
                    return null; // already small enough — keep original, lossless
                }

                double scale = MaxImportDimension / (double)longEdge;
                int targetWidth = Math.Max(1, (int)Math.Round(decoded.Width * scale));
                int targetHeight = Math.Max(1, (int)Math.Round(decoded.Height * scale));

                SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.None);
                using SKBitmap resized = decoded.Resize(new SKImageInfo(targetWidth, targetHeight), sampling);
                if (resized is null)
                {
                    return null;
                }

                using SKImage image = SKImage.FromBitmap(resized);
                using SKData data = image.Encode(SKEncodedImageFormat.Webp, ImportImageQuality);
                byte[] encoded = data?.ToArray();

                // The downscaled WebP should always be smaller; if it somehow isn't
                // (e.g. a tiny source the codec can't beat, or no WebP encoder), keep
                // the original.
                if (encoded == null || encoded.Length == 0 || encoded.Length >= bytes.Length)
                {
                    return null;
                }

                _logger.Log(LogLevel.Info,
                    $"Downscaled import from {decoded.Width}x{decoded.Height} ({bytes.Length} bytes) " +
                    $"to {targetWidth}x{targetHeight} WebP ({encoded.Length} bytes)");
                return encoded;
            }
            finally
            {
                // Only dispose a bitmap we decoded ourselves; a preDecoded one is
                // owned by the caller (who may still need it, e.g. for a thumbnail).
                if (preDecoded is null)
                {
                    decoded?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Warn, ex,
                $"Failed to downscale import ({bytes.Length} bytes); storing original");
            return null;
        }
    }

    /// <summary>Decodes the stored Base64 image data back to its original bytes.</summary>
    public static byte[] GetBytes(StoryImage image)
        => string.IsNullOrEmpty(image?.ImageData)
            ? Array.Empty<byte>()
            : Convert.FromBase64String(image.ImageData);

    /// <summary>
    ///     Decodes a stored image into a displayable bitmap. Pass
    ///     <paramref name="decodePixelWidth"/> to decode a lightweight thumbnail
    ///     for gallery tiles rather than the full stored image. Must be called on
    ///     the UI thread.
    /// </summary>
    public async Task<ImageSource> ToImageSourceAsync(StoryImage image, int? decodePixelWidth = null)
    {
        byte[] bytes = GetBytes(image);
        if (bytes.Length == 0)
        {
            return null;
        }

        // Decode via SkiaSharp (see BuildImageSourceAsync for why). Best-effort: a
        // failure returns null (the tile still shows) and is logged.
        try
        {
            using SKBitmap decoded = SKBitmap.Decode(bytes);
            if (decoded is null)
            {
                _logger.Log(LogLevel.Warn,
                    $"SkiaSharp could not decode image (type={image?.ContentType}, bytes={bytes.Length})");
                return null;
            }

            return await BuildImageSourceAsync(decoded, decodePixelWidth);
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Warn, ex,
                $"Failed to decode image (type={image?.ContentType}, bytes={bytes.Length})");
            return null;
        }
    }

    /// <summary>
    ///     Builds a displayable <see cref="ImageSource"/> from an already-decoded
    ///     <paramref name="decoded"/> bitmap, resizing to <paramref name="decodePixelWidth"/>
    ///     if given. Renders into a <see cref="WriteableBitmap"/> (a raw BGRA pixel
    ///     buffer the UNO/Skia desktop head renders directly) — BitmapImage.SetSourceAsync
    ///     from an in-memory stream does not render on that head; SkiaSharp is the
    ///     head's own codec so this path works on both Windows and macOS. Must be
    ///     called on the UI thread.
    /// </summary>
    private async Task<ImageSource> BuildImageSourceAsync(SKBitmap decoded, int? decodePixelWidth)
    {
        int targetWidth = decoded.Width;
        int targetHeight = decoded.Height;
        if (decodePixelWidth is int w && w > 0 && decoded.Width > w)
        {
            targetWidth = w;
            targetHeight = Math.Max(1, (int)Math.Round(decoded.Height * (w / (double)decoded.Width)));
        }

        // Resize (if needed) and convert to BGRA8888 in one step.
        SKImageInfo info = new(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.None);
        using SKBitmap bgra = decoded.Resize(info, sampling);
        if (bgra is null)
        {
            _logger.Log(LogLevel.Warn, "SkiaSharp resize returned null while building image thumbnail");
            return null;
        }

        WriteableBitmap writeable = new(targetWidth, targetHeight);
        byte[] pixels = bgra.Bytes;
        using (Stream pixelStream = writeable.PixelBuffer.AsStream())
        {
            await pixelStream.WriteAsync(pixels, 0, pixels.Length);
        }

        writeable.Invalidate();
        return writeable;
    }

    /// <summary>
    ///     Determines a MIME content type from the file extension, falling back
    ///     to magic-byte sniffing. Pure; headless-testable.
    /// </summary>
    public static string DetectContentType(string fileName, byte[] bytes)
    {
        string ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        switch (ext)
        {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".bmp": return "image/bmp";
            case ".webp": return "image/webp";
            case ".tif":
            case ".tiff": return "image/tiff";
        }

        return SniffContentType(bytes) ?? "application/octet-stream";
    }

    private static string SniffContentType(byte[] b)
    {
        if (b == null || b.Length < 4)
        {
            return null;
        }

        // PNG: 89 50 4E 47
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
        {
            return "image/png";
        }

        // JPEG: FF D8 FF
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // GIF: "GIF"
        if (b[0] == (byte)'G' && b[1] == (byte)'I' && b[2] == (byte)'F')
        {
            return "image/gif";
        }

        // BMP: "BM"
        if (b[0] == (byte)'B' && b[1] == (byte)'M')
        {
            return "image/bmp";
        }

        // WEBP: "RIFF"????"WEBP"
        if (b.Length >= 12
            && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
            && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P')
        {
            return "image/webp";
        }

        return null;
    }
}
