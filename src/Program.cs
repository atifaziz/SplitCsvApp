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
        static int Main(string[] args)
        {
            TextWriter log = null;

            try
            {
                Run(args, ref log);
                return Environment.ExitCode;
            }
            catch (Exception e)
            {
                if (log != null)
                    log.WriteLine(e.ToString());
                else
                   Console.Error.WriteLine(e.GetBaseException().Message);

                return Environment.ExitCode != 0
                     ? Environment.ExitCode : 0xbad;
            }
        }

        static class Defaults
        {
            public const int LinesPerGroup = 10000;
        }

        static void Run(IEnumerable<string> args, ref TextWriter log)
        {
            Debug.Assert(args != null);

            var help = false;
            var verbose = false;
            var debug = false;
            var encoding = Encoding.Default;
            var linesPerGroup = (int?)null;
            var outputDirectoryPath = (string)null;
            var emitAbsolutePaths = false;
            var isDryRun = false;

            var options = new OptionSet
            {
                { "?|help|h"         , "prints out the options", _ => help = true },
                { "verbose|v"        , "enable additional output", _ => verbose = true },
                { "d|debug"          , "debug break", _ => debug = true },
                { "e|encoding="      , "input/output file encoding", v => encoding = Encoding.GetEncoding(v) },
                { "l|lines="         , $"lines per split ({Defaults.LinesPerGroup:N0})", v => linesPerGroup = int.Parse(v, NumberStyles.None | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite) },
                { "od|output-dir="   , "output directory (default is same as source)", v => outputDirectoryPath = v.Trim() },
                { "ap|absolute-paths", "emit absolute paths to split files", v => emitAbsolutePaths = true },
                { "dry-run"          , "pretend to split", v => isDryRun = true },
            };

            var tail = options.Parse(args);

            if (debug)
                Debugger.Break();

            if (verbose)
                log = Console.Error;

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

            Split(Math.Max(1, linesPerGroup ?? Defaults.LinesPerGroup), log);

            void Split(int linesPerGroup, TextWriter log)
            {
                foreach (var path in paths)
                {
                    log?.WriteLine("Processing: " + path);

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
                    {
                        log?.WriteLine("...did not need splitting.");
                        continue;
                    }

                    var sw = Stopwatch.StartNew();

                    var writer = TextWriter.Null;
                    var rowCount = 0L;

                    try
                    {
                        foreach (var pair in rows.Prepend((0, default)).Pairwise(Tuple.Create))
                        {
                            rowCount++;
                            var ((prevGroup, _), (group, row)) = pair;

                            if (group != prevGroup)
                            {
                                writer.Close();

                                var filename = FormattableString.Invariant($@"{Path.GetFileNameWithoutExtension(path)}-{group}{Path.GetExtension(path)}");
                                var dir = string.IsNullOrEmpty(outputDirectoryPath)
                                        ? Path.GetDirectoryName(path)
                                        : outputDirectoryPath;
                                var outputFilePath = Path.Combine(dir, filename);

                                if (!isDryRun)
                                    writer = new StreamWriter(outputFilePath, false, encoding);

                                Console.WriteLine(emitAbsolutePaths ? Path.GetFullPath(outputFilePath) : outputFilePath);
                                header.WriteCsv(writer);
                            }

                            if (row.Count != header.Count)
                                throw new Exception($"File \"{path}\" has an uneven row on line {row.LineNumber}; expected {header.Count} fields, got {row.Count} instead.");

                            if (!isDryRun)
                                row.WriteCsv(writer);
                        }
                    }
                    finally
                    {
                        writer.Close();
                    }

                    log?.WriteLine($"...{rowCount:N0} total row(s); time taken = {sw.Elapsed}");
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
