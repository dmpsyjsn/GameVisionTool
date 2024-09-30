namespace GameVisionTool.Common.Domain.Services;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; private set; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException();
        }
        if (!isSuccess && string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException();
        }
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Fail(string message)
    {
        return new Result(false, message);
    }

    public static Result Fail(IEnumerable<string> messages)
    {
        return new Result(false, string.Join(Environment.NewLine, messages));
    }

    public static Result<T> Fail<T>(string message)
    {
        return new Result<T>(value: default!, false, message);
    }

    public static Result<T> Fail<T>(IEnumerable<string> messages)
    {
        return new Result<T>(value: default!, false, string.Join(Environment.NewLine, messages));
    }

    public static Result Ok()
    {
        return new Result(true, string.Empty);
    }

    public static Result<T> Ok<T>(T value)
    {
        return new Result<T>(value, true, string.Empty);
    }
}

public class Result<T> : Result
{
    private T _value;
    public T Value
    {
        get
        {
            if (!IsSuccess && string.IsNullOrEmpty(Error))
                throw new InvalidOperationException("There is no value for failure.");

            return _value;
        }

        private set => _value = value;
    }

    protected internal Result(T value, bool isSuccess, string error) : base(isSuccess, error)
    {
        if (!IsFailure && value == null)
            throw new ArgumentNullException(nameof(value));

        _value = value;
    }
}