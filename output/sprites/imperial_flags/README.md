# Imperial banner variants

The current game sprites use the user-requested white sword-and-laurel emblem.
Their full-resolution sources, final 6x/native-size preview, and repeatable
PowerShell export script are in `laurel/`. The files in this parent directory document the earlier sun
emblem iteration.

Sprited by Taleryn for Hullrot:Eclipsion from the existing Shiptest imperial
banner. The five `*-source.png` files preserve the full-resolution artwork.

Game-ready sprites are in
`Resources/Textures/_Crescent/Structures/imperialflag_improved.rsi/`.
Each is a single 32 x 32 RGBA frame, downscaled with nearest-neighbor sampling
and binary alpha (threshold 128) for crisp game rendering.

| Prototype | RSI state | Design |
| --- | --- | --- |
| ImperialFlag | flag | Regal draped banner |
| ImperialFlagCeremonial | ceremonial | Pointed ceremonial banner |
| ImperialFlagSwallowtail | swallowtail | Split-tail standard |
| ImperialFlagGuard | guard | Dark guard banner |
| ImperialFlagBattleworn | battleworn | Torn battle standard |

`preview.png` compares the original with the five final sprites at 6x scale,
with native-size copies below. The original RSI remains intact; existing maps
using ImperialFlag receive the new default through its prototype reference.
In-game appearance has not been play-tested.
