namespace FastQ.Domain.Common
{
    public class Result
    {
        public bool Ok { get; protected set; }
        public string Error { get; protected set; }

        public static Result Success() => new Result { Ok = true };

        public static Result Fail(string error) =>
            new Result { Ok = false, Error = error };
    }

    public class Result<T> : Result
    {
        public T Value { get; private set; }

        public static Result<T> Success(T value) =>
            new Result<T> { Ok = true, Value = value };

        public static Result<T> Fail(string error) =>
            new Result<T> { Ok = false, Error = error };
    }
}
