namespace CatalogService.Application.Common;

public class ServiceResult<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public IReadOnlyCollection<string> Errors { get; }

    private ServiceResult(bool succeeded, T? value, IReadOnlyCollection<string> errors)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors;
    }

    public static ServiceResult<T> Success(T value) => new(true, value, Array.Empty<string>());

    public static ServiceResult<T> Failure(params string[] errors) => new(false, default, errors);
}
