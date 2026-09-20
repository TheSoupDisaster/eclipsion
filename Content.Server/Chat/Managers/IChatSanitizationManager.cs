using System.Diagnostics.CodeAnalysis;
using Content.Shared._Crescent.Chat;

namespace Content.Server.Chat.Managers;

public interface IChatSanitizationManager
{
    public void Initialize();

    public bool TrySanitizeOutSmilies(string input, EntityUid speaker, out string sanitized, [NotNullWhen(true)] out string? emote);

    /// <summary>
    /// Crescent - checks a player's message against the chatFilter prototypes.
    /// </summary>
    /// <returns>True if the message should be blocked.</returns>
    public bool CheckFilter(string input, [NotNullWhen(true)] out ChatFilterPrototype? filter, [NotNullWhen(true)] out string? match);
}
