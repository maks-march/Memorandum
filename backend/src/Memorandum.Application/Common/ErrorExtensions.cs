namespace Memorandum.Application.Common;

public static class ErrorExtensions
{
    
    public static Result<T> ToResult<T>(this Error error) => Result<T>.Failure(error);
}
