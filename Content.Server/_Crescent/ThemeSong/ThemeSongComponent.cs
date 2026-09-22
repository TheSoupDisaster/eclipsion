using Content.Shared.Audio.Jukebox;
using Robust.Shared.Prototypes;

namespace Content.Server._Crescent.ThemeSong;

/// <summary>
/// Gives the owner its own soundtrack: the song starts when the owner spawns and loops until it dies.
/// Playback rides on the owner's <see cref="JukeboxComponent"/>, which is where the audible range and
/// volume are set, so the owner is effectively a walking boombox that nobody can switch off.
/// </summary>
[RegisterComponent]
public sealed partial class ThemeSongComponent : Component
{
    /// <summary>
    /// Track to loop. Same prototypes the jukeboxes pick from.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JukeboxPrototype> Song;
}
