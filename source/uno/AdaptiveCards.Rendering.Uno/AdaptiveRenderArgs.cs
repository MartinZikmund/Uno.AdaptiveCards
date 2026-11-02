// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using AdaptiveCards.ObjectModel.Uno;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveRenderArgs
    {
        public AdaptiveRenderArgs()
        {
        }

        public AdaptiveRenderArgs(ContainerStyle containerStyle, object parentElement, AdaptiveRenderArgs renderArgs)
        {
            ContainerStyle = containerStyle;
            ParentElement = parentElement;
            if (renderArgs != null)
            {
                AncestorHasFallback = renderArgs.AncestorHasFallback;
                ParentCard = renderArgs.ParentCard;
            }
        }

        public AdaptiveRenderArgs(ContainerStyle containerStyle, object parentElement, AdaptiveCard parentCard, AdaptiveRenderArgs renderArgs)
        {
            ContainerStyle = containerStyle;
            ParentElement = parentElement;
            ParentCard = parentCard;
            if (renderArgs != null)
            {
                AncestorHasFallback = renderArgs.AncestorHasFallback;
            }
        }

        public ContainerStyle ContainerStyle { get; set; }

        public object ParentElement { get; set; }

        public bool IsInShowCard { get; set; }

        public bool AllowAboveTitleIconPlacement { get; set; }

        public bool AncestorHasFallback { get; set; }

        public bool AddContainerPadding { get; set; }

        public AdaptiveCard ParentCard { get; set; }
    }
}
