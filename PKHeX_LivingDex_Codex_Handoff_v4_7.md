# PKHeX Living Dex Project — Codex Handoff / Technical Context

> **Purpose:** This document is a complete handoff of the custom PKHeX + Auto-Legality Mod (ALM) Living Dex work done in this chat.
>
> Give this file to Codex together with the **latest source package**:
>
> `PKHeX_LivingDex_Polisher_v4_7_MetLevelEvolutionChain.zip`
>
> The objective is to let Codex continue development without having to rediscover the project history, design decisions, bugs, or intended behavior.

---

# 1. Project goal

Build a custom PKHeX / santacrab2 Auto-Legality Mod workflow that can create a polished, plausible, game-native Living Dex directly inside Nintendo Switch Pokémon saves.

The desired final collection is:

1. **Normal Living Dex**
2. **All permanent/legal in-game forms**
3. **Shiny Living Dex**
4. **All legal shiny permanent forms**
5. Both sections in the same save, sequentially organized
6. Generated using native provenance where possible
7. Using the loaded save's trainer identity when legally allowed
8. Post-processed so levels look like naturally evolved Pokémon instead of arbitrary Lv.100 outputs
9. Conservative about provenance
10. Safe for official Pokémon HOME transfer

The workflow intentionally prefers:

`native game save -> PKHeX/ALM -> restore save to Switch -> official Pokémon HOME`

HOME should assign its own tracker server-side.

---

# 2. Target games

The custom tools are intended only for these Nintendo Switch games:

- Pokémon: Let's Go, Pikachu! — `GameVersion.GP`
- Pokémon: Let's Go, Eevee! — `GameVersion.GE`
- Pokémon Sword — `GameVersion.SW`
- Pokémon Shield — `GameVersion.SH`
- Pokémon Brilliant Diamond — `GameVersion.BD`
- Pokémon Shining Pearl — `GameVersion.SP`
- Pokémon Legends: Arceus — `GameVersion.PLA`
- Pokémon Scarlet — `GameVersion.SL`
- Pokémon Violet — `GameVersion.VL`
- Pokémon Legends: Z-A — `GameVersion.ZA`

Relevant current `GameVersion` values in the santacrab2 / PKHeX branch used during development:

```text
GP  = 42
GE  = 43
SW  = 44
SH  = 45
PLA = 47
BD  = 48
SP  = 49
SL  = 50
VL  = 51
ZA  = 52
CP  = 53
```

`GG` is the LGPE version group containing GP + GE.

---

# 3. Repository / dependency context

Primary ALM repository used:

```text
https://github.com/santacrab2/PKHeX-Plugins
branch: cherrytree
```

Current source package downloads that branch during build.

At the time these fixes were developed, the projects referenced:

```text
PKHeX.Core 26.7.7
```

The santacrab2 `cherrytree` branch targets:

```text
net10.0-windows
```

A **.NET 10 SDK** is therefore expected for compilation.

---

# 4. Current latest package

Latest package produced in this conversation:

```text
PKHeX_LivingDex_Polisher_v4_7_MetLevelEvolutionChain.zip
```

It contains:

```text
Build-And-Install.ps1
CombinedLivingDex.cs
DESIGN_NOTES.txt
ExportBoxToShowdown.cs
LivingDexPolisher.cs
README.txt
```

This is the version Codex should treat as the current baseline.

Do not restart from v1/v2/v3 unless specifically debugging historical behavior.

---

# 5. Build / installation workflow

From PowerShell, inside the extracted package:

```powershell
powershell -ExecutionPolicy Bypass -File .\Build-And-Install.ps1
```

Optional parameters exist:

```powershell
-PKHeXPath "C:\...\PKHeX.exe"
-WorkDir "C:\...\custom-build-folder"
```

Behavior of the build script:

1. Confirms the custom source files exist.
2. Checks that `dotnet` exists.
3. Warns if the SDK is not 10.x.
4. Downloads:

```text
santacrab2/PKHeX-Plugins
branch cherrytree
```

5. Extracts the source.
6. Copies custom plugins into:

```text
AutoLegalityMod\Plugins\
```

7. Replaces the stock exporter if the custom exporter file is present.
8. Applies the `GenerateSeed64` compatibility patch described below.
9. Builds:

```text
AutoLegalityMod\AutoModPlugins.csproj
```

10. Copies the resulting DLL to:

```text
compiled\AutoModPlugins.dll
```

