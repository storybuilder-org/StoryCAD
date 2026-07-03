using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;
using Windows.Storage.Streams;

namespace StoryCADTests.Services;

/// <summary>
///     Guards the StorageFile byte-read path used by ImageService.PickImageAsync:
///     FileIO.ReadBufferAsync + DataReader must round-trip bytes on the desktop
///     (UNO/Skia) head as well as Windows.
/// </summary>
[TestClass]
public class ImageReadTests
{
    [TestMethod]
    public async Task ReadBufferAsync_PlusDataReader_RoundTripsBytes()
    {
        byte[] original = Encoding.ASCII.GetBytes("PNG-ish bytes \0\x89 hello world");
        string path = Path.Combine(Path.GetTempPath(), $"imgexp_{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, original);

        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        IBuffer buffer = await FileIO.ReadBufferAsync(file);
        byte[] bytes = new byte[buffer.Length];
        using (DataReader reader = DataReader.FromBuffer(buffer))
        {
            reader.ReadBytes(bytes);
        }

        File.Delete(path);
        CollectionAssert.AreEqual(original, bytes);
    }
}
