#if WINDOWS10_0_22621_0_OR_GREATER
using System.Net;
using StoryCADAutomation.Voice;

namespace StoryCADTests.Automation;

/// <summary>
///     The voice command's clips (#1421 Milestone 5, video design section 8.1). ElevenLabs is
///     replaced by a fake handler; no request leaves the machine.
/// </summary>
[TestClass]
public class VoiceClipsTests
{
    private string _folder = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "voice-test-" + Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    /// <summary>A mono 16-bit WAV file of the given length at 24 kHz (48,000 bytes a second).</summary>
    private static byte[] Wav(double seconds, uint? dataSizeField = null)
    {
        var data = (int)(seconds * 48000);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + data);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(24000);
        writer.Write(48000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSizeField ?? (uint)data);
        writer.Write(new byte[data]);
        return stream.ToArray();
    }

    private sealed class FakeElevenLabs(HttpStatusCode status, byte[] body) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();

        /// <summary>Request bodies, read on arrival because the caller disposes each request.</summary>
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status) { Content = new ByteArrayContent(body) };
        }
    }

    [TestMethod]
    public void ClipName_WithSameVoiceAndText_ReturnsSameName()
    {
        Assert.AreEqual(VoiceClips.ClipName("v1", "Hello."), VoiceClips.ClipName("v1", "Hello."));
    }

    [TestMethod]
    public void ClipName_WithOtherVoiceOrText_ReturnsOtherName()
    {
        var name = VoiceClips.ClipName("v1", "Hello.");

        Assert.AreNotEqual(name, VoiceClips.ClipName("v2", "Hello."));
        Assert.AreNotEqual(name, VoiceClips.ClipName("v1", "Hello!"));
    }

    [TestMethod]
    public void WavLength_WithWavFile_ReturnsAudioLength()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(2.5), VoiceClips.WavLength(new MemoryStream(Wav(2.5))));
    }

    [TestMethod]
    public void WavLength_WithPlaceholderDataSize_UsesBytesPresent()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(1.5), VoiceClips.WavLength(new MemoryStream(Wav(1.5, uint.MaxValue))));
    }

    [TestMethod]
    public void WavLength_WithOddSizeChunkBeforeData_SkipsItAndPadding()
    {
        var wav = Wav(1).ToList();
        // After RIFF (12 bytes) and fmt (24 bytes): a 3-byte LIST chunk plus its pad byte.
        wav.InsertRange(36, "LIST"u8.ToArray().Concat(BitConverter.GetBytes(3)).Concat("abc\0"u8.ToArray()));

        Assert.AreEqual(TimeSpan.FromSeconds(1), VoiceClips.WavLength(new MemoryStream(wav.ToArray())));
    }

    [TestMethod]
    public void WavLength_WithMp3Bytes_ReturnsNull()
    {
        Assert.IsNull(VoiceClips.WavLength(new MemoryStream("ID3\u0004 not a wav file"u8.ToArray())));
    }

    [TestMethod]
    public void Lengths_WithMissingClip_ReturnsNull()
    {
        Assert.IsNull(VoiceClips.Lengths(_folder, "v1")("No clip for this line."));
    }

    [TestMethod]
    public async Task MakeAsync_WithMissingClips_MakesEachTextOnce()
    {
        var fake = new FakeElevenLabs(HttpStatusCode.OK, Wav(2));

        var (made, kept) = await VoiceClips.MakeAsync(
            new[] { "One.", "Two.", "One." }, _folder, "v1", "key", new HttpClient(fake), _ => { });

        Assert.AreEqual((2, 0), (made, kept));
        Assert.AreEqual(TimeSpan.FromSeconds(2), VoiceClips.Lengths(_folder, "v1")("Two."));
    }

    [TestMethod]
    public async Task MakeAsync_WithExistingClip_KeepsItWithoutRequest()
    {
        var fake = new FakeElevenLabs(HttpStatusCode.OK, Wav(2));
        await VoiceClips.MakeAsync(new[] { "One." }, _folder, "v1", "key", new HttpClient(fake), _ => { });

        var (made, kept) = await VoiceClips.MakeAsync(
            new[] { "One." }, _folder, "v1", "key", new HttpClient(fake), _ => { });

        Assert.AreEqual((0, 1), (made, kept));
        Assert.AreEqual(1, fake.Requests.Count);
    }

    [TestMethod]
    public async Task MakeAsync_SendsKeyVoiceAndWavFormat()
    {
        var fake = new FakeElevenLabs(HttpStatusCode.OK, Wav(2));

        await VoiceClips.MakeAsync(new[] { "One." }, _folder, "voice id", "secret", new HttpClient(fake), _ => { });

        var request = fake.Requests.Single();
        Assert.AreEqual("secret", request.Headers.GetValues("xi-api-key").Single());
        Assert.AreEqual("/v1/text-to-speech/voice%20id", request.RequestUri!.AbsolutePath);
        Assert.AreEqual("?output_format=wav_24000", request.RequestUri.Query);
        Assert.AreEqual("{\"text\":\"One.\",\"model_id\":\"eleven_multilingual_v2\"}", fake.Bodies.Single());
    }

    [TestMethod]
    public async Task MakeAsync_WithResponseThatIsNotWav_ThrowsAndWritesNoClip()
    {
        var fake = new FakeElevenLabs(HttpStatusCode.OK, "ID3 mp3 bytes"u8.ToArray());

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            VoiceClips.MakeAsync(new[] { "One." }, _folder, "v1", "key", new HttpClient(fake), _ => { }));

        Assert.IsEmpty(Directory.GetFiles(_folder));
    }

    [TestMethod]
    public void Voice_WithoutApiKey_ReturnsExitCode3()
    {
        var script = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "voice-test.scs");
        Directory.CreateDirectory(Path.GetDirectoryName(script)!);
        File.WriteAllText(script, "script \"Voice\"\nnarrate \"Hello.\"\n");
        var saved = Environment.GetEnvironmentVariable(VoiceClips.ApiKeyVariable);
        Environment.SetEnvironmentVariable(VoiceClips.ApiKeyVariable, null);
        try
        {
            Assert.AreEqual(3, StoryCADAutomation.Program.Voice(script, _folder));
        }
        finally
        {
            Environment.SetEnvironmentVariable(VoiceClips.ApiKeyVariable, saved);
            File.Delete(script);
        }
    }

    [TestMethod]
    public async Task MakeAsync_WithFailedRequest_ThrowsAndWritesNoClip()
    {
        var fake = new FakeElevenLabs(HttpStatusCode.Unauthorized, "{\"detail\":\"invalid key\"}"u8.ToArray());

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            VoiceClips.MakeAsync(new[] { "One." }, _folder, "v1", "bad", new HttpClient(fake), _ => { }));

        Assert.IsEmpty(Directory.GetFiles(_folder));
    }
}
#endif