11. If `-PKHeXPath` is supplied, it:
    - finds the PKHeX directory,
    - creates `plugins\`,
    - backs up an existing `AutoModPlugins.dll`,
    - installs the new DLL.

12. The script deliberately leaves the final PKHeX restart to the user.

If build fails, the rule throughout development was:

> use the **first `CSxxxx` compiler error** as the debugging target; do not guess.

---

# 6. Important upstream build compatibility fix

A real incompatibility existed between current santacrab2 `cherrytree` source and `PKHeX.Core 26.7.7`.

The branch called:

```csharp
GS64.GenerateSeed64(raw, tr, converted64);
```

But the actual `PKHeX.Core 26.7.7` interface was:

```csharp
public interface IGenerateSeed64
{
    void GenerateSeed64(PKM pk, ulong seed);
}
```

So the compatible call is:

```csharp
GS64.GenerateSeed64(raw, converted64);
```

`Build-And-Install.ps1` patches this automatically when it detects `PKHeX.Core 26.7.7`.

Do not remove this patch unless the upstream API changes.

---

# 7. Historical compile fix: ambiguous Math.Max

An earlier source used:

```csharp
Math.Max(1, source.MetLevel)
```

Because `MetLevel` is a byte, C# reported ambiguity between overloads.

It was corrected to:

```csharp
Math.Max(1, (int)source.MetLevel)
```

This fix is already incorporated in later versions.

---

# 8. User provenance philosophy / non-negotiable constraints

This project deliberately distinguishes **PKHeX Legal** from proof of naturally legitimate acquisition.

Important rules:

## 8.1 HOME Tracker

- HOME Tracker is server-side.
- Never fabricate a HOME tracker.
- Never clone a HOME tracker from one Pokémon to another.
- Prefer running the custom workflow **before** the Pokémon enter HOME.
- Once a tracker exists, treat origin/tracker data as finalized.
- The Polisher skips HOME-tracked Pokémon for modifications.

Desired chain:

```text
native save
-> PKHeX / ALM
-> restore save to Switch
-> official HOME
-> HOME assigns tracker
```

## 8.2 Native game origin

Preferred collection design:

```text
LGPE dex generated in LGPE
SWSH dex generated in SWSH
BDSP dex generated in BDSP
PLA dex generated in PLA
SV dex generated in SV
ZA dex generated in ZA
```

Avoid using one LGPE dex as the universal source for every later game.

## 8.3 Version pair provenance

Native-only generation may still legitimately select the paired version where appropriate.

Pairs:

```text
GP <-> GE
SW <-> SH
BD <-> SP
SL <-> VL
```

Single-game provenance:

```text
PLA only PLA
ZA only ZA
```

## 8.4 Fixed encounter trainer data

Fixed NPC/event OT/TID/SID must remain fixed if required by the encounter.

Do **not** replace every event/NPC trainer with the loaded save trainer.

## 8.5 LGPE Partner Pokémon

Partner Pikachu / Partner Eevee are special save-bound starter forms.

They are **not** ordinary Living Dex entries.

They must not be:

- generated as a normal Living Dex form,
- overwritten,
- re-leveled,
- re-balled,
- legalized/regenerated by the Polisher,
- treated as transferable ordinary forms.

## 8.6 Other game-specific provenance notes

- Regular Pikachu/Eevee can be boxed.
- Mega Evolutions are temporary battle states; they are not Living Dex slots.
- LGPE Mew is shiny-locked.
- LGPE Meltan/Melmetal provenance is through GO/GO Park.
- Meltan evolves only in Pokémon GO.
- Shiny Meltan/Melmetal require a legitimately available GO shiny period.
- LGPE Pokémon can move to HOME.
- Once an LGPE Pokémon is moved from HOME into a non-LGPE game, it cannot return to LGPE.

---

# 9. Recommended ALM generation settings

The combined generator temporarily applies a conservative native-generation profile:

```text
UseTrainerData = True
GameVersionPriority = NativeOnly
SetMatchingBalls = False
SetAllLegalRibbons = False
ForceLevel100for50 = False
SetBattleVersion = False
```

It also registers the loaded save trainer:

```csharp
TrainerSettings.Register(sav);
```

Then:

```csharp
APILegality.UseTrainerData = true;
```

The user's previous ALM settings are saved before generation and restored in `finally`.

The combined generator itself controls:

```text
IncludeForms
SetShiny
SetAlpha
TransferVersion
```

---

# 10. Combined Living Dex generator

File:

```text
CombinedLivingDex.cs
```

Menu:

```text
Tools
-> Auto-Legality Mod
-> Generate Normal + Shiny Living Dex
```

The generator creates two sections.

## 10.1 Normal section

Uses:

```csharp
var normalCfg = new LivingDexConfig
{
    IncludeForms = true,
    SetShiny = false,
    SetAlpha = false,
    TransferVersion = sav.Version,
};
```

## 10.2 Shiny section

Uses:

```csharp
var shinyCfg = new LivingDexConfig
{
    IncludeForms = true,
    SetShiny = true,
    SetAlpha = false,
    TransferVersion = sav.Version,
};
```

Then filters the shiny results:

```csharp
var shiny = shinyGenerated
    .Where(z => z.IsShiny && !IsSaveBoundPartner(z))
    .ToList();
```

Why the explicit `.IsShiny` filter exists:

ALM can return a legal non-shiny result for a shiny-locked species/form. A true shiny Living Dex should **omit** that entry instead of duplicating a normal Pokémon in the shiny section.

---

# 11. Form handling

The generator intentionally uses:

```text
IncludeForms = true
```

for both normal and shiny sections.

The underlying ALM / PKHeX Living Dex generation already excludes categories such as:

- forms not present in the loaded game's Personal table,
- battle-only forms,
- fused forms,
- Lord forms,
- Totem forms outside supported context.

ALM-specific special form logic such as Alcremie form arguments should remain intact.

Do not replace this with a hard-coded form list unless there is a proven upstream issue.

---

# 12. LGPE Partner / active-party storage protection

This was one of the most important safety fixes.

PKHeX `SAV7b` uses one storage pool for the LGPE boxes and live game references.

Relevant save behavior:

```csharp
public override StorageSlotSource GetBoxSlotFlags(int index)
```

The save can mark a box-storage index as:

```text
Party1 ... Party6
Starter
```

The Partner slot uses the special `STARTER` pointer.

PKHeX's `StorageSlotSource.IsOverwriteProtected()` treats:

- Starter
- Locked
- Battle Team

as overwrite protected.

However, ordinary LGPE party pointers are not generic overwrite-protected flags, so the custom generator protects those explicitly.

Current helper logic is conceptually:

```csharp
private static bool IsProtectedGameplaySlot(SaveFile sav, int index)
{
    var flags = sav.GetBoxSlotFlags(index);

    if (flags.IsOverwriteProtected())
        return true;

    if (sav.Version is GameVersion.GP or GameVersion.GE)
        return flags.IsParty() >= 0;

    return false;
}
```

Partner detection:

```csharp
private static bool IsSaveBoundPartner(PKM pk)
    => pk is PB7 { IsStarter: true };
