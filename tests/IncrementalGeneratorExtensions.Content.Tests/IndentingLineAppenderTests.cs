using System;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class IndentingLineAppenderTests
    {
        private static readonly string NL = Environment.NewLine;

        [Fact]
        public void AppendStartAndEndBlock_IndentsInnerContentAndUnindentsClose()
        {
            // Arrange
            var appender = new IndentingLineAppender();

            // Act
            appender.AppendLine("before");
            appender.AppendStartBlock();
            appender.AppendLine("inner");
            appender.AppendEndBlock();
            appender.AppendLine("after");

            // Assert
            var expected = "before" + NL + "{" + NL + "    inner" + NL + "}" + NL + "after" + NL;
            Assert.Equal(expected, appender.ToString());
        }

        [Fact]
        public void AppendStartBlock_Nested_AccumulatesIndent()
        {
            // Arrange
            var appender = new IndentingLineAppender();

            // Act
            appender.AppendStartBlock();
            appender.AppendStartBlock();
            appender.AppendLine("deep");
            appender.AppendEndBlock();
            appender.AppendEndBlock();

            // Assert
            var expected = "{" + NL + "    {" + NL + "        deep" + NL + "    }" + NL + "}" + NL;
            Assert.Equal(expected, appender.ToString());
        }

        [Fact]
        public void AppendLines_MultiLineContent_EachLineIndentedAtCurrentLevel()
        {
            // Arrange
            var appender = new IndentingLineAppender();
            appender.AppendStartBlock();

            // Act
            appender.AppendLines("first\nsecond\nthird");

            // Assert
            var expected = "{" + NL +
                           "    first" + NL +
                           "    second" + NL +
                           "    third" + NL;
            Assert.Equal(expected, appender.ToString());
        }

        [Fact]
        public void AppendLines_BlankLine_EmittedWithoutIndent()
        {
            // Arrange
            var appender = new IndentingLineAppender();
            appender.AppendStartBlock();

            // Act
            appender.AppendLines("a\n\nb");

            // Assert — blank lines must NOT be indented (would otherwise show as trailing spaces).
            var expected = "{" + NL +
                           "    a" + NL +
                           NL +
                           "    b" + NL;
            Assert.Equal(expected, appender.ToString());
        }

        [Fact]
        public void AppendFormatLines_MultiLineArgument_IndentsEverySubstitutedLine()
        {
            // Arrange
            var appender = new IndentingLineAppender();
            appender.AppendStartBlock();
            const string multiLineArg = "line1\nline2";

            // Act
            appender.AppendFormatLines("// {0}", multiLineArg);

            // Assert — every line produced by string.Format (including those from the
            // substituted argument) must be re-indented.
            var expected = "{" + NL +
                           "    // line1" + NL +
                           "    line2" + NL;
            Assert.Equal(expected, appender.ToString());
        }

        [Fact]
        public void StringForIndent_RepeatedCalls_ReturnsCachedInstance()
        {
            // Arrange
            var appender = new IndentingLineAppender();

            // Act — level 2 is not pre-seeded; the cache miss-then-hit path is exercised.
            var first = appender.StringForIndent(2);
            var second = appender.StringForIndent(2);

            // Assert
            Assert.Same(first, second);
            Assert.Equal("        ", second);
        }

        [Fact]
        public void Constructor_NullStringBuilder_Throws()
        {
            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => new IndentingLineAppender(null));
        }
    }
}
