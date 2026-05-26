#if !DATACUTE_EXCLUDE_INDENTINGLINEAPPENDER
using System;
using System.Text;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Wrap a <see cref="StringBuilder"/> to provide a convenient way to append lines of text with indentation.
    /// </summary>
    public class IndentingLineAppender
    {
        private const char DefaultIndentationCharacter = ' ';
        private const int DefaultIndentationCharacterRepetition = 4;
        private const string DefaultBlockStartCharacter = "{";
        private const string DefaultBlockEndCharacter = "}";

        /// <summary>
        /// The string used for a single indentation level.
        /// </summary>
        public string SingleIndent { get; }

        private int _indentLevel;
        private string _currentIndentString = string.Empty;
        
        private readonly StringBuilder _buffer;
        private readonly char _indentationCharacter;
        private readonly int _indentationCharacterRepetition;
        private readonly string _blockStart;
        private readonly string _blockEnd;
        private string[] _indentationCache = new string[16];

        /// <summary>
        /// Gets the underlying <see cref="StringBuilder"/> used for direct text manipulation.
        /// </summary>
        public StringBuilder Direct => _buffer;

        /// <summary>
        /// Gets or sets the current indentation level.
        /// </summary>
        public int IndentLevel
        {
            get => _indentLevel;
            set
            {
                _indentLevel = value;
                UpdateCurrentIndent();
            }
        }

        private void IncreaseIndent() { IndentLevel = _indentLevel + 1; }
        private void DecreaseIndent() { IndentLevel = _indentLevel - 1; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndentingLineAppender"/> class with a default <see cref="StringBuilder"/>.
        /// </summary>
        public IndentingLineAppender() : this(new StringBuilder())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndentingLineAppender"/> class with specified block start and end characters.
        /// </summary>
        public IndentingLineAppender(string blockStart, string blockEnd)
            : this(new StringBuilder(), blockStart: blockStart, blockEnd: blockEnd)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndentingLineAppender"/> class with specified indentation character and repetition, and optional block start and end characters.
        /// </summary>
        public IndentingLineAppender(char indentationCharacter, int indentationCharacterRepetition)
            : this(new StringBuilder(), indentationCharacter, indentationCharacterRepetition)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndentingLineAppender"/> class with a specified <see cref="StringBuilder"/> and optional block start and end characters.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder to use for appending text.</param>
        /// <param name="indentationCharacter">The character to use for indentation, defaulting to a space (' ').</param>
        /// <param name="indentationCharacterRepetition">The number of times to repeat the indentation character, defaulting to 4.</param>
        /// <param name="blockStart">The string to use for the start of a block, defaulting to "{".</param>
        /// <param name="blockEnd">The string to use for the end of a block, defaulting to "}".</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided StringBuilder is null.</exception>
        public IndentingLineAppender(
            StringBuilder stringBuilder, 
            char indentationCharacter = DefaultIndentationCharacter,
            int indentationCharacterRepetition = DefaultIndentationCharacterRepetition,
            string blockStart = DefaultBlockStartCharacter, 
            string blockEnd = DefaultBlockEndCharacter)
        {
            _buffer = stringBuilder ?? throw new ArgumentNullException(nameof(stringBuilder));
            _indentationCharacter = indentationCharacter;
            _indentationCharacterRepetition = indentationCharacterRepetition;
            _blockStart = blockStart;
            _blockEnd = blockEnd;

            SingleIndent = new string(_indentationCharacter, _indentationCharacterRepetition);
            _indentationCache[0] = string.Empty;
            _indentationCache[1] = SingleIndent;
        }

        /// <summary>
        /// Returns the current indentation string.
        /// </summary>
        /// <returns>A string containing the current indentation characters.</returns>
        public string CurrentIndent => _currentIndentString;

        private void UpdateCurrentIndent() => _currentIndentString = StringForIndent(_indentLevel);

        /// <summary>
        /// Returns a string representing the indentation for the specified level.
        /// </summary>
        /// <param name="indent">The indentation level.</param>
        /// <returns>A string containing the appropriate number of indentation characters.</returns>
        public string StringForIndent(int indent)
        {
            if (indent < 0) return string.Empty;
            
            if (indent < _indentationCache.Length)
            {
                var indentString = _indentationCache[indent];
                if (indentString != null)
                {
                    return indentString;
                }
            }

            var newIndentString = new string(_indentationCharacter, indent * _indentationCharacterRepetition);
            
            if (indent < _indentationCache.Length)
            {
                _indentationCache[indent] = newIndentString;
            }
            else if (indent < 64) // Allow cache to grow up to a reasonable limit
            {
                var newCache = new string[indent + 1];
                Array.Copy(_indentationCache, newCache, _indentationCache.Length);
                _indentationCache = newCache;
                _indentationCache[indent] = newIndentString;
            }

            return newIndentString;
        }

        /// <summary>
        /// Clears the internal <see cref="StringBuilder"/> and resets the indentation level.
        /// </summary>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender Clear()
        {
            _buffer.Clear();
            IndentLevel = 0;
            return this;
        }

        /// <summary>
        /// Appends the current indentation to the internal <see cref="StringBuilder"/> (no new line).
        /// </summary>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendIndent()
        {
            _buffer.Append(_currentIndentString);
            return this;
        }

        /// <summary>
        /// Appends a new line to the internal <see cref="StringBuilder"/>.
        /// </summary>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendLine()
        {
            _buffer.AppendLine();
            return this;
        }

        /// <summary>
        /// Appends a line of text to the internal <see cref="StringBuilder"/> with the current indentation.
        /// </summary>
        /// <param name="line">The line of text to append.</param>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendLine(string line)
        {
            _buffer.Append(_currentIndentString).AppendLine(line);
            return this;
        }

        /// <summary>
        /// Appends a text to the internal <see cref="StringBuilder"/> without adding any indentation.
        /// </summary>
        /// <param name="text">The text to append.</param>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender Append(string text)
        {
            _buffer.Append(text);
            return this;
        }

        /// <summary>
        /// Appends a character to the internal <see cref="StringBuilder"/> without adding any indentation.
        /// </summary>
        /// <param name="c">The character to append.</param>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender Append(char c)
        {
            _buffer.Append(c);
            return this;
        }

        /// <summary>
        /// Appends the block start line with the current indentation and increases the indentation level.
        /// </summary>
        /// <example>
        /// <code>
        /// // Previous content before calling AppendStartBlock()
        /// // Calling AppendStartBlock() adds the { character on the next line:
        /// {
        ///     // Content added after calling AppendStartBlock()
        /// </code>
        /// </example>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendStartBlock()
        {
            AppendLine(_blockStart);
            IncreaseIndent();
            return this;
        }

        /// <summary>
        /// Decreases the indentation level and then appends the block end line using the new indentation.
        /// </summary>
        /// <example>
        /// <code>
        ///     // Previous content before calling AppendEndBlock()
        ///     // Calling AppendEndBlock() adds the } character on the next line:
        /// }
        /// // Content added after calling AppendEndBlock()
        /// </code>
        /// </example>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendEndBlock()
        {
            DecreaseIndent();
            AppendLine(_blockEnd);
            return this;
        }

        /// <summary>
        /// Calls ToString on the internal <see cref="StringBuilder"/> to get the current content.
        /// </summary>
        /// <returns>A string containing the current content of the <see cref="StringBuilder"/>.</returns>
        public override string ToString() => _buffer.ToString();

        /// <summary>
        /// Appends multi-line content, with each line being correctly indented.
        /// </summary>
        /// <param name="content">The multi-line content to append.</param>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendLines(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return this;
            }

            int length = content.Length;
            int startIndex = 0;

            while (startIndex < length)
            {
                int newlineIndex = content.IndexOf('\n', startIndex);
                if (newlineIndex == -1)
                {
                    newlineIndex = length;
                }

                int endIndex = newlineIndex;
                if (endIndex > startIndex && content[endIndex - 1] == '\r')
                {
                    endIndex--;
                }

                int lineLength = endIndex - startIndex;
                if (lineLength == 0)
                {
                    _buffer.AppendLine();
                }
                else
                {
                    _buffer.Append(_currentIndentString).Append(content, startIndex, lineLength).AppendLine();
                }

                startIndex = newlineIndex + 1;
            }

            return this;
        }

        /// <summary>
        /// Formats a string and then appends the result line by line. All lines in the
        /// final string, including those originating from multi-line arguments,
        /// will be correctly indented.
        /// </summary>
        /// <param name="format">The format string to use.</param>
        /// <param name="args">The arguments to format the string with.</param>
        /// <remarks>
        /// To indent multi-line arguments, the result of calling
        /// <see cref="string.Format(string, object[])"/>
        /// is passed to <see cref="AppendLines(string)"/>.
        /// </remarks>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender AppendFormatLines(string format, params object[] args) => AppendLines(string.Format(format, args));
    }
}
#endif
