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
    using Microsoft.VisualBasic.FileIO;
    using Mono.Options;
    using MoreLinq;

    #endregion

    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Run(args);
                return Environment.ExitCode;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.GetBaseException().Message);
                Trace.TraceError(e.ToString());

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
            var verbose = false;
            var debug = false;
            var encoding = Encoding.Default;
            var linesPerGroup = (int?) null;
            var outputDirectoryPath = (string) null;
            var emitAbsolutePaths = false;

            var options = new OptionSet
            {
                { "?|help|h", "prints out the options", _ => help = true },
                { "verbose|v", "enable additional output", _ => verbose = true },
                { "d|debug", "debug break", _ => debug = true },
                { "e|encoding=", "input/output file encoding", v => encoding = Encoding.GetEncoding(v) },
                { "l|lines=", string.Format("lines per split ({0:N0})", Defaults.LinesPerGroup), v => linesPerGroup = int.Parse(v, NumberStyles.None | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite) },
                { "od|output-dir=", "output directory (default is same as source)", v => outputDirectoryPath = v.Trim() },
                { "ap|absolute-paths", "emit absolute paths to split files", v => emitAbsolutePaths = true },
            };

            var tail = options.Parse(args);

            if (debug)
                Debugger.Break();

            if (verbose)
                Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));

            if (help)
            {
                foreach (var line in About().Concat(new[] { null, "options:", null }))
                    Console.WriteLine(line);
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            linesPerGroup = Math.Max(1, linesPerGroup ?? Defaults.LinesPerGroup);

            var paths = from arg in tail
                        select arg.Trim() into arg
                        where arg.Length > 0
                        select arg;

            paths = paths.ToArray();

            if (!paths.Any())
                throw new Exception("Missing at least one file specification.");

            foreach (var rows in 
                from path in paths
                from rows in 
                    from source in new[] 
                    {
                        Parse(() => new TextFieldParser(path, encoding, detectEncoding: false)
                        {
                            TextFieldType  = FieldType.Delimited,
                            Delimiters = new[] { "," }, 
                        }, 
                        (_, hs) => hs, 
                        (ln, hs, fs) => new 
                        { 
                            LineNumber = ln, 
                            Headers = hs, 
                            Fields = fs 
                        })
                    }
                    select source.Index() into source
                    from pair in source.GroupAdjacent(e => e.Key / linesPerGroup, e => e.Value)
                                       .Pairwise((prev, curr) => new { Previous = prev, Current = curr })
                                       .Index()
                    from rows in pair.Key == 0 
                                 ? new[] { pair.Value.Previous, pair.Value.Current } 
                                 : new[] { pair.Value.Current }
                    select rows
                let filename = string.Format(CultureInfo.InvariantCulture, 
                                   @"{0}-{1}{2}", 
                                   Path.GetFileNameWithoutExtension(path),
                                   rows.Key + 1,
                                   Path.GetExtension(path))
                let dir = string.IsNullOrEmpty(outputDirectoryPath) 
                        ? Path.GetDirectoryName(path) 
                        : outputDirectoryPath
                let ofp = Path.Combine(dir, filename)
                select new 
                {
                    OutputFilePath = emitAbsolutePaths 
                                   ? Path.GetFullPath(ofp) 
                                   : ofp,
                    Rows = from row in rows.Index()
                           select new 
                           { 
                               Index = row.Key, 
                               row.Value.Headers, 
                               row.Value.Fields 
                           }
                })
            {
                Console.WriteLine(rows.OutputFilePath);
                using (var writer = new StreamWriter(rows.OutputFilePath, false, encoding))
                foreach (var row in rows.Rows)
                {
                    if (row.Index == 0)
                        writer.WriteLine(row.Headers.ToQuotedCommaDelimited());
                    writer.WriteLine(row.Fields.ToQuotedCommaDelimited());
                }
            }
        }

        static string ToQuotedCommaDelimited(this IEnumerable<string> fields)
        {
            Debug.Assert(fields != null);

            var quoted = 
                from field in fields 
                select field ?? string.Empty into field
                select field.Replace("\"", "\"\"") into escaped
                select "\"" + escaped + "\"";
            return quoted.ToDelimitedString(",");
        }

        static IEnumerable<TResult> Parse<THeader, TResult>(
            Func<TextFieldParser> opener, 
            Func<long, string[], THeader> headerSelector,
            Func<long, THeader, string[], TResult> resultSelector)
        {
            Debug.Assert(opener != null);
            Debug.Assert(headerSelector != null);
            Debug.Assert(resultSelector != null);

            using (var parser = opener())
            {
                if (parser == null)
                    throw new NullReferenceException("Unexpected null reference where an instance of TextFieldParser was expected.");
                var headerInitialzed = false;
                var header = default(THeader);
                while (!parser.EndOfData)
                {
                    if (!headerInitialzed)
                    {
                        header = headerSelector(parser.LineNumber, parser.ReadFields());
                        headerInitialzed = true;
                    }
                    else
                    {
                        yield return resultSelector(parser.LineNumber, header, parser.ReadFields());
                    }
                }
            }
        }

        static readonly Uri HomeUrl = new Uri("http://bitbucket.org/raboof/splitcsv");

        static IEnumerable<string> About()
        {
            var verinfo = GetVersionInfo();
            return new[]
            {
                string.Format("{0} (version {1})", verinfo.ProductName, verinfo.FileVersion),
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
