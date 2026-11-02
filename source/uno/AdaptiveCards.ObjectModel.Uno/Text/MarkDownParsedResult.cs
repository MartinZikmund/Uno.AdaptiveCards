// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Port of the shared MarkDownParsedResult.</summary>
    internal sealed class MarkDownParsedResult
    {
        private readonly List<MarkDownHtmlGenerator> m_codeGenTokens = new();
        private readonly List<MarkDownEmphasisHtmlGenerator> m_emphasisLookUpTable = new();
        private bool m_isHTMLTagsAdded;
        private bool m_isCaptured;

        public void Translate() => MatchLeftAndRightEmphasises();

        public void AddBlockTags()
        {
            if (m_codeGenTokens.Count == 0)
            {
                return;
            }

            m_codeGenTokens[0].MakeItHead();
            m_codeGenTokens[m_codeGenTokens.Count - 1].MakeItTail();
        }

        private void MarkTags(MarkDownHtmlGenerator x)
        {
            if (m_codeGenTokens[m_codeGenTokens.Count - 1].GetBlockType() != x.GetBlockType())
            {
                if (m_codeGenTokens[m_codeGenTokens.Count - 1].IsNewLine())
                {
                    m_codeGenTokens.RemoveAt(m_codeGenTokens.Count - 1);
                }

                if (m_codeGenTokens.Count > 0)
                {
                    m_codeGenTokens[m_codeGenTokens.Count - 1].MakeItTail();
                }

                x.MakeItHead();
            }
        }

        /// <summary>Moves (splices) the contents of <paramref name="x"/> into this result.</summary>
        public void AppendParseResult(MarkDownParsedResult x)
        {
            if (m_codeGenTokens.Count > 0 && x.m_codeGenTokens.Count > 0)
            {
                MarkTags(x.m_codeGenTokens[0]);
            }

            m_codeGenTokens.AddRange(x.m_codeGenTokens);
            x.m_codeGenTokens.Clear();
            m_emphasisLookUpTable.AddRange(x.m_emphasisLookUpTable);
            x.m_emphasisLookUpTable.Clear();
            m_isHTMLTagsAdded = m_isHTMLTagsAdded || x.HasHtmlTags();
            SetIsCaptured(x.GetIsCaptured());
        }

        public void AppendToTokens(MarkDownHtmlGenerator x)
        {
            if (m_codeGenTokens.Count > 0)
            {
                MarkTags(x);
            }

            m_codeGenTokens.Add(x);
        }

        public void AppendToLookUpTable(MarkDownEmphasisHtmlGenerator x) => m_emphasisLookUpTable.Add(x);

        public void AddNewTokenToParsedResult(int ch) => AppendToTokens(new MarkDownStringHtmlGenerator(((char)ch).ToString()));

        public void AddNewTokenToParsedResult(string word) => AppendToTokens(new MarkDownStringHtmlGenerator(word));

        public void AddNewLineTokenToParsedResult(char ch) => AppendToTokens(new MarkDownNewLineHtmlGenerator(ch.ToString()));

        public void PopFront()
        {
            if (m_codeGenTokens.Count > 0)
            {
                m_codeGenTokens.RemoveAt(0);
            }
        }

        public void PopBack()
        {
            if (m_codeGenTokens.Count > 0)
            {
                m_codeGenTokens.RemoveAt(m_codeGenTokens.Count - 1);
            }
        }

        public void Clear()
        {
            m_codeGenTokens.Clear();
            m_emphasisLookUpTable.Clear();
        }

        public bool HasHtmlTags() => m_isHTMLTagsAdded;

        public void FoundHtmlTags() => m_isHTMLTagsAdded = true;

        public bool GetIsCaptured() => m_isCaptured;

        public void SetIsCaptured(bool value) => m_isCaptured = value;

        public bool IsEmpty => m_codeGenTokens.Count == 0;

        public string GenerateHtmlString()
        {
            var html = new StringBuilder();
            foreach (var token in m_codeGenTokens)
            {
                html.Append(token.GenerateHtmlString());
            }

            return html.ToString();
        }

        // Follows the rules specified in CommonMark (http://spec.commonmark.org/0.27/), see the shared C++ implementation.
        private void MatchLeftAndRightEmphasises()
        {
            var leftEmphasisToExplore = new List<int>();
            int currentEmphasis = 0;
            while (currentEmphasis < m_emphasisLookUpTable.Count)
            {
                var current = m_emphasisLookUpTable[currentEmphasis];

                if (current.IsLeftEmphasis() || (current.IsLeftAndRightEmphasis() && leftEmphasisToExplore.Count == 0))
                {
                    if (current.IsLeftAndRightEmphasis() && current.IsRightEmphasis())
                    {
                        current.ChangeDirectionToLeft();
                    }

                    leftEmphasisToExplore.Add(currentEmphasis);
                    ++currentEmphasis;
                }
                else if (leftEmphasisToExplore.Count > 0)
                {
                    int currentLeftEmphasis = leftEmphasisToExplore[leftEmphasisToExplore.Count - 1];

                    if (!m_emphasisLookUpTable[currentLeftEmphasis].IsMatch(current))
                    {
                        var store = new List<int>();
                        bool isFound = false;

                        while (leftEmphasisToExplore.Count > 0 && !isFound)
                        {
                            int leftToken = leftEmphasisToExplore[leftEmphasisToExplore.Count - 1];
                            if (m_emphasisLookUpTable[leftToken].IsMatch(current))
                            {
                                currentLeftEmphasis = leftToken;
                                isFound = true;
                            }
                            else
                            {
                                leftEmphasisToExplore.RemoveAt(leftEmphasisToExplore.Count - 1);
                                store.Add(leftToken);
                            }
                        }

                        if (!isFound)
                        {
                            while (store.Count > 0)
                            {
                                leftEmphasisToExplore.Add(store[store.Count - 1]);
                                store.RemoveAt(store.Count - 1);
                            }

                            if (m_emphasisLookUpTable[leftEmphasisToExplore[leftEmphasisToExplore.Count - 1]].IsSameType(current))
                            {
                                current.ChangeDirectionToLeft();
                            }
                            else
                            {
                                ++currentEmphasis;
                            }

                            continue;
                        }
                    }

                    var leftGenerator = m_emphasisLookUpTable[currentLeftEmphasis];
                    m_isHTMLTagsAdded = leftGenerator.GenerateTags(current) || m_isHTMLTagsAdded;

                    if (current.IsDone())
                    {
                        ++currentEmphasis;
                    }

                    if (leftGenerator.IsDone())
                    {
                        leftEmphasisToExplore.RemoveAt(leftEmphasisToExplore.Count - 1);
                    }
                }
                else
                {
                    ++currentEmphasis;
                }
            }
        }
    }
}
