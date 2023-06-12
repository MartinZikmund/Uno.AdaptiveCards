namespace AdaptiveCards.ObjectModel.WinUI3;

public class AdaptiveWarning
{
    public AdaptiveWarning(WarningStatusCode statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message;
    }

    public WarningStatusCode StatusCode { get; set; }

    public string Message { get; set; }
}
