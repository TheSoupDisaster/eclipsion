command-glimmershow-description = Show the current glimmer level.
command-glimmershow-help = No arguments.

command-glimmerset-description = Set glimmer to a number.
command-glimmerset-help = glimmerset (integer)

command-lspsionic-description = List psionics.
command-lspsionic-help = No arguments. Lists each psionic player with the net entity id the other psionic commands accept.

command-addpsionicpower-description = Initialize an entity as Psionic with a given PowerPrototype
command-addpsionicpower-help = Usage: addpsionicpower <player name or entity id> <power>
addpsionicpower-args-two-error = Argument 2 must match the PrototypeId of a PsionicPower

command-addrandompsionicpower-description = Initialize an entity as Psionic with a random PowerPrototype that is available for that entity to roll.
command-addrandompsionicpower-help = Usage: addrandompsionicpower <player name or entity id>

command-removepsionicpower-description = Remove a Psionic power from an entity.
command-removepsionicpower-help = Usage: removepsionicpower <player name or entity id> <power>
removepsionicpower-args-two-error = Argument 2 must match the PrototypeId of a PsionicPower.
removepsionicpower-not-psionic-error = The target entity is not Psionic.
removepsionicpower-not-contains-error = The target entity does not have this PsionicPower.

command-removeallpsionicpowers-description = Remove all Psionic powers from an entity.
command-removeallpsionicpowers-help = Usage: removeallpsionicpowers <player name or entity id>
removeallpsionicpowers-not-psionic-error = The target entity is not Psionic.
