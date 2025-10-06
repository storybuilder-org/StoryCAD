namespace StoryCAD.Services.API;

/// <summary>
///     Result of API Operation
/// </summary>
/// <typeparam name="T"></typeparam>
public class OperationResult<T>
{
    public OperationResult()
    {
    }

    public OperationResult(bool isSuccess, T payload, string errorMessage)
    {
        IsSuccess = isSuccess;
        Payload = payload;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public T Payload { get; set; }

    public static OperationResult<T> Success(T payload) => new(true, payload, null);
    public static OperationResult<T> Failure(string errorMessage) => new(false, default, errorMessage);

    public static async Task<OperationResult<T>> SafeExecuteAsync<U>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return Success(result);
        }
        catch (Exception ex)
        {
            // Optionally log the exception here.
            return Failure(ex.Message);
        }
    }

    public static async Task<OperationResult<T>> SafeExecuteAsync(Task<T> task)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            return Success(result); // class‑level helper
        }
        catch (Exception ex)
        {
            return Failure(ex.Message);
        }
    }

    public static async Task<OperationResult<T>> SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Success(default);
        }
        catch (Exception ex)
        {
            // Optionally log the exception.
            return Failure(ex.Message);
        }
    }
}
