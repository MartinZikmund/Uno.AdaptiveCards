// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public interface IAdaptiveCardElement
    {
        ElementType ElementType { get; }
        string ElementTypeString { get; }
        HeightType Height { get; set; }
        Spacing Spacing { get; set; }
        bool Separator { get; set; }
        string Id { get; set; }
        bool IsVisible { get; set; }
        FallbackType FallbackType { get; set; }
        IAdaptiveCardElement FallbackContent { get; set; }
        JsonObject AdditionalProperties { get; set; }
        IList<AdaptiveRequirement> Requirements { get; }
        JsonObject ToJson();
    }

    public interface IAdaptiveActionElement
    {
        ActionType ActionType { get; }
        string ActionTypeString { get; }
        string Title { get; set; }
        string Id { get; set; }
        FallbackType FallbackType { get; set; }
        IAdaptiveActionElement FallbackContent { get; set; }
        string IconUrl { get; set; }
        string Style { get; set; }
        string Tooltip { get; set; }
        JsonObject AdditionalProperties { get; set; }
        bool IsEnabled { get; set; }
        ActionMode Mode { get; set; }
        ActionRole Role { get; set; }
        JsonObject ToJson();
    }

    public interface IAdaptiveElementWithRemoteResources
    {
        IReadOnlyList<AdaptiveRemoteResourceInformation> GetResourceInformation();
    }

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

    public interface IAdaptiveInline
    {
    }

    public interface IContentSource
    {
        string MimeType { get; set; }
        string Url { get; set; }
    }

    public interface IAdaptiveContainerBase
    {
        ContainerStyle Style { get; set; }
        IAdaptiveActionElement SelectAction { get; set; }
        bool Bleed { get; set; }
        BleedDirection BleedDirection { get; }
        uint MinHeight { get; set; }
    }

    public interface IAdaptiveContainer
    {
        VerticalContentAlignment? VerticalContentAlignment { get; set; }
        IList<IAdaptiveCardElement> Items { get; }
        AdaptiveBackgroundImage BackgroundImage { get; set; }
        bool? Rtl { get; set; }
    }

    public interface IAdaptiveInputElement
    {
        bool IsRequired { get; set; }
        string ErrorMessage { get; set; }
        string Label { get; set; }
    }

    public interface IAdaptiveElementParser
    {
        IAdaptiveCardElement FromJson(
            JsonObject inputJson,
            AdaptiveElementParserRegistration elementParsers,
            AdaptiveActionParserRegistration actionParsers,
            IList<AdaptiveWarning> warnings);
    }

    public interface IAdaptiveActionParser
    {
        IAdaptiveActionElement FromJson(
            JsonObject inputJson,
            AdaptiveElementParserRegistration elementParsers,
            AdaptiveActionParserRegistration actionParsers,
            IList<AdaptiveWarning> warnings);
    }
}
