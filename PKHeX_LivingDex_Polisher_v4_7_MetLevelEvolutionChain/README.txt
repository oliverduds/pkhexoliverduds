PKHeX LIVING DEX POLISHER v3
============================

FINAL GOAL
----------
Generate a Living Dex with a more natural-looking provenance while retaining
strict PKHeX legality.

RECOMMENDED FLOW
----------------
1. Start from a clean backup copy.
2. Generate Living Dex with ALM.
3. Run:
   Tools
   > Auto-Legality Mod
   > Living Dex Polish, Legalize & Audit
   > Polish ALL Boxes (Recommended)
4. Confirm FINAL AUDIT:
   Legal: all Pokémon
   Invalid: 0
5. Export save.
6. Restore/use on Switch.
7. Move through official Pokémon HOME only AFTER this process.

LEVEL LOGIC — PRESERVED FOR EVERY GAME
--------------------------------------
The plugin does NOT use a generic external evolution table.

For each exact Pokémon:
- keep MetLevel unchanged;
- keep encounter/origin data unchanged whenever possible;
- try Current Level values from MetLevel upward;
- accept the FIRST level PKHeX considers Legal;
- require the candidate to still match the SAME material encounter.

This automatically preserves the rules discussed for every game:
- LGPE: level-up evolutions may require +1 after encounter.
- Sword/Shield: mainline level-up behavior and special evolution methods.
- BDSP: BDSP-specific evolution tree.
- Legends: Arceus: manual evolution behavior is modeled by PKHeX.
- Scarlet/Violet: Gen 9 special evolution methods are modeled by PKHeX.
- Legends: Z-A: Z-A-specific/manual evolution tree is modeled by PKHeX.

MASTER BALL NATURALIZATION
--------------------------
The tool does NOT randomize all balls.

It changes a ball only when:
- current Ball is Master Ball;
- PKHeX confirms other common balls are legal for the SAME encounter.

It never forces a replacement if Master Ball is the only legal option.

Mainline common pool:
- Poke Ball
- Great Ball
- Ultra Ball
- Premier Ball

The distribution is deterministic and weighted by Met Level so the collection
does not become hundreds of identical balls.

PLA uses its own common ball family when applicable.

Fixed-ball gifts, NPC trades and events remain protected because PKHeX's
BallApplicator only returns balls legal for that exact encounter.

INVALID POKÉMON REPAIR
----------------------
Order of operations:
1. Conservative repair using only Current Level and, when relevant, Master Ball.
2. If still invalid, ALM regeneration via the same sav.Legalize(pk, la) path
   used by the ALM Legalize command.
3. Reject regenerated results if:
   - still invalid;
   - they already have HOME Tracker;
   - origin is outside the selected game/pair.
4. Re-apply lowest-legal-level logic.
5. Naturalize unnecessary Master Ball.
6. Final legality check before writing.
7. Full box audit at the end.

VERSION PAIRS ALLOWED
---------------------
GP <-> GE
SW <-> SH
BD <-> SP
SL <-> VL

PLA only PLA.
ZA only ZA.

HOME
----
Pokémon with a HOME Tracker are skipped by the modifying workflow.
This prevents cross-game HOME history from affecting our native-by-game Living Dex.

MENU
----
Tools
> Auto-Legality Mod
> Living Dex Polish, Legalize & Audit
    > Polish Current Box
    > Polish ALL Boxes (Recommended)
    > Polish Box Range...
    > Audit ALL Boxes Only

BUILD
-----
Requires the .NET 10 SDK for the current santacrab2 cherrytree branch.

Compile:
  powershell -ExecutionPolicy Bypass -File .\Build-And-Install.ps1

Compile + install:
  powershell -ExecutionPolicy Bypass -File .\Build-And-Install.ps1 -PKHeXPath "C:\CAMINHO\PKHeX.exe"


v3.1 BUILD FIX
--------------
The current santacrab2 cherrytree branch has a source/API mismatch:
APILegality.cs calls GenerateSeed64 with 3 arguments while the referenced
PKHeX.Core 26.7.7 exposes the 2-argument method.

