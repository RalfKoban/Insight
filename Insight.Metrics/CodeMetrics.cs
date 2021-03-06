﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Insight.Shared.Model;
using Insight.Shared.System;

namespace Insight.Metrics
{
    enum State
    {
        InsideBlockComment,
        InsideLineComment,
        SomewhereElse
    }

    public sealed class CodeMetrics
    {
        const string Cloc = "cloc-1.76.exe";
        const string ClocSubDir = "ExternalTools";

        /// <summary>
        /// Normalized extension -> language (cloc). Not used yet.
        /// </summary>
        readonly Dictionary<string, string> _extensionToLanguage = new Dictionary<string, string>();

        public CodeMetrics()
        {
            // These file we do support for metric calculation.

            // Cloc sometimes confuses cs files with smalltalk files. As a workaround I include smalltalk.
            _extensionToLanguage.Add(".cs", "C#,Smalltalk");
            _extensionToLanguage.Add(".xml", "XML");
            _extensionToLanguage.Add(".xaml", "XAML");

            _extensionToLanguage.Add(".java", "Java");
            _extensionToLanguage.Add(".css", "CSS");
            _extensionToLanguage.Add(".cpp", "C++");
            _extensionToLanguage.Add(".h", "C/C++ Header");
            _extensionToLanguage.Add(".c", "C");
        }

        /// <summary>
        ///     As a measure of complexity of the file
        /// </summary>
        public InvertedSpace CalculateInvertedSpaceMetric(FileInfo file)
        {
            var logicalSpacesByLine = File.ReadLines(file.FullName)
                                          .Where(IsCode)
                                          .Select(line => GetLogicalSpaces(line));

            return CalculateStatistics(logicalSpacesByLine);
        }

        /// <summary>
        /// Language
        /// </summary>
        public Dictionary<string, LinesOfCode> CalculateLinesOfCode(DirectoryInfo rootDir, IEnumerable<string> normalizedFileExtensions)
        {
            // Get path of this assembly
            var assembly = Assembly.GetAssembly(typeof(CodeMetrics));

            var location = new FileInfo(assembly.Location);

            // Map file extension to language name understood by cloc
            var languagesToParse = MapFileExtensionToLanguage(normalizedFileExtensions.ToList());
            var stdOut = CallClocForDirectory(location.Directory, rootDir, languagesToParse);

            return ParseClocOutput(stdOut);
        }

        public LinesOfCode CalculateLinesOfCode(FileInfo file)
        {
            // Get path of this assembly
            var assembly = Assembly.GetAssembly(typeof(CodeMetrics));

            var location = new FileInfo(assembly.Location);
            var stdOut = CallClocForSingleFile(location.Directory, file);

            var dict = ParseClocOutput(stdOut);

            Debug.Assert(dict.Count == 1);
            return dict.First().Value;
        }

