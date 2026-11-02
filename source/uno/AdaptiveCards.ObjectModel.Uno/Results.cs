// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveError
    {
        public AdaptiveError(ErrorStatusCode statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
        }

        public ErrorStatusCode StatusCode { get; set; }

        public string Message { get; set; }

        public override string ToString() => $"{StatusCode}: {Message}";
    }

    public sealed class AdaptiveWarning
    {
        public AdaptiveWarning(WarningStatusCode statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
        }

        public WarningStatusCode StatusCode { get; set; }

        public string Message { get; set; }

        public override string ToString() => $"{StatusCode}: {Message}";
    }

    public sealed class AdaptiveCardParseResult
    {
        public AdaptiveCardParseResult()
        {
        }

        public AdaptiveCard AdaptiveCard { get; internal set; }

        public IList<AdaptiveError> Errors { get; } = new List<AdaptiveError>();

        public IList<AdaptiveWarning> Warnings { get; } = new List<AdaptiveWarning>();
    }

    public sealed class AdaptiveRequirement
    {
        public AdaptiveRequirement(string requirementName, string requirementVersion)
        {
            Name = requirementName;
            Version = requirementVersion;
        }

        public string Name { get; set; }

        /// <summary>Version string; "*" or "0" means any version.</summary>
        public string Version { get; set; }
    }

    public sealed class AdaptiveRemoteResourceInformation
    {
        public AdaptiveRemoteResourceInformation()
        {
        }

        public AdaptiveRemoteResourceInformation(string url, string mimeType)
        {
            Url = url;
            MimeType = mimeType;
        }

        public string Url { get; set; }

        public string MimeType { get; set; }
    }

    /// <summary>
    /// Exception thrown while parsing an Adaptive Card. Port of the shared model's AdaptiveCardParseException.
    /// Custom element/action parsers can throw this to report a parse error.
    /// </summary>
    public class AdaptiveCardParseException : Exception
    {
        public AdaptiveCardParseException(ErrorStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public ErrorStatusCode StatusCode { get; }

        public string Reason => Message;
    }

    /// <summary>Port of the shared model's InternalId.</summary>
    internal static class InternalId
    {
        public const uint Invalid = 0;
        private static uint s_current = 1;
        private static readonly object s_lock = new();

        public static uint Current
        {
            get
            {
                lock (s_lock)
                {
                    return s_current;
                }
            }
        }

        [System.ThreadStatic]
        private static int t_parseDepth;

        /// <summary>
        /// Id for a newly constructed element. While parsing, elements take the id allocated by the parser wrapper
        /// (InternalId::Current() in the shared model); elements constructed in code get a fresh id.
        /// </summary>
        public static uint ForNewElement() => t_parseDepth > 0 ? Current : Next();

        public static void EnterParse() => t_parseDepth++;

        public static void ExitParse() => t_parseDepth--;

        public static uint Next()
        {
            lock (s_lock)
            {
                s_current++;
                if (s_current == Invalid)
                {
                    s_current++;
                }

                return s_current;
            }
        }
    }
}
