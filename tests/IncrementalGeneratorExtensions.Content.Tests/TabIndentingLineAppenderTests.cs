using System;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class TabIndentingLineAppenderTests
    {
        private static readonly string NL = Environment.NewLine;

        [Fact]
        public void StringForIndent_UsesTabsNotSpaces()
        {
            // Arrange
            var appender = new TabIndentingLineAppender();

            // Act
            var oneLevel = appender.StringForIndent(1);
            var twoLevels = appender.StringForIndent(2);

            // Assert
            Assert.Equal("\t", oneLevel);
            Assert.Equal("\t\t", twoLevels);
        }

        [Fact]
        public void AppendStartBlock_IndentsInnerContentWithSingleTab()
        {
            // Arrange
            var appender = new TabIndentingLineAppender();

            // Act
            appender.AppendStartBlock();
            appender.AppendLine("inner");
            appender.AppendEndBlock();

            // Assert
            var expected = "{" + NL + "\tinner" + NL + "}" + NL;
            Assert.Equal(expected, appender.ToString());
        }
    }
}