Build-And-Install.ps1 now patches this automatically before dotnet build.

Expected build message:
  Applied PKHeX.Core 26.7.7 GenerateSeed64 compatibility fix.

IMPORTANT:
When using -PKHeXPath, replace the example path with the REAL location of
your PKHeX.exe. Do not literally use C:\CAMINHO\PARA\PKHeX.exe.


v3.2 COMPILER FIX
-----------------
Fixed two .NET 10 overload ambiguities in LivingDexPolisher.cs:

Before:
  Math.Max(1, source.MetLevel)

Now:
  Math.Max(1, (int)source.MetLevel)

This resolves CS0121 at the two reported locations while preserving the exact
same level-normalization logic.


v4 — COMBINED NORMAL + SHINY LIVING DEX
---------------------------------------
New command:
  Tools
  > Auto-Legality Mod
  > Generate Normal + Shiny Living Dex

It generates:
1. Normal Living Dex (species only)
2. Shiny Living Dex (species only)

The Shiny section starts at the next box boundary after the Normal section.
Shiny-locked species are OMITTED from the Shiny section.

During generation the plugin temporarily enforces:
  UseTrainerData = True
  GameVersionPriority = NativeOnly
  SetMatchingBalls = False
  SetAllLegalRibbons = False
  ForceLevel100for50 = False
  SetBattleVersion = False

The active save trainer is registered in ALM's trainer database before generation.
All original ALM settings are restored afterwards.

Recommended workflow:
  Generate Normal + Shiny Living Dex
  -> Living Dex Polish, Legalize & Audit
  -> Polish ALL Boxes (Recommended)
  -> Final Audit = 0 Invalid
  -> Export save
  -> Switch
  -> HOME


v4.1 — LEVEL NORMALIZATION FIX
------------------------------
Removed the overly strict requirement that PKHeX must resolve the exact same
internal EncounterOriginal object at every tested Current Level.

The intended rule is restored:

  MetLevel is preserved.
  All provenance fields are preserved.
  Clone the exact Pokémon.
  Test Current Level from MetLevel upward.
  First level whose full LegalityAnalysis is Valid wins.

Only CurrentLevel/EXP are changed by the level normalizer.

This restores the logic originally designed after the LGPE invalid-level cases:
- whole evolution chain is considered by PKHeX;
- +1 after met level is enforced when the game/evolution requires it;
- PLA / Z-A manual-evolution rules remain handled by PKHeX;
- Current Level is never reduced below MetLevel.


v4.2 — NORMAL + SHINY + ALL LEGAL FORMS
----------------------------------------
The combined generator now uses:
  IncludeForms = True

for BOTH sections.

It generates:
1. Normal Living Dex including all permanent/legal forms available in the game.
2. Shiny Living Dex including all legal shiny forms available in the game.

The underlying ALM generator already excludes forms that are not valid for a
stored Living Dex in that game, including:
- battle-only forms;
- fused forms;
- lord forms;
- forms not present in the loaded game's Personal table;
- Totem forms outside their supported context.

Special form handling already present in ALM (for example Alcremie form
arguments) is preserved.

Shiny-locked species/forms are omitted from the Shiny section because only
actually shiny generated entries are retained.

Capacity is checked before writing. If Normal + Shiny + Forms does not fit in
the loaded save's boxes, the plugin aborts without writing the combined dex.


v4.3 — LGPE COMBAT POWER / LEVEL FIX
------------------------------------
Root cause found for LGPE levels not decreasing correctly.

PB7 stores Stat_CP separately. Changing CurrentLevel updates EXP/Stat_Level but
does NOT automatically update Stat_CP. PKHeX's MiscVerifierPB7 requires:

  Stat_CP == CalcCP

The previous normalizer therefore rejected most lower levels due to stale
level-100 CP, not because of evolution legality.

v4.3 now calls:
  PB7.ResetCP()

