using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Benchmarks for <see cref="IndentingLineAppender"/> and <see cref="TabIndentingLineAppender"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>
    ///     <term>AppendLine</term>
    ///     <description>Single line at a fixed indent level, measured at depth 0 and depth 5 to show
    ///     the cached indentation string look-up cost.</description>
    ///   </item>
    ///   <item>
    ///     <term>FlatFile</term>
    ///     <description>50 lines with no indentation — representative of top-level using / namespace output.</description>
    ///   </item>
    ///   <item>
    ///     <term>NestedBlocks</term>
    ///     <description>Simulates a realistic generator output: namespace → class → three methods, each with
    ///     five body lines.  Exercises <see cref="IndentingLineAppender.AppendStartBlock"/> /
    ///     <see cref="IndentingLineAppender.AppendEndBlock"/> and the indentation cache at four levels.</description>
    ///   </item>
    ///   <item>
    ///     <term>AppendLines</term>
    ///     <description>Multi-line string passed to <see cref="IndentingLineAppender.AppendLines"/>; each line
    ///     is split via <see cref="System.IO.StringReader"/> and re-indented.</description>
    ///   </item>
    /// </list>
    /// All benchmarks reuse a single <see cref="StringBuilder"/> that is cleared before each
    /// invocation so allocations reported by BenchmarkDotNet reflect only the appender logic,
    /// not initial <see cref="StringBuilder"/> construction.
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("IndentingLineAppender")]
    [HideColumns("Runtime")]
    [SimpleJob(RuntimeMoniker.Net481, id: "net481")]
    [SimpleJob(RuntimeMoniker.Net10_0, id: "net10")]
    public class IndentingLineAppenderBenchmarks
    {
        private IndentingLineAppender _appender = null!;
        private string _multiLineContent = null!;
        private string[] _flatFileLines = null!;
        private string[] _methodLines = null!;
        private string[] _varLines = null!;

        [GlobalSetup]
        public void Setup()
        {
            _appender = new IndentingLineAppender(new StringBuilder(4096));

            // A representative 10-line block used by AppendLines benchmarks.
            var sb = new StringBuilder();
            for (int i = 0; i < 10; i++)
                sb.AppendLine($"    var x{i} = value + {i};");
            _multiLineContent = sb.ToString();

            _flatFileLines = new string[50];
            for (int i = 0; i < 50; i++)
                _flatFileLines[i] = $"// line {i}";

            _methodLines = new string[3];
            for (int i = 0; i < 3; i++)
                _methodLines[i] = $"public void Method{i}()";

            _varLines = new string[5];
            for (int i = 0; i < 5; i++)
                _varLines[i] = $"var x{i} = {i};";
        }

        // ------------------------------------------------------------------ //
        //  Single line                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>Append one line at indentation level 0.</summary>
        [Benchmark]
        public IndentingLineAppender AppendLine_NoIndent()
        {
            _appender.Clear();
            return _appender.AppendLine("using System;");
        }

        /// <summary>Append one line at indentation level 5 — the indentation string is cached after the first call.</summary>
        [Benchmark]
        public IndentingLineAppender AppendLine_IndentLevel5()
        {
            _appender.Clear();
            _appender.IndentLevel = 5;
            return _appender.AppendLine("return value;");
        }

        // ------------------------------------------------------------------ //
        //  Flat file (many lines, no indentation)                             //
        // ------------------------------------------------------------------ //

        /// <summary>Append 50 lines at indent level 0.</summary>
        [Benchmark]
        public string FlatFile_50Lines()
        {
            _appender.Clear();
            for (int i = 0; i < 50; i++)
            {
                _appender.AppendLine(_flatFileLines[i]);
            }
            return _appender.ToString();
        }

        // ------------------------------------------------------------------ //
        //  Nested blocks (realistic generator output)                         //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Simulates generating a file with namespace → class → three methods, each with five body lines.
        /// Tests <see cref="IndentingLineAppender.AppendStartBlock"/> and
        /// <see cref="IndentingLineAppender.AppendEndBlock"/> round-trips across four indent levels.
        /// </summary>
        [Benchmark]
        public string NestedBlocks_ThreeMethods()
        {
            _appender.Clear();
            _appender.AppendLine("namespace Generated");
            _appender.AppendStartBlock();
            _appender.AppendLine("public partial class MyClass");
            _appender.AppendStartBlock();

            for (int m = 0; m < 3; m++)
            {
                _appender.AppendLine(_methodLines[m]);

                _appender.AppendStartBlock();
                for (int i = 0; i < 5; i++)
                {
                    _appender.AppendLine(_varLines[i]);
                }
                _appender.AppendEndBlock();
            }

            _appender.AppendEndBlock(); // class
            _appender.AppendEndBlock(); // namespace
            return _appender.ToString();
        }

        // ------------------------------------------------------------------ //
        //  AppendLines (multi-line string split + re-indent)                  //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Passes a pre-built 10-line string to <see cref="IndentingLineAppender.AppendLines"/>
        /// at indent level 2, exercising the internal <see cref="System.IO.StringReader"/> split.
        /// </summary>
        [Benchmark]
        public IndentingLineAppender AppendLines_10Lines_IndentLevel2()
        {
            _appender.Clear();
            _appender.IndentLevel = 2;
            return _appender.AppendLines(_multiLineContent);
        }

        // ------------------------------------------------------------------ //
        //  Complete Source File Simulation                                    //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Simulates producing a standard C# source file matching <c>SourceTextGeneratorBase.AppendSource</c>.
        /// Includes auto-generated comment, nullable directive, namespace, containing type, type declaration, and methods.
        /// </summary>
        [Benchmark]
        public string TypicalSourceFile()
        {
            _appender.Clear();

            // AppendAutoGeneratedComment
            _appender.AppendLines(@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Datacute.IncrementalGeneratorExtensions.
//     Version: 1.0.0
// </auto-generated>
//------------------------------------------------------------------------------");
            _appender.AppendLine();

            // AppendNullableEnable
            _appender.AppendLine("#nullable enable");
            _appender.AppendLine();

            // AppendStartNamespace
            _appender.Append("namespace ").AppendLine("GeneratedNamespace");
            _appender.AppendStartBlock();

            // AppendContainingTypes
            _appender.AppendLine("public partial class ParentClass");
            _appender.AppendStartBlock();

            // AppendTypeDeclaration
            _appender.AppendLine("public partial class GeneratedClass");
            _appender.AppendStartBlock();

            // AppendCustomMembers
            for (int m = 0; m < 3; m++)
            {
                _appender.AppendLine(_methodLines[m]);

                _appender.AppendStartBlock();
                for (int i = 0; i < 5; i++)
                {
                    _appender.AppendLine(_varLines[i]);
                }
                _appender.AppendEndBlock();
            }

            // AppendEndBlock
            _appender.AppendEndBlock();

            // AppendContainingTypesEndBlock
            _appender.AppendEndBlock();

            // AppendEndNamespace
            _appender.AppendEndBlock();

            return _appender.ToString();
        }

        // ------------------------------------------------------------------ //
        //  Tab variant                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>Same nested-blocks scenario using <see cref="TabIndentingLineAppender"/>.</summary>
        [Benchmark]
        public string NestedBlocks_TabIndent_ThreeMethods()
        {
            var tab = new TabIndentingLineAppender(new StringBuilder(2048));
            tab.AppendLine("namespace Generated");
            tab.AppendStartBlock();
            tab.AppendLine("public partial class MyClass");
            tab.AppendStartBlock();

            for (int m = 0; m < 3; m++)
            {
                tab.AppendLine(_methodLines[m]);

                tab.AppendStartBlock();
                for (int i = 0; i < 5; i++)
                {
                    tab.AppendLine(_varLines[i]);
                }
                tab.AppendEndBlock();
            }

            tab.AppendEndBlock();
            tab.AppendEndBlock();
            return tab.ToString();
        }
    }
}

