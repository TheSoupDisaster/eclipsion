using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared._Crescent.Chat;

namespace Content.Server._Crescent.Chat;

/// <summary>
/// Compiled form of a <see cref="ChatFilterPrototype"/>.
/// </summary>
public sealed class ChatFilterMatcher
{
    public readonly ChatFilterPrototype Prototype;

    private readonly Regex? _anywhere;
    private readonly Regex? _inWord;
    private readonly Regex? _wholeWord;
    private readonly Regex? _allowed;

    private static readonly Dictionary<char, char> Substitutions = new()
    {
        // leetspeak
        { '0', 'o' }, { '1', 'i' }, { '!', 'i' }, { '|', 'i' }, { '3', 'e' }, { '€', 'e' },
        { '4', 'a' }, { '@', 'a' }, { '5', 's' }, { '$', 's' }, { '7', 't' }, { '+', 't' },
        { '8', 'b' }, { '9', 'g' }, { '6', 'g' },
        // letters that don't decompose to a-z
        { 'ı', 'i' }, { 'ł', 'l' }, { 'ø', 'o' }, { 'ß', 's' }, { 'æ', 'a' }, { 'œ', 'o' },
        // cyrillic / greek look-alikes
        { 'а', 'a' }, { 'в', 'b' }, { 'е', 'e' }, { 'ё', 'e' }, { 'г', 'r' }, { 'к', 'k' },
        { 'м', 'm' }, { 'н', 'h' }, { 'о', 'o' }, { 'р', 'p' }, { 'с', 'c' }, { 'т', 't' },
        { 'у', 'y' }, { 'х', 'x' }, { 'і', 'i' }, { 'ј', 'j' }, { 'ѕ', 's' },
        { 'α', 'a' }, { 'ε', 'e' }, { 'ι', 'i' }, { 'κ', 'k' }, { 'ν', 'v' }, { 'ο', 'o' },
        { 'ρ', 'p' }, { 'τ', 't' }, { 'υ', 'u' }, { 'χ', 'x' },
    };

    public ChatFilterMatcher(ChatFilterPrototype prototype)
    {
        Prototype = prototype;
        // Anywhere is matched against text whose spaces have been stripped, so its entries
        // are stripped too. The other three run against the spaced text and keep word gaps.
        _anywhere = Build(prototype.Anywhere, "", "", spaceless: true);
        _inWord = Build(prototype.InWord, "", "", spaceless: false);
        _wholeWord = Build(prototype.WholeWord, @"\b", @"(?:e?s)?\b", spaceless: false);
        _allowed = Build(prototype.Allowed, "", "", spaceless: false);
    }

    /// <summary>
    /// Returns the offending part of the normalized text, or null if the text is clean.
    /// </summary>
    public string? Match(string normalized)
    {
        if (_allowed != null)
            normalized = _allowed.Replace(normalized, " ");

        if (_wholeWord?.Match(normalized) is { Success: true } whole)
            return whole.Value;

        if (_inWord?.Match(normalized) is { Success: true } inWord)
            return inWord.Value;

        if (_anywhere?.Match(normalized.Replace(" ", "")) is { Success: true } anywhere)
            return anywhere.Value;

        return null;
    }

    /// <summary>
    /// Lowercases, strips accents, maps look-alike characters to a-z and turns everything else into single spaces.
    /// Runs of single letters ("f u c k") are joined back into one word.
    /// </summary>
    public static string Normalize(string input)
    {
        var builder = new StringBuilder(input.Length);

        foreach (var ch in input.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            // combining accents and zero-width characters are dropped outright so they can't split a word
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.Format)
                continue;

            var c = Substitutions.GetValueOrDefault(ch, ch);

            if (c is >= 'a' and <= 'z')
                builder.Append(c);
            else if (builder.Length > 0 && builder[^1] != ' ')
                builder.Append(' ');
        }

        var words = builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder(builder.Length);
        var singles = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length == 1)
            {
                singles.Append(word);
                continue;
            }

            FlushSingles(result, singles);
            Append(result, word);
        }

        FlushSingles(result, singles);
        return result.ToString();
    }

    private static void FlushSingles(StringBuilder result, StringBuilder singles)
    {
        if (singles.Length == 0)
            return;

        // "a" or "i a" is just normal text, three or more is somebody spelling something out
        if (singles.Length >= 3)
        {
            Append(result, singles.ToString());
        }
        else
        {
            foreach (var c in singles.ToString())
                Append(result, c.ToString());
        }

        singles.Clear();
    }

    private static void Append(StringBuilder result, string word)
    {
        if (result.Length > 0)
            result.Append(' ');
        result.Append(word);
    }

    /// <param name="spaceless">
    /// Whether entries are collapsed into a single word. Set for patterns matched against
    /// text whose spaces were stripped; leave unset so "van dyke" keeps its gap.
    /// </param>
    private static Regex? Build(List<string> words, string prefix, string suffix, bool spaceless)
    {
        var patterns = words
            .Select(w => spaceless ? Normalize(w).Replace(" ", "") : Normalize(w))
            .Where(w => w.Length > 0)
            .Distinct()
            // longest first so the reported match is the most specific one
            .OrderByDescending(w => w.Length)
            // every letter may be repeated: "nigger" also matches "niiiggggerrr". The gap in
            // a multi-word entry is optional, so it covers the run-together spelling too.
            .Select(w => string.Concat(w.Select(c => c == ' ' ? " *" : c + "+")))
            .ToList();

        if (patterns.Count == 0)
            return null;

        return new Regex($"{prefix}(?:{string.Join('|', patterns)}){suffix}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
