// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Port of the shared MarkDownBlockParser hierarchy (EmphasisParser, LinkParser, ListParser, OrderedListParser).</summary>
    internal abstract class MarkDownBlockParser
    {
        protected const int EOF = MarkDownStream.EOF;

        protected MarkDownParsedResult m_parsedResult = new();

        public abstract void Match(MarkDownStream stream);

        public MarkDownParsedResult GetParsedResult() => m_parsedResult;

        // The C++ implementation uses the "C" locale ctype functions on UTF-8 bytes; any non-ASCII code unit is
        // treated as alphanumeric, and none of the other classes.
        protected static bool IsSpace(int ch) => ch == ' ' || (ch >= 9 && ch <= 13);

        protected static bool IsPunct(int ch) =>
            (ch >= 33 && ch <= 47) || (ch >= 58 && ch <= 64) || (ch >= 91 && ch <= 96) || (ch >= 123 && ch <= 126);

        protected static bool IsAlnum(int ch) =>
            ch > 0x7F || (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');

        protected static bool IsCntrl(int ch) => (ch > 0 && ch <= 31) || ch == 127;

        protected static bool IsDigit(int ch) => ch >= '0' && ch <= '9';

        public void ParseBlock(MarkDownStream stream)
        {
            switch (stream.Peek())
            {
                case '[':
                    {
                        var linkParser = new LinkParser();
                        linkParser.Match(stream);
                        m_parsedResult.AppendParseResult(linkParser.GetParsedResult());
                        break;
                    }

                case ']':
                case ')':
                    {
                        var streamChar = stream.GetChar();
                        m_parsedResult.AddNewTokenToParsedResult(streamChar);
                        break;
                    }

                case '\n':
                case '\r':
                    {
                        var streamChar = stream.GetChar();
                        m_parsedResult.AddNewLineTokenToParsedResult(streamChar);
                        break;
                    }

                case '-':
                case '+':
                case '*':
                    {
                        var listParser = new ListParser();
                        listParser.Match(stream);
                        m_parsedResult.AppendParseResult(listParser.GetParsedResult());
                        break;
                    }

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    {
                        var orderedListParser = new OrderedListParser();
                        orderedListParser.Match(stream);
                        m_parsedResult.AppendParseResult(orderedListParser.GetParsedResult());
                        break;
                    }

                default:
                    ParseTextAndEmphasis(stream);
                    break;
            }
        }

        protected void ParseTextAndEmphasis(MarkDownStream stream)
        {
            var emphasisParser = new EmphasisParser();
            emphasisParser.Match(stream);
            m_parsedResult.AppendParseResult(emphasisParser.GetParsedResult());
        }
    }

    internal sealed class EmphasisParser : MarkDownBlockParser
    {
        private enum EmphasisState
        {
            Text,
            Emphasis,
            Captured,
        }

        private bool m_checkLookAhead;
        private bool m_checkIntraWord;
        private DelimiterType m_lookBehind = DelimiterType.Init;
        private int m_delimiterCnts;
        private DelimiterType m_currentDelimiterType = DelimiterType.Init;
        private EmphasisState m_current_state = EmphasisState.Text;
        private readonly StringBuilder m_current_token = new();

        public override void Match(MarkDownStream stream)
        {
            while (m_current_state != EmphasisState.Captured)
            {
                m_current_state = m_current_state == EmphasisState.Text
                    ? MatchText(stream, m_current_token)
                    : MatchEmphasis(stream, m_current_token);
            }
        }

        private static bool IsEmphasisToken(int currentChar) =>
            currentChar == '[' || currentChar == ']' || currentChar == ')' || currentChar == '\n' || currentChar == '\r';

        private static DelimiterType GetDelimiterTypeForChar(int ch) => ch == '*' ? DelimiterType.Asterisk : DelimiterType.Underscore;

        private EmphasisState MatchText(MarkDownStream stream, StringBuilder token)
        {
            int currentChar = stream.Peek();
            bool isEmphasisToken = IsEmphasisToken(currentChar);

            if (stream.Eof() || (m_lookBehind != DelimiterType.Escape && isEmphasisToken))
            {
                Flush(currentChar, token);
                return EmphasisState.Captured;
            }

            if (IsMarkDownDelimiter(currentChar))
            {
                CaptureCurrentCollectedStringAsRegularToken(m_current_token);
                var emphasisType = GetDelimiterTypeForChar(currentChar);
                if (stream.TellG() != 0)
                {
                    stream.Unget();
                    UpdateLookBehind(stream.Get());
                }

                UpdateCurrentEmphasisRunState(emphasisType);
                token.Append(stream.GetChar());
                return EmphasisState.Emphasis;
            }
            else
            {
                if (isEmphasisToken && m_lookBehind == DelimiterType.Escape && token.Length > 0)
                {
                    // remove escape char from stream
                    token.Length--;
                }

                UpdateLookBehind(currentChar);
                token.Append(stream.GetChar());
                return EmphasisState.Text;
            }
        }

        private EmphasisState MatchEmphasis(MarkDownStream stream, StringBuilder token)
        {
            int currentChar = stream.Peek();

            if (currentChar == '[' || currentChar == ']' || currentChar == ')' || currentChar == '\n' || currentChar == '\r' || stream.Eof())
            {
                Flush(currentChar, token);
                return EmphasisState.Captured;
            }

            if (IsMarkDownDelimiter(currentChar))
            {
                var emphasisType = GetDelimiterTypeForChar(currentChar);
                if (IsEmphasisDelimiterRun(emphasisType))
                {
                    UpdateCurrentEmphasisRunState(emphasisType);
                }

                token.Append(stream.GetChar());
            }
            else
            {
                CaptureEmphasisToken(currentChar, token);

                if (currentChar == '\\')
                {
                    // skips escape char
                    stream.Get();
                }

                ResetCurrentEmphasisState();
                UpdateLookBehind(stream.Peek());
                token.Append(stream.GetChar());
                return EmphasisState.Text;
            }

            return EmphasisState.Emphasis;
        }

        private void Flush(int ch, StringBuilder currentToken)
        {
            if (m_current_state == EmphasisState.Emphasis)
            {
                CaptureEmphasisToken(ch, currentToken);
                m_delimiterCnts = 0;
            }
            else
            {
                CaptureCurrentCollectedStringAsRegularToken(currentToken);
            }

            currentToken.Clear();
        }

        private bool IsMarkDownDelimiter(int ch) => (ch == '*' || ch == '_') && m_lookBehind != DelimiterType.Escape;

        private void CaptureCurrentCollectedStringAsRegularToken(StringBuilder currentToken)
        {
            if (currentToken.Length == 0)
            {
                return;
            }

            m_parsedResult.AppendToTokens(new MarkDownStringHtmlGenerator(currentToken.ToString()));
            currentToken.Clear();
        }

        private void UpdateCurrentEmphasisRunState(DelimiterType emphasisType)
        {
            if (m_lookBehind != DelimiterType.WhiteSpace)
            {
                m_checkLookAhead = m_lookBehind == DelimiterType.Puntuation;
                m_checkIntraWord = m_lookBehind == DelimiterType.Alphanumeric && emphasisType == DelimiterType.Underscore;
            }

            ++m_delimiterCnts;
            m_currentDelimiterType = emphasisType;
        }

        private bool IsEmphasisDelimiterRun(DelimiterType emphasisType) => m_currentDelimiterType == emphasisType;

        private void ResetCurrentEmphasisState() => m_delimiterCnts = 0;

        private bool IsLeftEmphasisDelimiter(int ch)
        {
            if (m_delimiterCnts != 0 && ch != EOF)
            {
                return !IsSpace(ch) &&
                       !(m_lookBehind == DelimiterType.Alphanumeric && IsPunct(ch)) &&
                       !(m_lookBehind == DelimiterType.Alphanumeric && m_currentDelimiterType == DelimiterType.Underscore);
            }

            return false;
        }

        private bool IsRightEmphasisDelimiter(int ch)
        {
            if ((ch == EOF || IsSpace(ch)) && m_lookBehind != DelimiterType.WhiteSpace &&
                (m_checkLookAhead || m_checkIntraWord || m_currentDelimiterType == DelimiterType.Asterisk))
            {
                return true;
            }

            if (IsAlnum(ch) && m_lookBehind != DelimiterType.WhiteSpace && m_lookBehind != DelimiterType.Init)
            {
                if (!m_checkLookAhead && !m_checkIntraWord)
                {
                    return true;
                }

                if (m_checkLookAhead || m_checkIntraWord)
                {
                    return false;
                }
            }

            if (IsPunct(ch) && m_lookBehind != DelimiterType.WhiteSpace)
            {
                return true;
            }

            return false;
        }

        private bool TryCapturingRightEmphasisToken(int ch, StringBuilder currentToken)
        {
            if (IsRightEmphasisDelimiter(ch))
            {
                MarkDownEmphasisHtmlGenerator codeGen = IsLeftEmphasisDelimiter(ch)
                    ? new MarkDownLeftAndRightEmphasisHtmlGenerator(currentToken.ToString(), m_delimiterCnts, m_currentDelimiterType)
                    : new MarkDownRightEmphasisHtmlGenerator(currentToken.ToString(), m_delimiterCnts, m_currentDelimiterType);

                m_parsedResult.AppendToLookUpTable(codeGen);
                m_parsedResult.AppendToTokens(codeGen);
                currentToken.Clear();
                return true;
            }

            return false;
        }

        private bool TryCapturingLeftEmphasisToken(int ch, StringBuilder currentToken)
        {
            if (IsLeftEmphasisDelimiter(ch))
            {
                var codeGen = new MarkDownLeftEmphasisHtmlGenerator(currentToken.ToString(), m_delimiterCnts, m_currentDelimiterType);
                m_parsedResult.AppendToLookUpTable(codeGen);
                m_parsedResult.AppendToTokens(codeGen);
                currentToken.Clear();
                return true;
            }

            return false;
        }

        private void UpdateLookBehind(int ch)
        {
            if (IsAlnum(ch))
            {
                m_lookBehind = DelimiterType.Alphanumeric;
            }
            else if (IsSpace(ch))
            {
                m_lookBehind = DelimiterType.WhiteSpace;
            }
            else if (IsPunct(ch))
            {
                m_lookBehind = ch == '\\' ? DelimiterType.Escape : DelimiterType.Puntuation;
            }
        }

        private void CaptureEmphasisToken(int ch, StringBuilder currentToken)
        {
            if (!TryCapturingRightEmphasisToken(ch, currentToken) && !TryCapturingLeftEmphasisToken(ch, currentToken) &&
                currentToken.Length > 0)
            {
                CaptureCurrentCollectedStringAsRegularToken(currentToken);
            }
        }
    }

    internal sealed class LinkParser : MarkDownBlockParser
    {
        private int m_leftParenthesisCounts;
        private long m_positionOfLinkDestinationEndToken;
        private readonly MarkDownParsedResult m_linkTextParsedResult = new();

        public override void Match(MarkDownStream stream)
        {
            bool capturedLink = MatchAtLinkInit(stream) && MatchAtLinkTextRun(stream) && MatchAtLinkTextEnd(stream);
            if (capturedLink && MatchAtLinkDestinationStart(stream) && MatchAtLinkDestinationRun(stream))
            {
                CaptureLinkToken();
            }
        }

        private bool MatchAtLinkInit(MarkDownStream lookahead)
        {
            if (lookahead.Peek() == '[')
            {
                m_linkTextParsedResult.AddNewTokenToParsedResult(lookahead.GetChar());
                return true;
            }

            return false;
        }

        private bool MatchAtLinkTextRun(MarkDownStream lookahead)
        {
            if (lookahead.Peek() == ']')
            {
                m_linkTextParsedResult.AddNewTokenToParsedResult(lookahead.GetChar());
                return true;
            }

            while (lookahead.Peek() != EOF && lookahead.Peek() != ']')
            {
                ParseBlock(lookahead);
                m_linkTextParsedResult.AppendParseResult(GetParsedResult());

                if (m_linkTextParsedResult.GetIsCaptured())
                {
                    break;
                }
            }

            if (lookahead.Peek() == ']')
            {
                m_linkTextParsedResult.AddNewTokenToParsedResult(lookahead.GetChar());
                return true;
            }

            m_parsedResult.AppendParseResult(m_linkTextParsedResult);
            return false;
        }

        private bool MatchAtLinkTextEnd(MarkDownStream lookahead)
        {
            if (lookahead.Peek() == '(')
            {
                ++m_leftParenthesisCounts;
                m_linkTextParsedResult.AddNewTokenToParsedResult(lookahead.GetChar());
                return true;
            }

            m_parsedResult.AppendParseResult(m_linkTextParsedResult);
            return false;
        }

        private bool MatchAtLinkDestinationStart(MarkDownStream lookahead)
        {
            if (lookahead.Peek() < 0)
            {
                m_parsedResult.AppendParseResult(m_linkTextParsedResult);
                return false;
            }

            long initialPosition = lookahead.TellG();
            long currentPosition = initialPosition;
            m_positionOfLinkDestinationEndToken = 0;

            while (lookahead.Peek() != EOF && m_leftParenthesisCounts > 0)
            {
                char token = lookahead.GetChar();

                if (token == '(')
                {
                    m_leftParenthesisCounts++;
                }
                else if (token == ')')
                {
                    m_leftParenthesisCounts--;
                }

                if (m_leftParenthesisCounts == 0)
                {
                    m_positionOfLinkDestinationEndToken = currentPosition;
                }

                currentPosition++;
            }

            lookahead.Clear();
            lookahead.SeekG(initialPosition);

            if (m_positionOfLinkDestinationEndToken == 0 || IsCntrl(lookahead.Peek()))
            {
                m_parsedResult.AppendParseResult(m_linkTextParsedResult);
                return false;
            }

            return true;
        }

        private bool MatchAtLinkDestinationRun(MarkDownStream lookahead)
        {
            if (lookahead.Peek() > 0 && (IsSpace(lookahead.Peek()) || IsCntrl(lookahead.Peek())))
            {
                m_parsedResult.AppendParseResult(m_linkTextParsedResult);
                return false;
            }

            long currentPos = lookahead.TellG();
            while (currentPos < m_positionOfLinkDestinationEndToken && lookahead.Peek() != EOF)
            {
                if (lookahead.Peek() == '[')
                {
                    m_parsedResult.AddNewTokenToParsedResult(lookahead.GetChar());
                }
                else
                {
                    ParseBlock(lookahead);
                }

                currentPos = lookahead.TellG();
            }

            if (lookahead.Peek() == ')')
            {
                lookahead.Get();
            }

            return true;
        }

        private void CaptureLinkToken()
        {
            var html = new StringBuilder();
            html.Append("<a href=\"");
            html.Append(m_parsedResult.GenerateHtmlString());
            html.Append("\">");

            m_linkTextParsedResult.PopFront();
            m_linkTextParsedResult.PopBack();
            m_linkTextParsedResult.PopBack();

            m_linkTextParsedResult.Translate();
            html.Append(m_linkTextParsedResult.GenerateHtmlString());
            html.Append("</a>");

            m_parsedResult.Clear();
            m_parsedResult.FoundHtmlTags();
            m_parsedResult.AppendToTokens(new MarkDownStringHtmlGenerator(html.ToString()));
            m_parsedResult.SetIsCaptured(true);
        }
    }

    internal class ListParser : MarkDownBlockParser
    {
        protected static bool IsHyphen(int ch) => ch == '-';

        protected static bool IsPlus(int ch) => ch == '+';

        protected static bool IsAsterisk(int ch) => ch == '*';

        protected static bool IsDot(int ch) => ch == '.';

        protected static bool IsNewLine(int ch) => ch == '\r' || ch == '\n';

        protected bool MatchNewListItem(MarkDownStream stream)
        {
            int ch = stream.Peek();
            if (IsHyphen(ch) || IsPlus(ch) || IsAsterisk(ch))
            {
                stream.Get();
                if (stream.Peek() == ' ')
                {
                    stream.Unget();
                    return true;
                }

                stream.Unget();
            }

            return false;
        }

        protected bool MatchNewBlock(MarkDownStream stream)
        {
            if (IsNewLine(stream.Peek()))
            {
                do
                {
                    stream.Get();
                }
                while (IsNewLine(stream.Peek()));

                return true;
            }

            return false;
        }

        protected bool MatchNewOrderedListItem(MarkDownStream stream, StringBuilder numberString)
        {
            do
            {
                numberString.Append(stream.GetChar());
            }
            while (IsDigit(stream.Peek()));

            if (IsDot(stream.Peek()))
            {
                stream.Unget();
                return true;
            }

            return false;
        }

        protected void ParseSubBlocks(MarkDownStream stream)
        {
            while (!stream.Eof())
            {
                if (IsNewLine(stream.Peek()))
                {
                    char newLineChar = stream.GetChar();

                    if (IsDigit(stream.Peek()))
                    {
                        var numberString = new StringBuilder();
                        if (MatchNewOrderedListItem(stream, numberString))
                        {
                            break;
                        }
                        else
                        {
                            m_parsedResult.AddNewTokenToParsedResult(numberString.ToString());
                        }
                    }
                    else if (MatchNewListItem(stream) || MatchNewBlock(stream))
                    {
                        break;
                    }

                    m_parsedResult.AddNewTokenToParsedResult(newLineChar);
                }

                ParseBlock(stream);
            }
        }

        protected bool CompleteListParsing(MarkDownStream stream)
        {
            if (stream.Peek() == ' ')
            {
                do
                {
                    stream.Get();
                }
                while (stream.Peek() == ' ');

                ParseBlock(stream);
                ParseSubBlocks(stream);
                return true;
            }

            return false;
        }

        public override void Match(MarkDownStream stream)
        {
            int ch = stream.Peek();
            if (IsHyphen(ch) || IsPlus(ch) || IsAsterisk(ch))
            {
                stream.Get();
                if (CompleteListParsing(stream))
                {
                    CaptureListToken();
                }
                else
                {
                    if (IsAsterisk(ch))
                    {
                        stream.Putback((char)ch);
                        ParseTextAndEmphasis(stream);
                    }
                    else
                    {
                        m_parsedResult.AddNewTokenToParsedResult(ch);
                    }
                }
            }
        }

        private void CaptureListToken()
        {
            m_parsedResult.Translate();
            var htmlString = m_parsedResult.GenerateHtmlString();

            m_parsedResult.Clear();
            m_parsedResult.FoundHtmlTags();
            m_parsedResult.AppendToTokens(new MarkDownListHtmlGenerator("<li>" + htmlString + "</li>"));
        }
    }

    internal sealed class OrderedListParser : ListParser
    {
        public override void Match(MarkDownStream stream)
        {
            var numberString = new StringBuilder();
            if (IsDigit(stream.Peek()))
            {
                do
                {
                    numberString.Append(stream.GetChar());
                }
                while (IsDigit(stream.Peek()));

                if (IsDot(stream.Peek()))
                {
                    stream.Get();
                    if (CompleteListParsing(stream))
                    {
                        CaptureOrderedListToken(numberString.ToString());
                    }
                    else
                    {
                        numberString.Append('.');
                        m_parsedResult.AddNewTokenToParsedResult(numberString.ToString());
                    }
                }
                else
                {
                    m_parsedResult.AddNewTokenToParsedResult(numberString.ToString());
                }
            }
        }

        private void CaptureOrderedListToken(string numberString)
        {
            m_parsedResult.Translate();
            var htmlString = m_parsedResult.GenerateHtmlString();

            m_parsedResult.Clear();
            m_parsedResult.FoundHtmlTags();
            m_parsedResult.AppendToTokens(new MarkDownOrderedListHtmlGenerator("<li>" + htmlString + "</li>", numberString));
        }
    }
}
