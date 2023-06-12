using System;

namespace AdaptiveCards.ObjectModel.WinUI3;

public class AdaptiveError
{
    public AdaptiveError(ErrorStatusCode statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message;
    }

    public ErrorStatusCode StatusCode { get; set; }

    public string Message { get; set; }
}
