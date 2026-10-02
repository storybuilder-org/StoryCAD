using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using StoryCADLib.Models;
using StoryCADLib.Services;

namespace StoryCADTests.Services;

[TestClass]
public class ImageServiceTests
{
    private static ImageService Service => Ioc.Default.GetRequiredService<ImageService>();

    /// <summary>
    ///     Encodes a gradient bitmap of the given size as PNG bytes — a real,
    ///     decodable image with enough detail that downscaling + re-encoding
    ///     measurably shrinks it (a solid colour would compress to almost
    ///     nothing and make the size assertion meaningless). SkiaSharp runs
    ///     headlessly.
    /// </summary>
    private static byte[] MakeImageBytes(int width, int height)
    {
        using SKBitmap bitmap = new(width, height);
        using (SKCanvas canvas = new(bitmap))
        using (SKPaint paint = new()
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Yellow },
                null,
                SKShaderTileMode.Clamp)
        })
        {
            canvas.DrawRect(new SKRect(0, 0, width, height), paint);
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static (int Width, int Height) DimensionsOf(byte[] bytes)
    {
        using SKBitmap decoded = SKBitmap.Decode(bytes);
        return (decoded.Width, decoded.Height);
    }

    [TestMethod]
    public void CreateFromBytes_WithLargeImage_DownscalesToCapAndStoresWebp()
    {
        // 3000x2000 is well over the 1280px cap; it must be downscaled and
        // re-encoded as a smaller WebP.
        byte[] original = MakeImageBytes(3000, 2000);

        StoryImage image = Service.CreateFromBytes(original, "big.png");

        byte[] stored = ImageService.GetBytes(image);
        (int width, int height) = DimensionsOf(stored);
        Assert.IsTrue(Math.Max(width, height) <= 2048,
            $"long edge {Math.Max(width, height)} should be capped at 2048");
        Assert.AreEqual(2048, width, "3000x2000 should scale to 2048 on the long edge");
        Assert.AreEqual("image/webp", image.ContentType);
        Assert.IsTrue(stored.Length < original.Length,
            "downscaled WebP should be smaller than the source");
        Assert.AreEqual("big.png", image.FileName);
    }

    [TestMethod]
    public void CreateFromBytes_WithSmallImage_StoresUnchanged()
    {
        // 800x600 is within the cap, so the original bytes are kept verbatim.
        byte[] original = MakeImageBytes(800, 600);

        StoryImage image = Service.CreateFromBytes(original, "small.png");

        CollectionAssert.AreEqual(original, ImageService.GetBytes(image));
        Assert.AreEqual("image/png", image.ContentType);
    }

    [TestMethod]
    public void CreateFromBytes_WithNonImageBytes_StoresUnchanged()
    {
        // Bytes that can't be decoded as an image are stored as-is, never lost.
        byte[] original = { 1, 2, 3, 4, 250, 0, 99, 128, 255 };

        StoryImage image = Service.CreateFromBytes(original, "photo.jpg");

        CollectionAssert.AreEqual(original, ImageService.GetBytes(image));
        Assert.AreEqual("image/jpeg", image.ContentType);
        Assert.AreEqual("photo.jpg", image.FileName);
        Assert.AreNotEqual(Guid.Empty, image.Id);
    }

    [TestMethod]
    public void DetectContentType_FromExtension_IsCaseInsensitive()
    {
        Assert.AreEqual("image/png", ImageService.DetectContentType("a.PNG", null));
        Assert.AreEqual("image/jpeg", ImageService.DetectContentType("a.jpeg", null));
        Assert.AreEqual("image/jpeg", ImageService.DetectContentType("a.jpg", null));
        Assert.AreEqual("image/gif", ImageService.DetectContentType("a.gif", null));
        Assert.AreEqual("image/bmp", ImageService.DetectContentType("a.bmp", null));
        Assert.AreEqual("image/webp", ImageService.DetectContentType("a.webp", null));
        Assert.AreEqual("image/tiff", ImageService.DetectContentType("a.tiff", null));
    }

    [TestMethod]
    public void DetectContentType_FallsBackToMagicBytes_WhenExtensionUnknown()
    {
        byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Assert.AreEqual("image/png", ImageService.DetectContentType("noextension", png));

        byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xE0 };
        Assert.AreEqual("image/jpeg", ImageService.DetectContentType("", jpeg));
    }

    [TestMethod]
    public void DetectContentType_Unknown_ReturnsOctetStream()
    {
        Assert.AreEqual("application/octet-stream",
            ImageService.DetectContentType("x.dat", new byte[] { 0, 1, 2, 3 }));
    }

    [TestMethod]
    public void GetBytes_NullOrEmpty_ReturnsEmpty()
    {
        Assert.AreEqual(0, ImageService.GetBytes(null).Length);
        Assert.AreEqual(0, ImageService.GetBytes(new StoryImage()).Length);
    }
}