after every candidate CurrentLevel change and before LegalityAnalysis.

This changes only a derived LGPE value that must match the Pokémon's current
level. Provenance fields remain unchanged:
- MetLevel
- MetLocation
- Version
- OT/TID/SID
- PID/EC
- encounter data
- form/shiny
- etc.

Expected examples after this fix:
- Wartortle met at 16 -> minimum legal Current Level 17 when evolution requires
  a later level-up.
- Alakazam met as Abra at 11 -> minimum legal Current Level 16.
- Dragonite from low-level Dratini encounter -> true evolution-chain minimum,
  rather than artificial 99 caused by stale CP.


v4.4 — EVOLUTION-TARGET LEVEL ENGINE
------------------------------------
Level target now comes from PKHeX EvolutionHistory / EvoCriteria.LevelMin,
not from scanning for the first level that happens to stay legal with Lv.100 data.

Flow:
  1. Determine the current stage's EvoCriteria.LevelMin.
  2. Set CurrentLevel to that evolution minimum.
  3. Recalculate LGPE CP when applicable.
  4. If moves block the target, replace only with natural level-up/relearn/evolution moves.
  5. If Hyper Training blocks the target, clear those training flags.
  6. Accept only if final LegalityAnalysis is Valid.

Examples intended by PKHeX's own evolution criteria:
  Dratini Met 3 -> Dragonite 55
  Abra Met 11 -> Alakazam 16
  Geodude Met 5 -> Golem 25
  Squirtle received as Squirtle at 16 -> Wartortle 17 when +1 is required

PLA and Z-A use their own evolution environments, so manual-evolution behavior
is derived from PKHeX rather than hard-coded.

Normal + Shiny + all legal forms generation remains included.


v4.5 — PRESERVE LGPE PARTNER / GAMEPLAY SLOTS
----------------------------------------------
Important LGPE safety fix.

PKHeX SAV7b stores the active party and Partner Pikachu/Eevee inside the same
1000-slot storage pool used by boxes. The Partner slot is referenced by the
special STARTER pointer.

Previous combined generator wrote directly by absolute box-slot index and could
overwrite the save-bound Partner.

v4.5 changes this:

- Partner Pikachu (PB7 form 8) and Partner Eevee (PB7 form 1) are excluded from
  the generated Living Dex.
- The STARTER slot is never overwritten.
- LGPE active-party linked storage slots are never overwritten.
- In LGPE, Normal Living Dex generation starts at the next complete box after
  the highest active party/Partner storage pointer.
- Shiny Living Dex starts at the next full box after the Normal section.
- Other PKHeX overwrite-protected slots are skipped as well.
- The Polisher skips protected gameplay slots, so the Partner and active LGPE
  party are not level-normalized, re-balled, legalized or otherwise modified.
- Final Audit still audits every occupied slot, including the Partner, so an
  invalid save-bound Pokémon will still be visible in the audit result.

If an older version already overwrote/deleted the Partner, use a backup from
before generation. v4.5 preserves an existing Partner; it does not fabricate a
replacement for a missing save-bound starter.


v4.6 — CANONICAL EVOLUTION LEVEL FIX
-------------------------------------
A real bug was found by inspecting a full LGPE box export.

Examples from the affected output:
  Dragonair Lv.18
  Dragonite Lv.3
  Poliwhirl Lv.3
  Poliwrath Lv.3
  Tentacruel Lv.7

Why v4.4/v4.5 allowed this:
  LegalityAnalysis / EvoCriteria.LevelMin is encounter-aware.
  PKHeX may legally match an evolved species to a direct wild/static encounter
  at a level below the normal evolution requirement.

That is valid PKHeX legality, but it does NOT match this Living Dex project's
requested policy.

v4.6 now calculates the target from EvolutionTree itself.

For every evolution edge:
  target = MAX(previous stage minimum + method.LevelUp, method.Level)

Then the final target is never below the Pokémon's real MetLevel.

