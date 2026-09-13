namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// Marks an entity whose ghost role came from an admin: placed from the spawn menu (or spawned by
/// something placed from it), or made a ghost role with makeghostrole/makeghostroleraffled.
/// Ghost roles without this marker are never offered to ghosts. See GhostRoleSystem.AdminOnly.
/// </summary>
[RegisterComponent]
public sealed partial class AdminGhostRoleComponent : Component;
