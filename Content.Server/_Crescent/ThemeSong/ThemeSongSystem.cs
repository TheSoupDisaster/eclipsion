using Content.Shared.Audio.Jukebox;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Crescent.ThemeSong;

/// <summary>
/// Drives <see cref="ThemeSongComponent"/>. The owner's jukebox is fed its own song on spawn and kept
/// fed for as long as the owner lives, which is what makes the track repeat; death cuts it off.
/// </summary>
public sealed class ThemeSongSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThemeSongComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThemeSongComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<ThemeSongComponent> ent, ref MapInitEvent args)
    {
        QueueSong(ent);
    }

    private void OnMobStateChanged(Entity<ThemeSongComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !TryComp<JukeboxComponent>(ent, out var jukebox))
            return;

        // The corpse doesn't get to finish the track. Update stops refilling the queue from here on,
        // and a revived owner starts the song over.
        RaiseLocalEvent(ent.Owner, new JukeboxQueueClearMessage());
        _audio.Stop(jukebox.AudioStream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ThemeSongComponent, JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp, out var jukebox))
        {
            // Keeping one copy of the song waiting behind the one that is playing is what loops it:
            // the jukebox pulls the next queue entry itself the moment a track ends.
            if (jukebox.Queue.Count > 0 || _mobState.IsDead(uid))
                continue;

            QueueSong((uid, comp));
        }
    }

    /// <summary>
    /// Hands the song to the owner's jukebox. It starts playing at once if nothing is playing,
    /// otherwise it waits in the queue as the next track.
    /// </summary>
    private void QueueSong(Entity<ThemeSongComponent> ent)
    {
        RaiseLocalEvent(ent.Owner, new JukeboxQueueAddMessage(ent.Comp.Song));
    }
}