        public string StripComments(string fileContent)
        {
            var state = State.SomewhereElse;

            var builder = new StringBuilder(fileContent.Length);
            for (var index = 0; index < fileContent.Length; index++)
            {
                var c = fileContent[index];
                var peek = GetPeek(fileContent, index);

                if (state == State.SomewhereElse && c == '/')
                {
                    if (peek == '/')
                    {
                        state = State.InsideLineComment;
                    }
                    else if (peek == '*')
                    {
                        state = State.InsideBlockComment;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }

                else if (state == State.InsideLineComment && (c == '\n' || c == '\r'))
                {
                    // Single line comment ends with line end (or file end)
                    state = State.SomewhereElse;

                    // For any other character we continue skipping the command character
                }
                else if (state == State.InsideBlockComment && c == '*' && peek == '/')
                {
                    // Block comment ends with */ (or end of file)
                    state = State.SomewhereElse;
                }
                else
                {
                    // Forgot any case above to handle=
                    Debug.Assert(false);
                }
            }

            return builder.ToString();
        }

        static string GetPathToCloc(DirectoryInfo basePath)
        {
            return Path.Combine(basePath.FullName, ClocSubDir, Cloc);
        }

        static char GetPeek(string fileContent, int index)
        {
            return index == fileContent.Length ? (char)0 : fileContent[index + 1];
        }

        static void VerifyClocInstalled(DirectoryInfo basePath)
        {
            var pathToCloc = GetPathToCloc(basePath);
            if (!File.Exists(pathToCloc))
            {
                var url = "https://github.com/AlDanial/cloc/releases/tag/v1.76";
                var path = Path.Combine(basePath.FullName, ClocSubDir);
                var builder = new StringBuilder();
                builder.AppendLine($"Executable not found: '{pathToCloc}'.");
                builder.AppendLine($"Please go to '{url}' and download the file '{Cloc}'.");
                builder.AppendLine($"Copy this file to '{path}'.");
                throw new Exception(builder.ToString());
            }
        }

        InvertedSpace CalculateStatistics(IEnumerable<int> logicalSpacesByLine)
        {
            var data = logicalSpacesByLine.ToArray();

            var min = data.Min();
            var max = data.Max();
            var mean = Statistics.Mean(data);
            var sd = Statistics.StandardDeviation(data);
            var total = data.Sum();

            return new InvertedSpace
            {
                Min = min,
                Max = max,
                Mean = mean,
                StandardDeviation = sd,
                Total = total
            };
        }

        ProcessRunner CreateRunner()
        {
            return new ProcessRunner();
        }

        string CallClocForDirectory(DirectoryInfo basePath, DirectoryInfo rootDir, IEnumerable<string> languagesToParse)
        {
            if (basePath == null)
            {
                throw new InvalidOperationException("Can't find directory of assembly");
            }

            VerifyClocInstalled(basePath);
            var languages = string.Join(",", languagesToParse);

            // --skip-uniqueness If cloc finds duplicate files it skips the duplicates. We have to disable this behavior.
            var args = $"\"{rootDir.FullName}\" --by-file --csv --quiet --skip-uniqueness --include-lang=\"{languages}\"";

            var runner = CreateRunner();
            var result = runner.RunProcess(GetPathToCloc(basePath), args, rootDir.FullName);
            return result.StdOut;
        }

        string CallClocForSingleFile(DirectoryInfo basePath, FileInfo file)
        {
            VerifyClocInstalled(basePath);

            var args = $"\"{file.FullName}\" --csv --quiet";

            var runner = CreateRunner();
            var result = runner.RunProcess(GetPathToCloc(basePath), args);
            return result.StdOut;

        }

        LinesOfCode CreateMetric(string[] parts)
        {
            var blank = parts[2].Trim();
            var comment = parts[3].Trim();
            var code = parts[4].Trim();

            var metric = new LinesOfCode
            {
                Code = int.Parse(code),
                Blanks = int.Parse(blank),
                Comments = int.Parse(comment)
            };
            return metric;
        }

        int GetLogicalSpaces(string line, int spacesPerLogicalSpace = 4)
        {
            var spaces = 0;
            var tabs = 0;

            for (var i = 0; i < line.Length; i++)
            {
                // Count until the first non whitespace is hit
                if (line[i] != ' ' && line[i] != '\t')
                {
                    break;
                }

                if (line[i] == ' ')
                {
                    spaces++;
                }

                if (line[i] == '\t')
                {
                    tabs++;
                }
            }

            return tabs + spaces / spacesPerLogicalSpace;
        }

        bool IsCode(string line)
        {
            // Regex for empty line
            return !Regex.IsMatch(line, @"^\s*$");
        }

        IEnumerable<string> MapFileExtensionToLanguage(List<string> normalizedFileExtensions)
        {
            var unknownExtensions = normalizedFileExtensions.Where(ext => !_extensionToLanguage.ContainsKey(ext)).ToList();
            if (unknownExtensions.Any())
            {
                var details = string.Join(",", unknownExtensions);
                throw new ArgumentException($"Language not supported by cloc: {details} Please check the project settings.");
            }

            // Call ToArray such that the KeyNotFoundException is thrown now!
            return normalizedFileExtensions.Select(x => _extensionToLanguage[x]).ToArray();
        }

        Dictionary<string, LinesOfCode> ParseClocOutput(string clocOutput)
        {
            var metrics = new Dictionary<string, LinesOfCode>();
            var lines = clocOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Structure of output file
                // language,filename,blank,comment,code
                var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 5)
                {
                    continue;
                }

                var file = parts[1].Trim();
                var metric = CreateMetric(parts);
                metrics[file.ToLowerInvariant()] = metric;
            }

            return metrics;
        }
    }
}