using Robust.Shared.Prototypes;

namespace Content.Shared._Crescent.Chat;

public enum ChatFilterSeverity : byte
{
    /// <summary>
    /// Blocked when chat.filter_profanity is on.
    /// </summary>
    Profanity,

    /// <summary>
    /// Always blocked while the filter is enabled, and reported to admins.
    /// </summary>
    Slur,
}

/// <summary>
/// A list of words the server refuses to relay in chat.
/// Text is lowercased, leetspeak/homoglyphs are mapped to letters (n1gg3r -> nigger),
/// and every letter of an entry may repeat (niiiggger). Entries should be plain lowercase a-z.
/// </summary>
[Prototype("chatFilter")]
public sealed partial class ChatFilterPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ChatFilterSeverity Severity = ChatFilterSeverity.Profanity;

    /// <summary>
    /// Matched even when split by spaces or symbols ("n i.g-g e r") and inside other words.
    /// Only use for words that can't plausibly show up across word boundaries of normal text.
    /// </summary>
    [DataField]
    public List<string> Anywhere = new();

    /// <summary>
    /// Matched anywhere inside a single word ("motherfucker").
    /// </summary>
    [DataField]
    public List<string> InWord = new();

    /// <summary>
    /// Matched only as a whole word, optionally with an s/es plural.
    /// </summary>
    [DataField]
    public List<string> WholeWord = new();

    /// <summary>
    /// Words that are removed before checking, to avoid false positives ("retardant", "scunthorpe").
    /// </summary>
    [DataField]
    public List<string> Allowed = new();
}