/*
BenchmarkDotNet v0.15.1, Windows 11 (10.0.26200.8457)
12th Gen Intel Core i7-12700H 2.30GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.300
  [Host] : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2
  net10  : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2
  net481 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256

| Method                              | Job    | Mean         | Error      | StdDev     | Gen0   | Gen1   | Allocated |
|------------------------------------ |------- |-------------:|-----------:|-----------:|-------:|-------:|----------:|
| AppendLine_IndentLevel5             | net10  |     6.967 ns |  0.1807 ns |  0.1856 ns |      - |      - |         - |
| AppendLine_IndentLevel5             | net481 |    35.642 ns |  0.1936 ns |  0.1716 ns |      - |      - |         - |
| AppendLine_NoIndent                 | net10  |     4.211 ns |  0.1243 ns |  0.1163 ns |      - |      - |         - |
| AppendLine_NoIndent                 | net481 |    31.997 ns |  0.4872 ns |  0.4319 ns |      - |      - |         - |
| AppendLines_10Lines_IndentLevel2    | net10  |    80.139 ns |  1.5635 ns |  1.5356 ns |      - |      - |         - |
| AppendLines_10Lines_IndentLevel2    | net481 |   340.915 ns |  6.8392 ns |  8.1415 ns |      - |      - |         - |
| FlatFile_50Lines                    | net10  |   184.883 ns |  3.0740 ns |  2.7251 ns | 0.0961 |      - |    1208 B |
| FlatFile_50Lines                    | net481 | 1,531.134 ns | 23.2261 ns | 30.2005 ns | 0.1907 |      - |    1212 B |
| NestedBlocks_TabIndent_ThreeMethods | net10  |   269.311 ns |  4.5710 ns |  5.6136 ns | 0.4187 | 0.0062 |    5256 B |
| NestedBlocks_TabIndent_ThreeMethods | net481 | 1,116.606 ns |  9.7044 ns |  8.1036 ns | 0.8507 |      - |    5360 B |
| NestedBlocks_ThreeMethods           | net10  |   200.530 ns |  3.9825 ns |  5.5829 ns | 0.0987 |      - |    1240 B |
| NestedBlocks_ThreeMethods           | net481 | 1,007.518 ns | 18.6248 ns | 28.4420 ns | 0.1984 |      - |    1252 B |
| TypicalSourceFile                   | net10  |   267.524 ns |  5.2597 ns |  6.6518 ns | 0.1788 |      - |    2248 B |
| TypicalSourceFile                   | net481 | 1,506.055 ns | 17.4013 ns | 14.5308 ns | 0.3586 |      - |    2263 B |

*/