```

`PB7.IsStarter` identifies:

```text
Pikachu form 8
Eevee form 1
```

## Generator behavior in LGPE

- Partner is filtered out of generated Normal Living Dex.
- Partner is filtered out of generated Shiny Living Dex.
- Starter storage slot is never overwritten.
- Active party storage references are never overwritten.
- Generated Living Dex begins at the **next complete box after the highest gameplay pointer**.
- Shiny section begins at the next full box after the normal section.

## Polisher behavior in LGPE

Protected gameplay slots are skipped.

Expected report detail:

```text
SKIP GAMEPLAY SLOT — ...
```

The Partner and live party should therefore remain untouched.

Important:

If an older version already overwrote the Partner, this project does not fabricate a replacement. Restore a backup from before that overwrite.

---

# 13. Writable slot planning

Current combined generator no longer assumes box slot 0 is safe.

It builds explicit writable slot lists.

Conceptually:

```csharp
TryPlanWritableSlots(
    sav,
    normalStart,
    normal.Count,
    out normalSlots);
```

and later:

```csharp
TryPlanWritableSlots(
    sav,
    shinyStart,
    shiny.Count,
    out shinySlots);
```

Protected slots are skipped.

Capacity is checked before writing.

If the collection cannot fit, generation aborts rather than partially writing.

If writable target slots are already occupied, the user is warned before overwrite.

---

# 14. Custom export tool

File:

```text
ExportBoxToShowdown.cs
```

Purpose:

- export all boxes,
- export a selected box range,
- output Regen / Showdown-like text,
- copy to clipboard,
- ignore empty slots,
- party excluded.

This became important for auditing the generated boxes and discovering under-level evolved Pokémon.

---

# 15. Polisher menu and workflow

File:

```text
LivingDexPolisher.cs
```

Menu:

```text
Tools
-> Auto-Legality Mod
-> Living Dex Polish, Legalize & Audit
    -> Polish Current Box
    -> Polish ALL Boxes (Recommended)
    -> Polish Box Range...
    -> Audit ALL Boxes Only
```

Recommended command:

```text
Polish ALL Boxes (Recommended)
```

High-level order:

1. Validate supported game.
2. Enforce HOME tracker safety policy.
3. Skip empty slots.
4. Skip protected LGPE gameplay slots.
5. Skip eggs for modification.
6. Skip HOME-tracked Pokémon for modification.
7. Analyze legality.
8. Repair invalid Pokémon conservatively.
9. Normalize current level using the project's evolution policy.
10. Repair level-dependent data only if needed.
11. Naturalize conspicuous Master Balls when safely legal.
12. Run final legality audit.
13. Run separate canonical evolution-floor audit.
14. Produce a report.

---

# 16. Conservative invalid repair philosophy

Already-legal Pokémon should remain as intact as possible.

For an invalid source, the project evolved through several approaches. Current intent remains:

1. Try conservative edits.
2. Only use ALM regeneration as a last resort.
3. Reject regenerated results when provenance constraints are violated.
4. Explicitly report regeneration.

Regeneration must not silently:

- introduce a HOME tracker,
- move origin outside the loaded save/native version pair,
- replace fixed event/NPC origin improperly.

---

# 17. Master Ball naturalization

One aesthetic problem discovered in ALM outputs was the frequent use of Master Balls.

The Polisher does **not** broadly rewrite every legal ball.

It only targets:

```text
Ball.Master
```

and only replaces it if PKHeX confirms a legal common alternative.

Legal ball candidates are obtained using:

```csharp
BallApplicator.GetLegalBalls(...)
```

Fixed-ball encounters remain protected because the legal-ball list constrains replacements.

General common pool used outside PLA:

```text
Poke Ball
Great Ball
Ultra Ball
Premier Ball
```

PLA uses its own ball family:

```text
LAPoke
LAGreat
LAUltra
LAFeather
LAWing
LAHeavy
```

Ball choice is deterministic, based on Pokémon data, rather than pure runtime randomness.

`SetMatchingBalls=False` remains intentional.

---

# 18. LGPE Combat Power bug and fix

This was a major bug.

Changing:

```csharp
pk.CurrentLevel
```

updates EXP / stat level, but LGPE `PB7` stores Combat Power separately in `Stat_CP`.

PKHeX checks that:

```text
Stat_CP == CalcCP
```

Earlier code lowered the level while leaving the Lv.100 CP value stale.

Result:

- Pokémon often refused to go below levels like 99 / 98 / 95.
- This initially looked like an evolution-legality problem.
- It was actually stale CP.

Fix:

```csharp
private static void RefreshLevelDependentData(PKM pk)
{
    if (pk is PB7 pb7)
        pb7.ResetCP();
}
```

This must run after every LGPE level change and before legality re-analysis.

Do not remove it.

---

# 19. Why the original "lowest legal level" strategy was wrong

An early normalizer used:

```text
for level = MetLevel ... CurrentLevel:
    clone
    set CurrentLevel
    if LegalityAnalysis.Valid:
        choose that level
