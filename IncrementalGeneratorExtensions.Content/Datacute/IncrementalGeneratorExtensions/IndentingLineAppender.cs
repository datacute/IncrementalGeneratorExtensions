#if !DATACUTE_EXCLUDE_INDENTINGLINEAPPENDER
using System;
using System.Collections.Generic;
using System.IO;
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
        private char _indentationCharacter;
        private int _indentationCharacterRepetition;
        private readonly string _blockStart;
        private readonly string _blockEnd;
        private readonly Dictionary<int, string> _indentationCache = new Dictionary<int, string>();

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
            if (!_indentationCache.TryGetValue(indent, out var indentString))
            {
                indentString = new string(_indentationCharacter, indent * _indentationCharacterRepetition);
                _indentationCache[indent] = indentString;
            }

            return indentString;
        }

        /// <summary>
        /// Clears the internal <see cref="StringBuilder"/> and resets the indentation level.
        /// </summary>
        /// <returns>Returns the current instance for method chaining.</returns>
        public IndentingLineAppender Clear()
        {
            _buffer.Clear();
            _indentLevel = 0;
            _currentIndentString = string.Empty;
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
            _indentLevel++;
            UpdateCurrentIndent();
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
            _indentLevel--;
            UpdateCurrentIndent();
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

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        _buffer.AppendLine();
                    }
                    else
                    {
                        AppendLine(line);
                    }
                }
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