If the matched encounter is a PRE-evolution, its real MetLevel is used as the
starting stage. This preserves important cases:
  Squirtle met Lv.16 -> Wartortle Lv.17
  Omanyte met Lv.44 -> Omastar Lv.45
  Alolan Meowth met Lv.44 -> Persian-Alola Lv.45

If an EVOLVED species was encountered directly at an unusually low level,
v4.6 intentionally ignores that shortcut for the Living Dex level policy and
uses the normal evolution chain floor instead:
  Dragonair -> at least Lv.30
  Dragonite -> at least Lv.55
  Poliwhirl -> at least Lv.25
  Poliwrath -> at least Lv.25
  Tentacruel -> at least Lv.30

The polisher can now RAISE levels as well as lower them. This means an already
polished Dragonite Lv.3 will be corrected to the canonical floor on the next
v4.6 pass.

Game handling remains data-driven:
  LGPE / SWSH / BDSP / SV:
    evolution methods that require a level-up carry LevelUp=1.
  PLA / Z-A:
    their evolution tables use LevelUp=0 for manually-triggered evolutions where
    no extra level is required merely to press Evolve.

A new final policy audit is included:
  Canonical evolution-floor violations: 0

A collection is considered ready only when BOTH are zero:
  Invalid: 0
  Canonical evolution-floor violations: 0

All v4.5 protections remain:
  - Partner Pikachu/Eevee excluded and preserved
  - LGPE party/gameplay slots preserved
  - Normal + Shiny + legal forms
  - HOME tracker protection
  - LGPE CP recalculation
  - conservative move/relearn repair
  - Hyper Training clearing only when level-gated
  - Master Ball naturalization


v4.7 — METLEVEL-BASED EVOLUTION CHAIN
--------------------------------------
Refines the canonical evolution policy from v4.6.

Problem found after reviewing the polished LGPE export:
  Slowpoke Met 39 -> Slowbro remained 39
  Magnemite Met 37 -> Magneton remained 37

Those values can be PKHeX-legal as direct evolved encounters, but they do not
match this Living Dex project's requested evolution simulation.

v4.7 rule:

For an evolved Pokémon, start the NORMAL evolution chain at the Pokémon's
actual MetLevel, even when PKHeX matched the evolved species directly.

For every evolution edge:
  target = MAX(previous stage level + method.LevelUp, method.Level)

Therefore:

  LEVEL-UP EVOLUTION
    If MetLevel is already at/above the normal requirement,
    the Pokémon must gain the next required level.

    Slowpoke Met 39
      -> Slowbro 40

    Magnemite Met 37
      -> Magneton 38

    Squirtle Met 16
      -> Wartortle 17

  ITEM EVOLUTION
    No artificial extra level.

    Growlithe Met 11
      -> Arcanine 11

    Pikachu Met 3
      -> Raichu 3

    Clefairy Met 5
      -> Clefable 5

  TRADE EVOLUTION
    No artificial extra level unless the evolution method itself requires it.

    Kadabra 16
      -> Alakazam 16

    Graveler 25
      -> Golem 25

  MULTI-STAGE LEVEL CHAIN
    Dratini Met 3
      -> Dragonair 30
      -> Dragonite 55

    If a direct Dragonite had MetLevel 60, the same policy would simulate:
      Dratini 60 -> Dragonair 61 -> Dragonite 62

This deliberately differs from pure PKHeX legality when an under-level evolved
species can be caught directly. The collection policy is stricter:
the displayed CurrentLevel should reflect the first natural point at which that
evolution chain could have produced the species from the same MetLevel.

The final audit still requires:
  Invalid: 0
  Canonical evolution-floor violations: 0

All previous protections remain included:
  - Partner Pikachu/Eevee preserved and excluded from Living Dex generation
  - LGPE active party/gameplay slots protected
  - Normal + Shiny + legal forms
  - HOME tracker protection
  - LGPE CP recalculation
  - move/relearn repair when required
  - Hyper Training removal only when level-gated
  - Master Ball naturalization
  - upstream GenerateSeed64 build compatibility patch