```

Problems:

1. Lv.100 moves could keep lower levels invalid.
2. Hyper Training could keep lower levels invalid.
3. LGPE stale CP could keep lower levels invalid.
4. Encounter matching could change internally.
5. Most importantly, PKHeX legality can allow **direct under-level encounters of evolved species**, which may be legal but do not match the desired Living Dex presentation.

Example from real exported boxes before the canonical fixes:

```text
Dratini    Lv.18
Dragonair  Lv.18
Dragonite  Lv.3
```

Also observed:

```text
Poliwhirl  Lv.3
Poliwrath  Lv.3
Tentacruel Lv.7
```

These can be encounter-legal in PKHeX but were unacceptable for the project's requested evolution-chain policy.

---

# 20. Evolution level policy — current v4.7 behavior

This is the most important logic in the current project.

The user's desired rule is not simply:

```text
"what is the lowest level PKHeX considers legal?"
```

It is:

```text
"at what earliest level could this species exist if the normal evolution
chain began from this Pokémon's actual MetLevel?"
```

Current implementation uses:

```csharp
EvolutionTree.GetEvolutionTree(pk.Context)
```

Then builds the normal pre-evolution chain with:

```csharp
tree.Reverse.GetPreEvolutions(pk.Species, pk.Form)
```

Then resolves each forward edge using:

```csharp
tree.Forward.GetForward(from.Species, from.Form)
```

For every evolution edge:

```text
target =
MAX(
    previousStageLevel + method.LevelUp,
    method.Level
)
```

This is the core formula.

---

# 21. Meaning of `EvolutionMethod.LevelUp`

PKHeX evolution table entries provide:

```csharp
EvolutionMethod(
    ushort Species,
    ushort Argument,
    byte Form,
    EvolutionType Method,
    byte Level,
    byte LevelUp)
```

Important fields:

```text
Level   = method's level requirement
LevelUp = whether an additional level-up must occur
```

PKHeX's own structural check contains the idea:

```text
if lvl < levelMin + LevelUp
    insufficient
