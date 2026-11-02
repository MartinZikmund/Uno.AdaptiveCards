// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveFactSetRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveFactSet = (AdaptiveFactSet)cardElement;

                var xamlGrid = new Grid();
                xamlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
                xamlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var factSetGridHeight = cardElement.Height == HeightType.Stretch
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(0, GridUnitType.Auto);

                int currentFact = 0, validFacts = 0;
                foreach (var fact in adaptiveFactSet.Facts)
                {
                    xamlGrid.RowDefinitions.Add(new RowDefinition { Height = factSetGridHeight });

                    var factSetConfig = renderContext.HostConfig.FactSet;
                    var language = fact.Language;

                    var titleTextBlock = new TextBlock();
                    var factTitle = fact.Title;
                    TextHelpers.SetXamlInlinesWithFactSetTextConfig(renderContext, renderArgs, factSetConfig.Title, language, factTitle, titleTextBlock);

                    var valueTextBlock = new TextBlock();
                    var factValue = fact.Value;
                    TextHelpers.SetXamlInlinesWithFactSetTextConfig(renderContext, renderArgs, factSetConfig.Value, language, factValue, valueTextBlock);

                    if (!string.IsNullOrEmpty(factTitle) || !string.IsNullOrEmpty(factValue))
                    {
                        titleTextBlock.Margin = new Thickness(0, 0, factSetConfig.Spacing, 0);

                        XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Fact.Title", titleTextBlock);
                        XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Fact.Value", valueTextBlock);

                        Grid.SetColumn(titleTextBlock, 0);
                        Grid.SetRow(titleTextBlock, currentFact);
                        Grid.SetColumn(valueTextBlock, 1);
                        Grid.SetRow(valueTextBlock, currentFact);

                        XamlHelpers.AppendXamlElementToPanel(titleTextBlock, xamlGrid);
                        XamlHelpers.AppendXamlElementToPanel(valueTextBlock, xamlGrid);
                        ++currentFact;
                        ++validFacts;
                    }
                }

                if (validFacts == 0)
                {
                    return null;
                }

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.FactSet", xamlGrid);
                return xamlGrid;
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
