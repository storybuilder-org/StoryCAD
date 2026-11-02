using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Services;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #300: the Worker's model error reaches the writer as the Worker's own message,
/// on the three client paths: a 502 or 503 body on /workflow, an error event on an open stream
/// (the steps path, design 6.4), and chat's HttpOperationException. Real HttpResponseMessage,
/// StringContent and HttpOperationException objects with the Worker's real body shapes
/// (proxy/src/workers-ai.js modelErrorReply), per the NO MOCKS rule in Collaborator/CLAUDE.md.
/// </summary>
[TestClass]
public class ModelErrorExceptionTests
{
    // The Worker's messages, as proxy/src/workers-ai.js sends them.
    private const string BusyMessage = "The AI model is busy. Wait a minute and try again. You were not charged.";
    private const string ErrorMessage = "The AI model returned an error. Try again. If it happens again, email support@storybuilder.org with this code: a1b2c3d4. You were not charged.";

    private static string WorkerBody(string code, string message) =>
        $$$"""{"error":{"message":"{{{message}}}","type":"api_error","code":"{{{code}}}","param":null}}""";

    private static HttpResponseMessage Response(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // ── FromBody ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void FromBody_ModelBusy_ReturnsMessageAndCode()
    {
        var ex = ModelErrorException.FromBody(WorkerBody("model_busy", BusyMessage));

        Assert.IsNotNull(ex);
        Assert.AreEqual(BusyMessage, ex.Message);
        Assert.AreEqual("model_busy", ex.Code);
    }

    [TestMethod]
    public void FromBody_OtherErrorCode_ReturnsNull()
    {
        // An OpenAI error body passed through on the OpenAI path keeps its old handling.
        Assert.IsNull(ModelErrorException.FromBody("""{"error":{"message":"Rate limit reached","code":"rate_limit_exceeded"}}"""));
    }

    [TestMethod]
    public void FromBody_NotJson_ReturnsNull()
    {
        Assert.IsNull(ModelErrorException.FromBody("<html>Bad Gateway</html>"));
        Assert.IsNull(ModelErrorException.FromBody(""));
    }

    // ── WorkflowRunner.ThrowIfModelErrorAsync (/workflow before any output) ──

    [TestMethod]
    public async Task ThrowIfModelErrorAsync_503ModelBusy_ThrowsWithWorkerMessage()
    {
        using var response = Response(HttpStatusCode.ServiceUnavailable, WorkerBody("model_busy", BusyMessage));

        var ex = await Assert.ThrowsExactlyAsync<ModelErrorException>(() => WorkflowRunner.ThrowIfModelErrorAsync(response));

        Assert.AreEqual(BusyMessage, ex.Message);
    }

    [TestMethod]
    public async Task ThrowIfModelErrorAsync_502UpstreamError_ThrowsWithWorkerMessage()
    {
        using var response = Response(HttpStatusCode.BadGateway, WorkerBody("upstream_error", ErrorMessage));

        var ex = await Assert.ThrowsExactlyAsync<ModelErrorException>(() => WorkflowRunner.ThrowIfModelErrorAsync(response));

        Assert.AreEqual("upstream_error", ex.Code);
    }

    [TestMethod]
    public async Task ThrowIfModelErrorAsync_502OtherBody_DoesNotThrow()
    {
        using var response = Response(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>");

        await WorkflowRunner.ThrowIfModelErrorAsync(response); // left for EnsureSuccessStatusCode
    }

    [TestMethod]
    public async Task ThrowIfModelErrorAsync_500WorkerBody_DoesNotThrow()
    {
        // Only 502 and 503 carry a model error; a 500 is the Worker's own failure.
        using var response = Response(HttpStatusCode.InternalServerError, WorkerBody("upstream_error", ErrorMessage));

        await WorkflowRunner.ThrowIfModelErrorAsync(response);
    }

    // ── WorkflowRunner.ReadSseStreamAsync (steps path, stream already open) ──

    [TestMethod]
    public async Task ReadSseStream_ErrorEvent_ThrowsWithWorkerMessage()
    {
        // The Worker's real order on a failed main call: keep-alive, the error event, then the
        // abort comment line. The cost transform drops the abort line, so the stream ends there.
        var body = string.Join("\n",
            ": keep-alive", "",
            $$$"""data: {"error":{"code":"upstream_error","message":"{{{ErrorMessage}}}"}}""", "");
        using var response = new HttpResponseMessage { Content = new StringContent(body, Encoding.UTF8) };

        var ex = await Assert.ThrowsExactlyAsync<ModelErrorException>(() => WorkflowRunner.ReadSseStreamAsync(response));

        Assert.AreEqual(ErrorMessage, ex.Message);
    }

    [TestMethod]
    public async Task ReadSseStream_ErrorEventWithOtherCode_IsSkipped()
    {
        var body = string.Join("\n",
            """data: {"error":{"code":"something_else","message":"x"}}""",
            """data: {"id":"x","object":"chat.completion.chunk","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}""",
            "data: [DONE]");
        using var response = new HttpResponseMessage { Content = new StringContent(body, Encoding.UTF8) };

        var (content, _, complete) = await WorkflowRunner.ReadSseStreamAsync(response);

        Assert.AreEqual("Hello", content);
        Assert.IsTrue(complete);
    }

    // ── WorkflowRunner.ExecuteWithTruncationRetryAsync ───────────────────────

    [TestMethod]
    public async Task TruncationRetry_ModelError_NoSecondAttempt()
    {
        // Design 6.4: the Worker already tried; a client replay would send the same call again.
        var attempts = 0;
        var resets = 0;

        await Assert.ThrowsExactlyAsync<ModelErrorException>(() => WorkflowRunner.ExecuteWithTruncationRetryAsync(
            () => { attempts++; throw new ModelErrorException(ErrorMessage, "upstream_error"); },
            () => resets++));

        Assert.AreEqual(1, attempts);
        Assert.AreEqual(0, resets);
    }

    // ── WorkflowRunner.ExecuteWithContractRetryAsync ─────────────────────────

    [TestMethod]
    public async Task ContractRetry_ModelErrorOnSendAgain_ShowsFirstAnswer()
    {
        // The first answer was complete and charged; a model error on the send-again must not
        // replace it with a "you were not charged" message.
        var first = WorkflowResult.Succeeded();
        var attempts = 0;

        var result = await WorkflowRunner.ExecuteWithContractRetryAsync(
            () =>
            {
                attempts++;
                if (attempts == 1) return Task.FromResult(first);
                throw new ModelErrorException(BusyMessage, "model_busy");
            },
            r => ReferenceEquals(r, first) ? "3 of 9 outputs broke the field_states contract" : null);

        Assert.AreSame(first, result);
        Assert.AreEqual(2, attempts);
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("model_busy")));
    }

    // ── WorkflowSessionService.TranslateChatException (chat) ─────────────────

    [TestMethod]
    public void TranslateChatException_503ModelBusy_ReturnsModelErrorException()
    {
        var original = new HttpOperationException(HttpStatusCode.ServiceUnavailable, WorkerBody("model_busy", BusyMessage), "503", null);

        var translated = WorkflowSessionService.TranslateChatException(original);

        Assert.IsInstanceOfType<ModelErrorException>(translated);
        Assert.AreEqual(BusyMessage, translated.Message);
    }

    [TestMethod]
    public void TranslateChatException_502OtherBody_ReturnsOriginal()
    {
        var original = new HttpOperationException(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>", "502", null);

        var translated = WorkflowSessionService.TranslateChatException(original);

        Assert.AreSame(original, translated);
    }
}