```

The project uses that structural information instead of relying on encounter-aware `EvoCriteria.LevelMin` as the Living Dex policy.

---

# 22. Traditional level-up evolution examples

Examples expected in LGPE / SWSH / BDSP / SV where the method requires a level-up.

## Dratini

```text
Dratini Met 3
-> Dragonair 30
-> Dragonite 55
```

## Squirtle special case

If Squirtle is obtained already at Lv.16:

```text
Squirtle Met 16
-> Wartortle 17
-> Blastoise 36
```

Why Wartortle is 17 rather than 16:

The Pokémon was still Squirtle at the moment it entered the trainer's possession at Lv.16. A level-up must occur after acquisition for the evolution.

## Slowpoke

If:

```text
Slowpoke Met 39
```

and the evolution threshold is already satisfied, it still needs another level-up:

```text
Slowbro 40
```

## Magnemite

If:

```text
Magnemite Met 37
```

the same logic gives:

```text
Magneton 38
```

This was the specific refinement introduced in v4.7.

---

# 23. Item evolution examples

Item evolutions do not receive an artificial `+1` unless the actual evolution method indicates a level-up requirement.

Examples:

```text
Growlithe Met 11
-> Fire Stone
-> Arcanine 11
```

So **Arcanine Lv.11 is intentional and correct for this policy.**

Other examples:

```text
Pikachu Met 3
-> Thunder Stone
-> Raichu 3
```

```text
Clefairy Met 5
-> Moon Stone
-> Clefable 5
```

```text
Vulpix Met 11
-> Fire Stone
-> Ninetales 11
```

A low-level evolved Pokémon is therefore **not automatically wrong**.

The method matters.

---

# 24. Trade evolution examples

Trade-only evolution does not artificially add a level.

Examples:

```text
Abra Met 11
-> Kadabra 16
-> trade
-> Alakazam 16
```

```text
Geodude Met 5
-> Graveler 25
-> trade
-> Golem 25
```

---

# 25. Stone evolution after level-stage example

Example:

```text
Oddish Met 3
-> Gloom 21
-> Leaf Stone
-> Vileplume 21
```

Same idea:

```text
Bellsprout Met 3
-> Weepinbell 21
-> Leaf Stone
-> Victreebel 21
```

---

# 26. Fossil / already-above-threshold case

A key LGPE example:

If a fossil Pokémon is received at a level already above its normal evolution threshold, but it is still in the pre-evolution form, a new level is needed.

Example:

```text
Omanyte Met 44
-> Omastar 45
```

Likewise:

```text
Kabuto Met 44
-> Kabutops 45
```

This same principle was also used for:

```text
Meowth-Alola Met 44
-> Persian-Alola 45
```

```text
Grimer-Alola Met 44
-> Muk-Alola 45
```

when the evolution requires a level-up from the obtained pre-evolution.

---

# 27. PLA / Z-A evolution behavior

Do not hard-code traditional `+1` rules for every game.

PKHeX's evolution resources are deliberately loaded differently.

Relevant `EvolutionTree` setup observed during development:

```csharp
Evolves8a = GetViaPersonal(PersonalTable.LA, Get("la", "la"u8, 0));
Evolves9a = GetViaPersonal(PersonalTable.ZA, Get("za", "za"u8, 0));
```

The third argument sets the default level-up offset to `0`.

This corresponds to games where the evolution can be manually triggered once its condition is available.

Therefore the current algorithm should continue to respect:

```text
method.LevelUp
```

from PKHeX's actual evolution table rather than using a global per-game formula.

---

# 28. v4.4 mistake: treating EvoCriteria.LevelMin as canonical

v4.4 tried to use:

```csharp
la.Info.EvoChainsAllGens.Get(pk.Context)
```

and current-stage:

```csharp
EvoCriteria.LevelMin
```

as the desired Living Dex target.

That was a conceptual mistake.

`EvoCriteria.LevelMin` is legality / encounter aware.

PKHeX can therefore permit an evolved species at a low level when the evolved species itself has a direct legal encounter.

That is correct for legality analysis, but not for the requested "normal evolution chain" presentation.

v4.6 removed that as the canonical source of truth.

---

# 29. v4.6 canonical EvolutionTree fix

v4.6 switched to structural `EvolutionTree` traversal.

It fixed examples such as:

```text
Dragonair -> at least 30
Dragonite -> at least 55
Poliwhirl -> at least 25
Poliwrath -> at least 25
Tentacruel -> at least 30
```

It also allowed the Polisher to **raise** levels, not only lower them.

That matters because an already-bad polished save could contain:

```text
Dragonite Lv.3
```

and needs:

```text
Dragonite 3 -> 55
```

---

# 30. v4.7 refinement: start the normal chain from actual MetLevel

After v4.6, another subtle issue was found.

Real export showed:

```text
Slowpoke  Met 39, Current 39
Slowbro   Met 39, Current 39
```

and:

```text
Magnemite Met 37, Current 37
Magneton  Met 37, Current 37
```

Pure legality can accept those as direct evolved encounters.

But the project's requested simulation is:

```text
start the normal pre-evolution chain at the evolved Pokémon's own MetLevel
```

Then propagate the standard evolution chain.

So v4.7 changed the direct-evolved fallback from:

```text
propagate from level 1
then clamp final result to MetLevel
```

to:

```text
propagate the entire normal chain FROM MetLevel
```

Examples:

```text
Slowbro direct Met 39:
Slowpoke starts 39
-> level-up evolution
-> Slowbro 40
```

```text
Magneton direct Met 37:
Magnemite starts 37
-> level-up evolution
-> Magneton 38
```

But:

```text
Arcanine direct Met 11:
Growlithe starts 11
-> Fire Stone
-> Arcanine 11
```

And:

```text
Dragonite direct Met 3:
Dratini starts 3
-> Dragonair 30
-> Dragonite 55
```

A deliberately strict edge case:

```text
Dragonite direct Met 60
```

would be modeled as:

```text
Dratini 60
-> Dragonair 61
-> Dragonite 62
```

This is stricter than pure encounter legality but matches the user's explicit Living Dex presentation rule.

---

# 31. Current level target helper architecture

Current conceptual flow:

```text
GetEvolutionMinimumLevel(pk, la)
    |
    +-> load EvolutionTree for pk.Context
    |
    +-> collect pre-evolution stages
    |
    +-> append current species/form
    |
    +-> if base stage:
    |      return actual MetLevel
    |
    +-> if matched encounter is a pre-evolution:
    |      start at encounter/MetLevel for that stage
    |      propagate remaining chain
    |
    +-> else if current evolved species was matched directly:
           start full normal chain at actual MetLevel
           propagate every edge
```

Edge propagation:

```csharp
candidate = Math.Max(
    level + method.LevelUp,
    method.Level);
```

The code selects the smallest compatible forward method that reaches the desired next species/form.

---

# 32. Level normalization can raise or lower

The v4.7 Polisher should not assume:

```text
target <= current
```

Current target clamp is:

```csharp
Math.Clamp(targetLevel, 1, Experience.MaxLevel)
```

not:

```csharp
Math.Clamp(targetLevel, 1, source.CurrentLevel)
```

This allows repairs like:

```text
Dragonite 3 -> 55
```

as well as ordinary reductions:

```text
Charizard 100 -> 36
```

---

# 33. Moves / Relearn Moves repair

Changing a generated Lv.100 Pokémon directly to a low level can make its moves illegal.

Example concept:

```text
Charizard Lv.100
with late-level moves
```

cannot simply become:

```text
Charizard Lv.36
```

if those moves are impossible at 36.

Current conservative strategy:

1. Try target level while preserving current moves.
2. If invalid, generate a natural move set using PKHeX suggestions restricted to natural sources.

Preferred source flags:

```csharp
MoveSourceType.LevelUp |
MoveSourceType.RelearnMoves |
MoveSourceType.Evolve
```

Avoid using arbitrary TM/TR/tutor choices merely to satisfy legality.

Fallback can use encounter-oriented suggestions.

Relearn moves are also refreshed from the encounter if necessary.

Only change moves when they actually block the target level.

---

# 34. Hyper Training behavior

Hyper Training can impose a minimum level depending on the game.

Generated Lv.100 Pokémon may have Hyper Training that becomes invalid when the level is lowered.

The Polisher therefore:

1. tries target level first,
2. tries natural moves,
3. tries clearing Hyper Training,
4. tries natural moves + clearing Hyper Training.

Relevant interface:

```csharp
IHyperTrain
```

Current clear behavior conceptually:

```csharp
if (pk is IHyperTrain ht && ht.HyperTrainFlags != 0)
    ht.HyperTrainFlags = 0;
