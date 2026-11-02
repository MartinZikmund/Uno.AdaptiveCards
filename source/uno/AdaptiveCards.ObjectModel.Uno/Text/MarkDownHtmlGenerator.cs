// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text;

namespace AdaptiveCards.ObjectModel.Uno
{
    internal enum DelimiterType
    {
        Init,
        Alphanumeric,
        Puntuation,
        Escape,
        WhiteSpace,
        Underscore,
        Asterisk,
    }

    internal enum MarkDownBlockType
    {
        ContainerBlock,
        UnorderedList,
        OrderedList,
    }

    /// <summary>Port of the shared MarkDownHtmlGenerator hierarchy.</summary>
    internal abstract class MarkDownHtmlGenerator
    {
        protected string m_token;
        protected readonly StringBuilder html = new();
        protected bool m_isHead;
        protected bool m_isTail;

        protected MarkDownHtmlGenerator(string token)
        {
            m_token = token ?? string.Empty;
        }

        public void MakeItHead() => m_isHead = true;

        public void MakeItTail() => m_isTail = true;

        public virtual bool IsNewLine() => false;

        public abstract string GenerateHtmlString();

        public virtual MarkDownBlockType GetBlockType() => MarkDownBlockType.ContainerBlock;
    }

    internal class MarkDownStringHtmlGenerator : MarkDownHtmlGenerator
    {
        public MarkDownStringHtmlGenerator(string token) : base(token)
        {
        }

        public override string GenerateHtmlString()
        {
            if (m_isHead)
            {
                m_token = "<p>" + m_token;
            }

            if (m_isTail)
            {
                return m_token + "</p>";
            }

            return m_token;
        }
    }

    internal sealed class MarkDownNewLineHtmlGenerator : MarkDownStringHtmlGenerator
    {
        public MarkDownNewLineHtmlGenerator(string token) : base(token)
        {
        }

        public override bool IsNewLine() => true;
    }

    internal abstract class MarkDownEmphasisHtmlGenerator : MarkDownHtmlGenerator
    {
        protected const int Left = 0;
        protected const int Right = 1;

        protected int m_numberOfUnusedDelimiters;
        protected int m_directionType = Right;
        protected DelimiterType type;
        protected readonly List<string> m_tags = new();

        protected MarkDownEmphasisHtmlGenerator(string token, int sizeOfEmphasisDelimiterRun, DelimiterType type) : base(token)
        {
            m_numberOfUnusedDelimiters = sizeOfEmphasisDelimiterRun;
            this.type = type;
        }

        public virtual bool IsRightEmphasis() => false;

        public virtual bool IsLeftEmphasis() => false;

        public virtual bool IsLeftAndRightEmphasis() => false;

        public virtual void PushItalicTag() => m_tags.Add("<em>");

        public virtual void PushBoldTag() => m_tags.Add("<strong>");

        public bool IsMatch(MarkDownEmphasisHtmlGenerator emphasisToken)
        {
            if (type == emphasisToken.type)
            {
                // rule #9 & #10, sum of delimiter count can't be multiple of 3
                return !((IsLeftAndRightEmphasis() || emphasisToken.IsLeftAndRightEmphasis()) &&
                         ((m_numberOfUnusedDelimiters + emphasisToken.m_numberOfUnusedDelimiters) % 3) == 0);
            }

            return false;
        }

        public bool IsSameType(MarkDownEmphasisHtmlGenerator token) => type == token.type;

        public bool IsDone() => m_numberOfUnusedDelimiters == 0;

        public int GetNumberOfUnusedDelimiters() => m_numberOfUnusedDelimiters;

        public void ChangeDirectionToLeft() => m_directionType = Left;

        protected int AdjustEmphasisCounts(int leftOver, MarkDownEmphasisHtmlGenerator rightToken)
        {
            int delimiterCount;
            if (leftOver >= 0)
            {
                delimiterCount = m_numberOfUnusedDelimiters - leftOver;
                m_numberOfUnusedDelimiters = leftOver;
                rightToken.m_numberOfUnusedDelimiters = 0;
            }
            else
            {
                delimiterCount = m_numberOfUnusedDelimiters;
                rightToken.m_numberOfUnusedDelimiters = leftOver * -1;
                m_numberOfUnusedDelimiters = 0;
            }

            return delimiterCount;
        }

