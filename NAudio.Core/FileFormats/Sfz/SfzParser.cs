using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NAudio.Sfz
{
    /// <summary>
    /// Parses SFZ instrument files into an <see cref="SfzInstrument"/>: handles
    /// comments, the <c>#define</c>/<c>$variable</c> preprocessor,
    /// <c>#include</c>, the section headers and the <c>opcode=value</c> grammar
    /// (including sample paths that contain spaces), then flattens the
    /// global/master/group/region hierarchy into playable regions.
    ///
    /// A <c>&lt;</c> always starts a header, even without surrounding
    /// whitespace (<c>&lt;group&gt;&lt;region&gt;</c>,
    /// <c>&lt;region&gt;sample=a.wav</c>), matching real players' behaviour;
    /// consequently a sample path may contain spaces but never <c>&lt;</c>.
    ///
    /// This is the text/structure layer only — opcode <em>semantics</em> (key
    /// ranges, tuning, envelopes…) and external sample loading are applied by
    /// later layers on top of the parsed model.
    /// </summary>
    public static class SfzParser
    {
        private const int MaxIncludeDepth = 32;

        private static readonly Regex DefineRegex =
            new(@"^\s*#define\s+(\$[A-Za-z0-9_]+)\s+(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex IncludeRegex =
            new(@"^\s*#include\s+""([^""]+)""\s*$", RegexOptions.Compiled);
        private static readonly Regex VariableRegex =
            new(@"\$[A-Za-z0-9_]+", RegexOptions.Compiled);
        private static readonly Regex BlockCommentRegex =
            new(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Parses SFZ text. <paramref name="includeResolver"/> supplies the text
        /// of <c>#include</c>d files; if null, includes are skipped.
        /// </summary>
        public static SfzInstrument Parse(string text, ISfzIncludeResolver includeResolver = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var preprocessed = new StringBuilder();
            Preprocess(text, includeResolver, new Dictionary<string, string>(), 0, preprocessed);

            var sections = ParseSections(preprocessed.ToString());
            return Flatten(sections);
        }

        /// <summary>
        /// Loads and parses an SFZ file, resolving <c>#include</c>s and relative
        /// sample <c>default_path</c>s against the file's directory.
        /// </summary>
        public static SfzInstrument ParseFile(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var text = File.ReadAllText(path);
            var resolver = new FileSfzIncludeResolver(Path.GetDirectoryName(Path.GetFullPath(path)));
            return Parse(text, resolver);
        }

        // ---- preprocessing: comments, #define/$var, #include ----

        private static void Preprocess(string text, ISfzIncludeResolver resolver,
            Dictionary<string, string> defines, int depth, StringBuilder output)
        {
            if (depth > MaxIncludeDepth)
                throw new InvalidDataException("SFZ #include nesting too deep (possible cycle)");

            text = BlockCommentRegex.Replace(text, " ");

            foreach (var rawLine in text.Split('\n'))
            {
                var line = StripLineComment(rawLine).Trim();
                if (line.Length == 0) continue;

                var define = DefineRegex.Match(line);
                if (define.Success)
                {
                    // a define's value may reference earlier defines
                    defines[define.Groups[1].Value] = Substitute(define.Groups[2].Value, defines);
                    continue;
                }

                line = Substitute(line, defines);

                var include = IncludeRegex.Match(line);
                if (include.Success)
                {
                    var included = resolver?.Resolve(include.Groups[1].Value);
                    if (included != null)
                        Preprocess(included, resolver, defines, depth + 1, output);
                    continue;
                }

                output.Append(line).Append('\n');
            }
        }

        private static string StripLineComment(string line)
        {
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            return comment >= 0 ? line.Substring(0, comment) : line;
        }

        private static string Substitute(string line, Dictionary<string, string> defines)
        {
            if (defines.Count == 0 || line.IndexOf('$') < 0) return line;
            return VariableRegex.Replace(line, m =>
                defines.TryGetValue(m.Value, out var value) ? value : m.Value);
        }

        // ---- section tokenizing ----

        private static List<SfzSection> ParseSections(string text)
        {
            var sections = new List<SfzSection>();
            var tokens = Tokenize(text);

            SfzHeader currentHeader = SfzHeader.Unknown;
            string currentHeaderText = null;
            Dictionary<string, string> currentOpcodes = null;
            bool inSection = false;

            void Commit()
            {
                if (inSection)
                    sections.Add(new SfzSection(currentHeader, currentHeaderText, currentOpcodes));
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Length > 2 && token[0] == '<' && token[token.Length - 1] == '>')
                {
                    Commit();
                    currentHeaderText = token.Substring(1, token.Length - 2);
                    currentHeader = ParseHeader(currentHeaderText);
                    currentOpcodes = new Dictionary<string, string>();
                    inSection = true;
                    continue;
                }

                int equals = token.IndexOf('=');
                if (equals <= 0) continue; // not an opcode and not a header — skip stray token

                string key = token.Substring(0, equals);
                var value = new StringBuilder(token.Substring(equals + 1));

                // a value (e.g. a sample path) may contain spaces: keep appending
                // following tokens until the next opcode or header
                while (i + 1 < tokens.Count && !IsHeaderOrOpcode(tokens[i + 1]))
                {
                    value.Append(' ').Append(tokens[i + 1]);
                    i++;
                }

                if (!inSection)
                {
                    // opcodes before any header: keep them in a leading global
                    currentHeader = SfzHeader.Global;
                    currentHeaderText = "global";
                    currentOpcodes = new Dictionary<string, string>();
                    inSection = true;
                }

                currentOpcodes[key] = value.ToString();
            }

            Commit();
            return sections;
        }

        // Splits the text into header and opcode tokens. Whitespace separates
        // tokens as usual, but '<' also STARTS a new token wherever it appears
        // and the '>' closing a header ends one: real players treat '<' as a
        // hard delimiter, so "<group><region>" is two headers and
        // "<region>sample=a.wav" is a header followed by an opcode (previously
        // both collapsed into junk tokens and the headers were silently lost).
        // The trade-off is that a sample path can never contain '<' — which
        // real-world files already avoid precisely because players split there.
        private static List<string> Tokenize(string text)
        {
            var tokens = new List<string>();
            foreach (var chunk in text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                int start = 0;
                for (int i = 0; i < chunk.Length; i++)
                {
                    if (chunk[i] == '<' && i > start)
                    {
                        tokens.Add(chunk.Substring(start, i - start)); // the text before the '<'
                        start = i;
                    }
                    else if (chunk[i] == '>' && chunk[start] == '<')
                    {
                        tokens.Add(chunk.Substring(start, i - start + 1)); // the <header>
                        start = i + 1;
                    }
                }
                if (start < chunk.Length) tokens.Add(chunk.Substring(start));
            }
            return tokens;
        }

        private static bool IsHeaderOrOpcode(string token)
        {
            if (token.Length > 2 && token[0] == '<' && token[token.Length - 1] == '>') return true;
            return token.IndexOf('=') > 0;
        }

        private static SfzHeader ParseHeader(string headerText)
        {
            switch (headerText.ToLowerInvariant())
            {
                case "control": return SfzHeader.Control;
                case "global": return SfzHeader.Global;
                case "master": return SfzHeader.Master;
                case "group": return SfzHeader.Group;
                case "region": return SfzHeader.Region;
                case "curve": return SfzHeader.Curve;
                case "effect": return SfzHeader.Effect;
                case "midi": return SfzHeader.Midi;
                case "sample": return SfzHeader.Sample;
                default: return SfzHeader.Unknown;
            }
        }

        // ---- hierarchy flattening ----

        private static SfzInstrument Flatten(List<SfzSection> sections)
        {
            var regions = new List<SfzRegion>();
            var global = new Dictionary<string, string>();
            Dictionary<string, string> master = null;
            Dictionary<string, string> group = null;

            string defaultPath = null;
            int noteOffset = 0;
            int octaveOffset = 0;
            Dictionary<int, int> setCc = null;

            foreach (var section in sections)
            {
                switch (section.Header)
                {
                    case SfzHeader.Control:
                        if (section.Opcodes.TryGetValue("default_path", out var dp))
                            defaultPath = dp.Replace('\\', '/');
                        if (section.Opcodes.TryGetValue("note_offset", out var no) &&
                            int.TryParse(no, out var noVal)) noteOffset = noVal;
                        if (section.Opcodes.TryGetValue("octave_offset", out var oo) &&
                            int.TryParse(oo, out var ooVal)) octaveOffset = ooVal;
                        CollectSetCc(section.Opcodes, ref setCc);
                        break;
                    case SfzHeader.Global:
                        Merge(global, section.Opcodes);
                        break;
                    case SfzHeader.Master:
                        master = new Dictionary<string, string>(section.Opcodes);
                        group = null; // a new master scope resets the current group
                        break;
                    case SfzHeader.Group:
                        group = new Dictionary<string, string>(section.Opcodes);
                        break;
                    case SfzHeader.Region:
                        regions.Add(BuildRegion(global, master, group, section.Opcodes, defaultPath));
                        break;
                }
            }

            List<(int Controller, int Value)> initialCc = null;
            if (setCc != null)
            {
                initialCc = new List<(int, int)>(setCc.Count);
                foreach (var pair in setCc) initialCc.Add((pair.Key, pair.Value));
            }

            return new SfzInstrument(regions, sections, defaultPath, noteOffset, octaveOffset, initialCc);
        }

        // <control> set_ccN=v: initial MIDI controller values, e.g. seeding a
        // loccN/hiccN gate whose controller would otherwise default to 0. Later
        // control sections override earlier ones per controller; out-of-range
        // controller numbers are ignored and values clamp to the MIDI 0..127.
        private static void CollectSetCc(IReadOnlyDictionary<string, string> opcodes, ref Dictionary<int, int> setCc)
        {
            const string prefix = "set_cc";
            foreach (var pair in opcodes)
            {
                if (!pair.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (!int.TryParse(pair.Key.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cc))
                    continue;
                if (cc < 0 || cc > 127) continue;
                if (!int.TryParse(pair.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    continue;

                setCc ??= new Dictionary<int, int>();
                setCc[cc] = Math.Clamp(value, 0, 127);
            }
        }

        private static SfzRegion BuildRegion(
            Dictionary<string, string> global, Dictionary<string, string> master,
            Dictionary<string, string> group, IReadOnlyDictionary<string, string> region,
            string defaultPath)
        {
            // precedence: global < master < group < region
            var merged = new Dictionary<string, string>(global);
            if (master != null) Merge(merged, master);
            if (group != null) Merge(merged, group);
            Merge(merged, region);

            string sample = null;
            if (merged.TryGetValue("sample", out var rawSample))
            {
                sample = rawSample.Replace('\\', '/');
                if (!string.IsNullOrEmpty(defaultPath))
                    sample = defaultPath.TrimEnd('/') + "/" + sample.TrimStart('/');
            }

            return new SfzRegion(merged, sample);
        }

        private static void Merge(Dictionary<string, string> target, IReadOnlyDictionary<string, string> source)
        {
            foreach (var pair in source) target[pair.Key] = pair.Value;
        }
    }
}