```

This is not treated as provenance destruction; it is removal of training that no longer makes sense at the target level.

Do not clear it gratuitously if not needed.

---

# 35. IV behavior / current limitation

Important: the project **does not currently guarantee every Pokémon has 6 perfect natural IVs** after polishing.

A Showdown set defaults to:

```text
31 / 31 / 31 / 31 / 31 / 31
```

but ALM may need to produce encounter-compatible IVs.

Some generated Pokémon may have non-perfect real IVs plus Hyper Training.

When Hyper Training is removed to allow a lower natural level, the underlying non-31 IVs remain.

Therefore:

```text
Legal != all natural IVs are 31
```

Future desired enhancement discussed but **not yet implemented**:

```text
If 6x31 is legal/natural for the exact encounter:
    use 6x31

Else:
    preserve legal encounter-constrained IVs

Do not fabricate illegal RNG just to force perfection.
```

Potential future report:

```text
IV AUDIT

Natural 6x31: X
Encounter-constrained IVs: Y
Hyper Trained: Z
```

Codex should treat this as **future work**, not current behavior.

---

# 36. Final audit policy

The current project deliberately has two different notions of correctness.

## 36.1 PKHeX legality

Must end with:

```text
Invalid: 0
```

## 36.2 Living Dex canonical evolution-level policy

Must also end with:

```text
Canonical evolution-floor violations: 0
```

A collection is not considered ready unless both are zero:

```text
FINAL AUDIT
Legal: ...
Invalid: 0
Canonical evolution-floor violations: 0
```

This second check exists because PKHeX can consider some direct under-level evolved encounters legal even though the project intentionally wants the normal evolution-chain floor.

---

# 37. Expected report metrics

Depending on save/game, report can contain:

```text
Pokémon found
Initially legal
Initially invalid

Evolution-minimum levels applied
Moves/relearn adjusted for target level
Hyper Training cleared for target level
Evolution-level targets blocked
Master Balls naturalized

Skipped HOME-tracked
Skipped eggs
Skipped protected gameplay slots

FINAL AUDIT
Legal
Invalid
Canonical evolution-floor violations
```

Detailed entries can include:

```text
LEVEL — Box X, Slot Y:
Species oldLevel->newLevel
(Met N; evolution minimum M)
```

Optional suffixes:

```text
moves refreshed
Hyper Training cleared
```

Blocked target example:

```text
LEVEL TARGET BLOCKED — ...
```

Protected LGPE example:

```text
SKIP GAMEPLAY SLOT — ...
```

---

# 38. Desired test workflow

Use a backup / clean save.

Recommended sequence:

```text
1. Load native game save in PKHeX.

2. Tools
   -> Auto-Legality Mod
   -> Generate Normal + Shiny Living Dex

3. Tools
   -> Auto-Legality Mod
   -> Living Dex Polish, Legalize & Audit
   -> Polish ALL Boxes (Recommended)

4. Review report.

Required:
   Invalid: 0
   Canonical evolution-floor violations: 0

5. Export full boxes to Regen/Showdown text.

6. Spot-check evolution chains.

7. Export SAV.

8. Restore to Switch.

9. Open official Pokémon HOME.

