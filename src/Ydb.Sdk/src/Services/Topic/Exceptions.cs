namespace Ydb.Sdk.Services.Topic;

public class WriterException : Exception
{
    public WriterException(string message) : base(message)
    {
    }

    public WriterException(string message, Status status) : base(message + ": " + status)
    {
    }

    public WriterException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class ReaderException : Exception
{
    protected ReaderException(string message) : base(message)
    {
    }
}
