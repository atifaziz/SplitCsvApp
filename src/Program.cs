#region License and Terms
//
// splitcsv - CSV Splitter Utility
// Copyright (c) 2012 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace SplitCsvApp
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Dsv;
    using Mono.Options;
    using static MoreLinq.Extensions.PairwiseExtension;
    using static MoreLinq.Extensions.IndexExtension;

    #endregion

    static class Program
    {
        static bool _verbose;

        static int Main(string[] args)
        {
            try
            {
                Run(args);
                return Environment.ExitCode;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(_verbose ? e.ToString() : e.GetBaseException().Message);

                return Environment.ExitCode != 0
                     ? Environment.ExitCode : 0xbad;
            }
        }

        static class Defaults
        {
            public const int LinesPerGroup = 10000;
        }

        static void Run(IEnumerable<string> args)
        {
            Debug.Assert(args != null);

            var help = false;
            var debug = false;
            var encoding = Encoding.Default;
            var linesPerGroup = (int?)null;
            var outputDirectoryPath = (string)null;
            var emitAbsolutePaths = false;

            var options = new OptionSet
            {
                { "?|help|h"         , "prints out the options", _ => help = true },
                { "verbose|v"        , "enable additional output", _ => _verbose = true },
                { "d|debug"          , "debug break", _ => debug = true },
                { "e|encoding="      , "input/output file encoding", v => encoding = Encoding.GetEncoding(v) },
                { "l|lines="         , $"lines per split ({Defaults.LinesPerGroup:N0})", v => linesPerGroup = int.Parse(v, NumberStyles.None | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite) },
                { "od|output-dir="   , "output directory (default is same as source)", v => outputDirectoryPath = v.Trim() },
                { "ap|absolute-paths", "emit absolute paths to split files", v => emitAbsolutePaths = true },
            };

            var tail = options.Parse(args);

            if (debug)
                Debugger.Break();

            if (help)
            {
                foreach (var line in About().Concat(new[] { null, "options:", null }))
                    Console.WriteLine(line);
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var paths = from arg in tail
                        select arg.Trim() into arg
                        where arg.Length > 0
                        select arg;

            paths = paths.ToArray();

            if (!paths.Any())
                throw new Exception("Missing at least one file specification.");

            static void LogSkipWarning(string path) =>
                Console.Error.WriteLine($"Skipping empty file: {path}");

            Split(Math.Max(1, linesPerGroup ?? Defaults.LinesPerGroup));

            void Split(int linesPerGroup)
            {
                foreach (var path in paths)
                {
                    if (new FileInfo(path).Length == 0)
                    {
                        LogSkipWarning(path);
                        continue;
                    }

                    var lines = File.ReadLines(path, encoding);
                    var header = lines.ParseCsv().FirstOrDefault();

                    if (header.LineNumber == 0)
                    {
                        LogSkipWarning(path);
                        continue;
                    }

                    var rows =
                        from e in lines.ParseCsv(hr => hr).Index()
                        select (Group: 1 + e.Key / linesPerGroup, Fields: e.Value.Row);

                    if (!rows.SkipWhile(e => e.Group == 1).Take(1).Any())
                        continue;

                    var writer = TextWriter.Null;

                    try
                    {
                        foreach (var pair in rows.Prepend((0, default)).Pairwise(Tuple.Create))
                        {
                            var ((prevGroup, _), (group, row)) = pair;

                            if (group != prevGroup)
                            {
                                writer.Close();

                                var filename = FormattableString.Invariant($@"{Path.GetFileNameWithoutExtension(path)}-{group}{Path.GetExtension(path)}");
                                var dir = string.IsNullOrEmpty(outputDirectoryPath)
                                        ? Path.GetDirectoryName(path)
                                        : outputDirectoryPath;
                                var outputFilePath = Path.Combine(dir, filename);

                                writer = new StreamWriter(outputFilePath, false, encoding);
                                Console.WriteLine(emitAbsolutePaths ? Path.GetFullPath(outputFilePath) : outputFilePath);
                                header.WriteCsv(writer);
                            }

                            if (row.Count != header.Count)
                                throw new Exception($"File \"{path}\" has an uneven row on line {row.LineNumber}; expected {header.Count} fields, got {row.Count} instead.");

                            row.WriteCsv(writer);
                        }
                    }
                    finally
                    {
                        writer.Close();
                    }
                }
            }
        }

        static void WriteCsv(this TextRow row, TextWriter writer)
        {
            var i = 0;
            foreach (var field in row)
            {
                if (i++ > 0)
                    writer.Write(',');

                writer.Write('"');
                writer.Write(field.IndexOf('"') >= 0
                              ? field.Replace("\"", "\"\"")
                              : field);
                writer.Write('"');
            }

            writer.WriteLine();
        }

        static readonly Uri HomeUrl = new Uri("https://github.com/atifaziz/SplitCsvApp");

        static IEnumerable<string> About()
        {
            var verinfo = GetVersionInfo();
            return new[]
            {
                $"{verinfo.ProductName} (version {verinfo.FileVersion})",
                HomeUrl.OriginalString,
                null,
                verinfo.LegalCopyright,
                "Portions:",
                "  - Copyright (c) 2008 Novell (http://www.novell.com)",
                "  - Copyright (c) 2009 Federico Di Gregorio",
                "  - Copyright (c) 2008 Jonathan Skeet",
                null,
                "This is free software; see the source for copying conditions. There is NO",
                "warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.",
            };
        }

        static FileVersionInfo GetVersionInfo()
        {
            var assemblyPath = new Uri(typeof (Program).Assembly.CodeBase).LocalPath;
            return FileVersionInfo.GetVersionInfo(assemblyPath);
        }
    }
}
