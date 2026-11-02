// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Minimal emulation of the std::stringstream semantics (eof/fail/bad bits, peek/get/unget/putback/tellg/seekg)
    /// relied upon by the shared C++ markdown parser. Operates on UTF-16 code units instead of UTF-8 bytes.
    /// </summary>
    internal sealed class MarkDownStream
    {
        public const int EOF = -1;

        private readonly string _text;
        private int _position;
        private bool _eofBit;
        private bool _failBit;
        private bool _badBit;

        public MarkDownStream(string text)
        {
            _text = text ?? string.Empty;
        }

        private bool Good => !_eofBit && !_failBit && !_badBit;

        public bool Eof() => _eofBit;

        // istream::sentry -- fails (and sets failbit) when the stream is not good
        private bool Sentry()
        {
            if (Good)
            {
                return true;
            }

            _failBit = true;
            return false;
        }

        public int Peek()
        {
            if (!Sentry())
            {
                return EOF;
            }

            if (_position >= _text.Length)
            {
                _eofBit = true;
                return EOF;
            }

            return _text[_position];
        }

        public int Get()
        {
            if (!Sentry())
            {
                return EOF;
            }

            if (_position >= _text.Length)
            {
                _eofBit = true;
                _failBit = true;
                return EOF;
            }

            return _text[_position++];
        }

        /// <summary>istream::get(char&amp;): on failure the out char keeps its value-initialized value ('\0').</summary>
        public char GetChar()
        {
            var ch = Get();
            return ch == EOF ? '\0' : (char)ch;
        }

        public void Unget()
        {
            // C++11: unget first clears eofbit
            _eofBit = false;
            if (!Sentry())
            {
                return;
            }

            if (_position == 0)
            {
                _badBit = true;
            }
            else
            {
                _position--;
            }
        }

        public void Putback(char ch) => Unget();

        public long TellG()
        {
            if (!Sentry())
            {
                return -1;
            }

            return _position;
        }

        public void SeekG(long position)
        {
            // C++11: seekg first clears eofbit
            _eofBit = false;
            if (!Sentry())
            {
                return;
            }

            _position = (int)position;
        }

        public void Clear()
        {
            _eofBit = false;
            _failBit = false;
            _badBit = false;
        }
    }
}
