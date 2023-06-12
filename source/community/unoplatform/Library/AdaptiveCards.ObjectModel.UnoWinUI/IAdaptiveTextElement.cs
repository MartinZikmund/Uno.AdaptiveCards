using System;

namespace AdaptiveCards.ObjectModel.WinUI3;

public interface IAdaptiveTextElement
{
    TextSize? Size { get; set; }
    TextWeight? Weight { get; set; }
    ForegroundColor? Color { get; set; }
    string Text { get; set; }
    bool? IsSubtle { get; set; }
    string Language { get; set; }
    FontType? FontType { get; set; }
}