10. Let HOME assign tracker normally.
```

---

# 39. Critical regression checks

Whenever modifying the Polisher, manually verify at least these kinds of examples.

## LGPE level-up chains

```text
Caterpie -> Metapod 7 -> Butterfree 10
Weedle -> Kakuna 7 -> Beedrill 10
Pidgey -> Pidgeotto 18 -> Pidgeot 36
Dratini -> Dragonair 30 -> Dragonite 55
Tentacool -> Tentacruel 30
Poliwag -> Poliwhirl 25
```

## MetLevel already at/above threshold

```text
Squirtle Met16 -> Wartortle17
Slowpoke Met39 -> Slowbro40
Magnemite Met37 -> Magneton38
Omanyte Met44 -> Omastar45
Kabuto Met44 -> Kabutops45
```

## Item evolutions remain same level

```text
Growlithe Met11 -> Arcanine11
Pikachu Met3 -> Raichu3
Clefairy Met5 -> Clefable5
Vulpix Met11 -> Ninetales11
Oddish -> Gloom21 -> Vileplume21
```

## Trade evolutions remain same level after pre-evolution threshold

```text
Abra Met11 -> Kadabra16 -> Alakazam16
Geodude Met5 -> Graveler25 -> Golem25
Machop Met18 -> Machoke28 -> Machamp28
```

## LGPE special trade forms

Examples previously observed:

```text
Meowth-Alola Met44 -> Persian-Alola45
Grimer-Alola Met44 -> Muk-Alola45
```

## Partner protection

Partner Pikachu / Eevee should:

- remain present,
- keep the save's special starter pointer,
- never appear as generated Living Dex entry,
- never be polished.

## Shiny locks

Known shiny-locked species/forms should be absent from the shiny section, not duplicated as non-shiny.

---

# 40. Chronology of custom packages

Historical package list:

```text
PKHeX_LivingDex_LevelNormalizer_v1.zip
PKHeX_LivingDex_LevelNormalizer_v2.zip
PKHeX_LivingDex_Polisher_v3.zip
PKHeX_LivingDex_Polisher_v3_1_BUILD_FIX.zip
PKHeX_LivingDex_Polisher_v3_2_BUILD_FIX.zip
PKHeX_LivingDex_Polisher_v4_CombinedDex.zip
PKHeX_LivingDex_Polisher_v4_1_LevelFix.zip
PKHeX_LivingDex_Polisher_v4_2_Normal_Shiny_Forms.zip
PKHeX_LivingDex_Polisher_v4_3_CP_LevelFix.zip
PKHeX_LivingDex_Polisher_v4_4_EvolutionTarget.zip
PKHeX_LivingDex_Polisher_v4_5_PreservePartner.zip
PKHeX_LivingDex_Polisher_v4_6_CanonicalEvolutionLevels.zip
PKHeX_LivingDex_Polisher_v4_7_MetLevelEvolutionChain.zip
```

Treat all versions before v4.7 as superseded unless diagnosing a regression.

---

# 41. Version-by-version summary

## v1 / v2

Early level patching / normalizer attempts.

Deprecated.

Main problem: too simplistic for evolution context.

## v3

Introduced the Polisher architecture:

- Current box
- All boxes
- Box range
- audit-only
- conservative repair
- Master Ball naturalization
- final legality audit
- HOME tracker protection
- native-version pair checks

## v3.1

Build fix:

```text
GenerateSeed64(raw, tr, converted64)
```

patched to:

```text
GenerateSeed64(raw, converted64)
```

for PKHeX.Core 26.7.7.

## v3.2

Fixed ambiguous `Math.Max` overload by casting MetLevel to int.

## v4

Added combined:

```text
Normal Living Dex
+
Shiny Living Dex
```

Same save.

## v4.1

Removed overly strict internal encounter-identity equality check from level normalization.

Before that, PKHeX could choose a different internal `EncounterOriginal` after a level edit even when provenance fields were unchanged.

## v4.2

Enabled:

```text
IncludeForms = true
```

for both normal and shiny generation.

## v4.3

Discovered LGPE stale CP issue.

Added:

```csharp
PB7.ResetCP();
```

after level changes.

## v4.4

Switched level target from "first legal level" to encounter-aware `EvoCriteria.LevelMin`.

Later discovered this was still insufficient for the requested Living Dex policy.

## v4.5

Protected:

- LGPE Partner starter
- LGPE active-party storage pointers
- other overwrite-protected slots

Excluded Partner forms from Living Dex generation.

## v4.6

Replaced encounter-aware EvoCriteria level target with structural `EvolutionTree` chain traversal.

Added separate canonical level audit.

Allowed level increases.

## v4.7

Refined direct-evolved encounter handling.

The normal chain now starts from the evolved Pokémon's actual `MetLevel`.

This correctly distinguishes:

```text
Slowbro Met39 -> 40
Arcanine Met11 -> 11
```

---

# 42. Key PKHeX APIs verified during development

## CurrentLevel

`PKM.CurrentLevel` setter updates EXP / stat level.

Conceptually:

```csharp
public byte CurrentLevel
{
    get => Experience.GetLevel(EXP, PersonalInfo.EXPGrowth);
    set => EXP = Experience.GetEXP(Stat_Level = value, PersonalInfo.EXPGrowth);
}
```

It does **not** change `MetLevel`.

This is why the project changes CurrentLevel but preserves provenance MetLevel.

## LegalityAnalysis

Used as:

```csharp
new LegalityAnalysis(pk, sav.Personal)
```

Important properties:

```text
.Valid
.EncounterOriginal
.Info
```

## BallApplicator

Verified API shape:

```csharp
BallApplicator.GetLegalBalls(
    Span<Ball> result,
    PKM pk,
    LegalityAnalysis la)
```

and:

```csharp
BallApplicator.GetLegalBalls(
    Span<Ball> result,
    PKM pk,
    IEncounterTemplate enc)
```

## EvolutionTree

```csharp
EvolutionTree.GetEvolutionTree(pk.Context)
```

## Reverse chain

```csharp
tree.Reverse.GetPreEvolutions(species, form)
```

## Forward edge methods

```csharp
tree.Forward.GetForward(species, form)
```

## Partner detection

```csharp
PB7.IsStarter
```

## LGPE CP

```csharp
PB7.ResetCP()
```

## Hyper Training

```csharp
IHyperTrain
```

## Trainer registration

```csharp
TrainerSettings.Register(sav);
```

---

# 43. EvolutionTree context mapping

Important context mapping from PKHeX:

```text
LGPE -> EntityContext.Gen7b -> EvolutionTree.Evolves7b
SWSH -> EntityContext.Gen8  -> EvolutionTree.Evolves8
PLA  -> EntityContext.Gen8a -> EvolutionTree.Evolves8a
BDSP -> EntityContext.Gen8b -> EvolutionTree.Evolves8b
SV   -> EntityContext.Gen9  -> EvolutionTree.Evolves9
ZA   -> EntityContext.Gen9a -> EvolutionTree.Evolves9a
```

Do not merge these trees.

They intentionally represent different evolution rules.

---

# 44. Important distinction: legality vs project policy

This distinction must be preserved in future work.

PKHeX answers:

```text
"Can this Pokémon legally exist?"
```

This project additionally asks:

```text
"Does this Living Dex entry look like it reached its evolved stage
at the earliest normal point in the evolution chain, starting from
its actual MetLevel?"
```

These can differ.

Example:

```text
Arcanine Lv11
```

can satisfy both.

Example:

```text
Dragonite Lv3
```

may be encounter-legal in some PKHeX contexts, but violates this project's presentation policy.

This is why both audits exist.

---

# 45. What must NOT be "fixed" without evidence

Do not casually change the following:

- `MetLevel`
- `MetLocation`
- origin `Version`
- OT/TID/SID
- PID
- EC
- event trainer identity
- HOME Tracker
- shiny status
- fixed event ball
- Partner starter pointer
- encounter provenance

The project's preferred approach is always:

```text
change derived / presentation / training data first
preserve provenance
```

---

# 46. Potential future work for Codex

The following are logical next steps but were not completed in this chat.

## 46.1 IV audit / perfect-IV policy

Add a post-process audit that reports:

```text
6x31 natural
non-perfect encounter-compatible
Hyper Trained
fixed-IV encounter
```

Potential rule:

```text
Try 6x31 only if legality remains valid for the exact encounter.

