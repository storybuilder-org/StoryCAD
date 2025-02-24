using System.ClientModel.Primitives;

namespace StoryCAD.Services.API;

/// <summary>
/// Result of API Operation
/// </summary>
/// <typeparam name="T"></typeparam>
public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public T Payload { get; set; }

    public OperationResult() {}
    public OperationResult(bool isSuccess, T payload, string errorMessage)
    {
        IsSuccess = isSuccess;
        Payload = payload;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Success(T payload) => new OperationResult<T>(true, payload, null);
    public static OperationResult<T> Failure(string errorMessage) => new OperationResult<T>(false, default, errorMessage);

    public static async Task<OperationResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            T result = await operation();
            return OperationResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            // Optionally log the exception here.
            return OperationResult<T>.Failure(ex.Message);
        }
    }
    public static async Task<OperationResult<T>> SafeExecuteAsync<T>(Task<T> task)
    {
        try
        {
            T result = await task;
            return OperationResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            // Optionally log the exception here.
            return OperationResult<T>.Failure(ex.Message);
        }
    }
    public static async Task<OperationResult<T>> SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return OperationResult<T>.Success<default(T)>(default(T));
        }
        catch (Exception ex)
        {
            // Optionally log the exception.
            return OperationResult<T>.Failure<default(T)>(ex.Message);
        }
    }
}