        public bool GenerateTags(MarkDownEmphasisHtmlGenerator token)
        {
            int leftOver = m_numberOfUnusedDelimiters - token.m_numberOfUnusedDelimiters;
            int delimiterCount = AdjustEmphasisCounts(leftOver, token);

            bool hasHtmlTags = delimiterCount > 0;

            if (delimiterCount % 2 != 0)
            {
                PushItalicTag();
                token.PushItalicTag();
            }

            for (int i = 0; i < delimiterCount / 2; i++)
            {
                PushBoldTag();
                token.PushBoldTag();
            }

            return hasHtmlTags;
        }

        protected string UnusedDelimiters()
        {
            int startIdx = m_token.Length - m_numberOfUnusedDelimiters;
            return startIdx >= 0 && startIdx <= m_token.Length ? m_token.Substring(startIdx) : m_token;
        }
    }

    internal sealed class MarkDownLeftEmphasisHtmlGenerator : MarkDownEmphasisHtmlGenerator
    {
        public MarkDownLeftEmphasisHtmlGenerator(string token, int sizeOfEmphasisDelimiterRun, DelimiterType type)
            : base(token, sizeOfEmphasisDelimiterRun, type)
        {
        }

        public override bool IsLeftEmphasis() => true;

        public override string GenerateHtmlString()
        {
            if (m_isHead)
            {
                html.Append("<p>");
            }

            if (m_numberOfUnusedDelimiters != 0)
            {
                html.Append(UnusedDelimiters());
            }

            // append tags; since left delims, append it in the reverse order
            for (int i = m_tags.Count - 1; i >= 0; i--)
            {
                html.Append(m_tags[i]);
            }

            if (m_isTail)
            {
                return html + "</p>";
            }

            return html.ToString();
        }
    }

    internal class MarkDownRightEmphasisHtmlGenerator : MarkDownEmphasisHtmlGenerator
    {
        public MarkDownRightEmphasisHtmlGenerator(string token, int sizeOfEmphasisDelimiterRun, DelimiterType type)
            : base(token, sizeOfEmphasisDelimiterRun, type)
        {
        }

        public override bool IsRightEmphasis() => m_directionType == Right;

        public override bool IsLeftEmphasis() => m_directionType == Left;

        public override void PushItalicTag() => m_tags.Add("</em>");

        public override void PushBoldTag() => m_tags.Add("</strong>");

        public override string GenerateHtmlString()
        {
            if (m_isHead)
            {
                html.Append("<p>");
            }

            foreach (var tag in m_tags)
            {
                html.Append(tag);
            }

            if (m_numberOfUnusedDelimiters != 0)
            {
                html.Append(UnusedDelimiters());
            }

            if (m_isTail)
            {
                return html + "</p>";
            }

            return html.ToString();
        }
    }

    internal sealed class MarkDownLeftAndRightEmphasisHtmlGenerator : MarkDownRightEmphasisHtmlGenerator
    {
        public MarkDownLeftAndRightEmphasisHtmlGenerator(string token, int sizeOfEmphasisDelimiterRun, DelimiterType type)
            : base(token, sizeOfEmphasisDelimiterRun, type)
        {
        }

        public override bool IsLeftAndRightEmphasis() => true;

        public override void PushItalicTag() => m_tags.Add(m_directionType == Left ? "<em>" : "</em>");

        public override void PushBoldTag() => m_tags.Add(m_directionType == Left ? "<strong>" : "</strong>");
    }

    internal sealed class MarkDownListHtmlGenerator : MarkDownStringHtmlGenerator
    {
        public MarkDownListHtmlGenerator(string token) : base(token)
        {
        }

        public override string GenerateHtmlString()
        {
            if (m_isHead)
            {
                m_token = "<ul>" + m_token;
            }

            if (m_isTail)
            {
                return m_token + "</ul>";
            }

            return m_token;
        }

        public override MarkDownBlockType GetBlockType() => MarkDownBlockType.UnorderedList;
    }

    internal sealed class MarkDownOrderedListHtmlGenerator : MarkDownStringHtmlGenerator
    {
        private readonly string m_numberString;

        public MarkDownOrderedListHtmlGenerator(string token, string numberString) : base(token)
        {
            m_numberString = numberString;
        }

        public override string GenerateHtmlString()
        {
            if (m_isHead)
            {
                m_token = "<ol start=\"" + m_numberString + "\">" + m_token;
            }

            if (m_isTail)
            {
                return m_token + "</ol>";
            }

            return m_token;
        }

        public override MarkDownBlockType GetBlockType() => MarkDownBlockType.OrderedList;
    }
}