If not:
    preserve encounter-compatible IVs.
```

Do not blindly force 6x31.

## 46.2 Better unit/regression tests

Create automated tests for:

- canonical level target calculation,
- item evolutions,
- trade evolutions,
- level-up above threshold,
- multi-stage chains,
- PLA/Z-A manual evolution semantics,
- Partner slot protection,
- HOME tracker skip behavior.

## 46.3 Structured report export

Instead of only a MessageBox/string report, optionally save:

```text
LivingDexPolisher-report.txt
LivingDexPolisher-report.json
```

Useful fields:

```text
box
slot
species
form
shiny
originalCurrentLevel
targetCurrentLevel
metLevel
movesAdjusted
hyperTrainingCleared
ballChanged
initialLegal
finalLegal
canonicalLevelViolation
homeTracked
protectedGameplaySlot
```

## 46.4 Detect stale source assumptions

The build script currently assumes:

```text
santacrab2 cherrytree
PKHeX.Core 26.7.7
```

Future Codex work should check upstream API changes before preserving old patches.

In particular:

```text
IGenerateSeed64
EvolutionMethod
StorageSlotSource
LivingDexConfig
BallApplicator
MoveListSuggest
```

may change.

---

# 47. Current recommended user workflow

For each supported game:

```text
BACK UP SAVE

Open save in PKHeX

Tools
-> Auto-Legality Mod
-> Generate Normal + Shiny Living Dex

Then:

Tools
-> Auto-Legality Mod
-> Living Dex Polish, Legalize & Audit
-> Polish ALL Boxes (Recommended)

Confirm:
Invalid: 0
Canonical evolution-floor violations: 0

Export full boxes if desired for manual review.

Export SAV.

Restore SAV to Switch.

Open official Pokémon HOME.

Do not fabricate HOME tracker.
```

---

# 48. Codex instructions

When continuing this project:

1. **Start from v4.7 source.**
2. Read `LivingDexPolisher.cs`, `CombinedLivingDex.cs`, and `Build-And-Install.ps1` before changing behavior.
3. Preserve every safety fix listed in this document.
4. Do not revert to `EvoCriteria.LevelMin` as the canonical level policy.
5. Keep the `EvolutionTree` stage-by-stage formula.
6. Keep LGPE `PB7.ResetCP()` after level changes.
7. Keep Partner / party slot protection.
8. Keep HOME tracker protection.
9. Do not hard-code all evolution levels manually.
10. Prefer PKHeX's current evolution resources and method metadata.
11. If upstream PKHeX APIs have changed, update against the actual current signatures.
12. On compile failure, debug from the **first** compiler error.
13. Never claim a generated Pokémon is legitimate merely because PKHeX says `Legal`.
14. Keep provenance changes conservative.
15. Final acceptance requires both:
    - `Invalid: 0`
    - `Canonical evolution-floor violations: 0`

---

# 49. Short project specification for Codex

If Codex needs a compact statement of intent, use this:

```text
Build and maintain a PKHeX Auto-Legality Mod plugin for Nintendo Switch
Pokémon games that generates a Normal + Shiny Living Dex with all permanent
legal forms, using native save/trainer provenance when possible.

After generation, polish the collection conservatively:
- preserve provenance fields,
- skip HOME-tracked Pokémon,
- preserve LGPE Partner and active party storage,
- omit save-bound Partner forms from Living Dex generation,
- normalize CurrentLevel to the earliest normal evolution-chain level
  starting from the Pokémon's actual MetLevel,
- use EvolutionTree + EvolutionMethod.Level/LevelUp rather than
  encounter-aware EvoCriteria.LevelMin,
- recalculate LGPE CP,
- repair natural moves/relearn only when required,
- clear Hyper Training only when it blocks the target level,
- replace conspicuous Master Balls only when a common legal ball is verified,
- keep fixed encounter data intact,
- perform both PKHeX legality audit and canonical evolution-floor audit.

Required final state:
Invalid = 0
Canonical evolution-floor violations = 0

Do not fabricate HOME trackers or overwrite LGPE Partner Pokémon.
```

---

# 50. Latest status at handoff

Latest code package:

```text
PKHeX_LivingDex_Polisher_v4_7_MetLevelEvolutionChain.zip
```

v4.7 was created specifically to fix the final identified level-policy issue:

```text
Slowpoke Met39 -> Slowbro40
Magnemite Met37 -> Magneton38
```

while preserving valid same-level item evolutions such as:

```text
Growlithe Met11 -> Arcanine11
```

At this handoff point, v4.7 should be compiled and regression-tested against real saves before being treated as fully final.

The next likely area of work is the IV policy/audit, not another broad rewrite of the evolution engine unless regression testing identifies a concrete failure.

