// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveRichTextBlockRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var xamlRichTextBlock = new RichTextBlock();
                var adaptiveRichTextBlock = (AdaptiveRichTextBlock)cardElement;

                TextHelpers.SetHorizontalAlignment(adaptiveRichTextBlock.HorizontalAlignment, renderContext, xamlRichTextBlock);

                var xamlParagraph = new Paragraph();
                xamlRichTextBlock.Blocks.Add(xamlParagraph);

                uint currentOffset = 0;
                var xamlInlines = xamlParagraph.Inlines;

                foreach (var adaptiveInline in adaptiveRichTextBlock.Inlines)
                {
                    if (adaptiveInline is not AdaptiveTextRun adaptiveTextRun)
                    {
                        continue;
                    }

                    var selectAction = adaptiveTextRun.SelectAction;
                    bool selectActionIsEnabled = selectAction?.IsEnabled ?? false;

                    var text = adaptiveTextRun.Text;
                    bool isStrikethrough = adaptiveTextRun.Strikethrough;
                    bool isItalic = adaptiveTextRun.Italic;
                    bool isUnderline = adaptiveTextRun.Underline;

                    uint inlineLength;
                    if (selectAction != null && selectActionIsEnabled)
                    {
                        var hyperlink = new Hyperlink();
                        var actionInvoker = renderContext.ActionInvoker;
                        if (!string.IsNullOrEmpty(selectAction.Title))
                        {
                            AutomationProperties.SetName(hyperlink, selectAction.Title);
                        }

                        hyperlink.Click += (sender, args) => actionInvoker.SendActionEvent(selectAction);

                        inlineLength = TextHelpers.AddSingleTextInline(
                            adaptiveTextRun,
                            renderContext,
                            renderArgs,
                            text,
                            new TextRunStyleParameters(isStrikethrough, isItalic, isUnderline, true),
                            hyperlink.Inlines);

                        xamlInlines.Add(hyperlink);
                    }
                    else
                    {
                        inlineLength = TextHelpers.AddSingleTextInline(
                            adaptiveTextRun,
                            renderContext,
                            renderArgs,
                            text,
                            new TextRunStyleParameters(isStrikethrough, isItalic, isUnderline, false),
                            xamlInlines);
                    }

                    if (adaptiveTextRun.Highlight)
                    {
                        var textHighlighter = Util.GetHighlighter(adaptiveTextRun, renderContext, renderArgs);
                        textHighlighter.Ranges.Add(new TextRange { StartIndex = (int)currentOffset, Length = (int)inlineLength });
                        xamlRichTextBlock.TextHighlighters.Add(textHighlighter);
                    }

                    currentOffset += inlineLength;
                }

                return xamlRichTextBlock;
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
