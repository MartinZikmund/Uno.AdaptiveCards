// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Converts the Adaptive Cards markdown subset (emphasis, links, ordered/unordered lists) to HTML.
    /// Port of the shared C++ MarkDownParser; produces identical output.
    /// </summary>
    public sealed class MarkDownParser
    {
        private readonly string m_text;
        private readonly MarkDownParsedResult m_parsedResult = new();
        private bool m_hasHTMLTag;
        private bool m_isEscaped;

        public MarkDownParser(string text)
        {
            m_text = text ?? string.Empty;
        }

        /// <summary>Transforms the text to HTML.</summary>
        public string TransformToHtml()
        {
            if (m_text.Length == 0)
            {
                return "<p></p>";
            }

            ParseBlock();
            m_parsedResult.Translate();
            m_parsedResult.AddBlockTags();
            m_hasHTMLTag = m_parsedResult.HasHtmlTags();
            return m_parsedResult.GenerateHtmlString();
        }

        public string GetRawText() => m_text;

        /// <summary>True if the generated HTML contains markup tags (emphasis, links or lists).</summary>
        public bool HasHtmlTags() => m_hasHTMLTag;

        /// <summary>True if the input contained characters that were HTML-escaped (&lt; &gt; &quot; &amp;).</summary>
        public bool IsEscaped() => m_isEscaped;

        private void ParseBlock()
        {
            var stream = new MarkDownStream(EscapeText());
            var parser = new EmphasisParser();
            while (!stream.Eof())
            {
                parser.ParseBlock(stream);
            }

            m_parsedResult.AppendParseResult(parser.GetParsedResult());
        }

        private string EscapeText()
        {
            var escaped = new StringBuilder(m_text.Length);
            int nonEscapedCounts = 0;

            foreach (var c in m_text)
            {
                switch (c)
                {
                    case '<':
                        escaped.Append("&lt;");
                        break;
                    case '>':
                        escaped.Append("&gt;");
                        break;
                    case '"':
                        escaped.Append("&quot;");
                        break;
                    case '&':
                        escaped.Append("&amp;");
                        break;
                    default:
                        escaped.Append(c);
                        nonEscapedCounts++;
                        break;
                }
            }

            m_isEscaped = nonEscapedCounts != m_text.Length;
            return escaped.ToString();
        }
    }
}
