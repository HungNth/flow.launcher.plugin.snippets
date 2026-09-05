namespace Flow.Launcher.Plugin.Snippets.Storage;

public class SnippetException : Exception
{
    public SnippetException(string message) : base(message) { }
    public SnippetException(string message, Exception inner) : base(message, inner) { }
}

public class SnippetValidationException : SnippetException
{
    public SnippetValidationException(string message) : base(message) { }
}

public class SnippetStorageException : SnippetException
{
    public SnippetStorageException(string message) : base(message) { }
    public SnippetStorageException(string message, Exception inner) : base(message, inner) { }
}
