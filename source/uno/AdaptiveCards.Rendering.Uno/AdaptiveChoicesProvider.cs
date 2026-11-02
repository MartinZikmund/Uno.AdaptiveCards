// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.Uno;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// A request for dynamic choices of a filtered <c>Input.ChoiceSet</c> whose <c>choices.data</c> is a
    /// <c>Data.Query</c> (Adaptive Cards 1.6 dynamic typeahead). Not part of the WinUI3 renderer, which ignores
    /// <c>choices.data</c>.
    /// </summary>
    public sealed class AdaptiveChoicesQuery
    {
        internal AdaptiveChoicesQuery(AdaptiveChoiceSetInput input, string queryText)
        {
            Input = input;
            QueryText = queryText ?? string.Empty;
        }

        /// <summary>The input element requesting choices.</summary>
        public AdaptiveChoiceSetInput Input { get; }

        /// <summary>The <c>dataset</c> of the Data.Query.</summary>
        public string Dataset => Input.ChoicesData?.Dataset ?? string.Empty;

        /// <summary>The text typed by the user.</summary>
        public string QueryText { get; }

        /// <summary>The requested maximum number of choices (<c>count</c>), if any.</summary>
        public int? Count => Input.ChoicesData?.Count;

        /// <summary>The number of choices to skip (<c>skip</c>), if any.</summary>
        public int? Skip => Input.ChoicesData?.Skip;
    }

    /// <summary>
    /// Host-provided source of dynamic choices for <c>Input.ChoiceSet</c> elements using <c>Data.Query</c>.
    /// Set <see cref="AdaptiveCardRenderer.ChoicesProvider"/> to enable dynamic typeahead.
    /// </summary>
    public interface IAdaptiveChoicesProvider
    {
        Task<IList<AdaptiveChoiceInput>> GetChoicesAsync(AdaptiveChoicesQuery query, CancellationToken cancellationToken);
    }
}
