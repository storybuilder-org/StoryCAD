# Issue #1478 implementation plan

Character Relationships Map report: do not print empty non-relationship pairings.

Branch: `issue-1478-relationships-map-placeholders` (from `dev`).
Issue: https://github.com/storybuilder-org/StoryCAD/issues/1478

## What this issue is about

The biggest part is not printing pairings that do not exist. Today the report fills those slots with placeholder text.

Two empty lines:

1. `ReportFormatter.cs:943` — `Reciprocal: (no reciprocal relationship defined)` when the partner has no reverse entry. Reciprocal relationships are opt-in.
2. `ReportFormatter.cs:903` — `(no relationships)` under every character with an empty `RelationshipList`, plus a heading for that character.

A cast of 20 with 15 one-way relationships currently produces 15 noise lines mixed with the 15 real ones, plus a heading-plus-placeholder block for every character who has no relationships.

Secondary, already decided: a real pair is printed once. If Alice→Bob and Bob→Alice both exist, do not print the pair under both headings.

## Success criteria

1. The string `(no reciprocal relationship defined)` never appears.
2. The string `(no relationships)` never appears.
3. A character heading is omitted when that character has nothing to print (empty list, or every remaining edge already printed as someone else's reciprocal).
4. If the partner has a relationship back, it is printed as a nested `Reciprocal:` line.
5. That pair is printed once.
6. `ReportFormatterRelationshipsTests` and the full MSTest run pass.

## Current behavior

`FormatCharacterRelationshipsMapReport` (`ReportFormatter.cs:874`) walks `_storyModel.StoryElements.Characters` in creation order, drops the `(none)` placeholder with `.OfType<CharacterModel>()`, then walks each character's `RelationshipList` in add order.

For every relationship it prints `-> Partner (type)`, then always writes a `Reciprocal:` line: the reverse if present, otherwise the placeholder. Characters with an empty list still get a heading and `(no relationships)`.

Symmetric example as printed today:

```
Alice
    -> Bob  (Father)
       Reciprocal: Bob -> Alice  (Son)

Bob
    -> Alice  (Son)
       Reciprocal: Alice -> Bob  (Father)
```

## New emission rules

Walk characters in creation order. Walk each `RelationshipList` in add order.

Keep a `HashSet<(Guid from, Guid to)>` of directed pairs already printed.

For character `C`, relationship to `P`:

- If `(C, P)` is already in the set, skip it.
- Print `-> {partner}  ({type})`, plus Trait/Attitude/Notes when set.
- Add `(C, P)`.
- Look up `P.RelationshipList` for an entry whose `PartnerUuid` is `C`.
- If found: print the nested `Reciprocal:` block (type, Trait/Attitude, Notes), then add `(P, C)`.
- If not found: print nothing extra. No placeholder.

Only write `C`'s heading if at least one relationship was actually printed. Collect the body first, then emit heading + body.

Zero `CharacterModel`s: keep `  (no characters)`.

Deleted partner: keep `-> (unknown character)  ({type})` and the existing Warn log. No reciprocal lookup, no placeholder.

## Sample output

Alice created first, Alice → Bob (Father), no reverse:

```
StoryCAD - Character Relationships Map
======================================

Alice
    -> Bob  (Father)
```

Bob has no heading.

Alice → Bob (Father) and Bob → Alice (Son):

```
Alice
    -> Bob  (Father)
       Reciprocal: Bob -> Alice  (Son)
```

Bob has no heading. `Reciprocal:` appears once.

Same pair, plus Bob → Carol (Friend):

```
Alice
    -> Bob  (Father)
       Reciprocal: Bob -> Alice  (Son)

Bob
    -> Carol  (Friend)
```

Carol has no heading.

A cast member with an empty `RelationshipList` does not appear.

## Files

| File | Change |
|---|---|
| `StoryCADLib/Services/Reports/ReportFormatter.cs` | `FormatCharacterRelationshipsMapReport` only. Rewrite the method doc so it no longer says a missing inverse is stated explicitly. |
| `StoryCADTests/Services/Reports/ReportFormatterRelationshipsTests.cs` | Flip assertions. Add the mixed-pair case. Fix the class summary comment. |

No `PrintReports` changes. No new types.

## Tests (red, then green)

`RelationshipsMap_WithAsymmetricRelationship_ShowsNoReciprocal` — rename to `RelationshipsMap_WithAsymmetricRelationship_OmitsReciprocalLine`. Assert Alice, Bob, Father present. Assert `(no reciprocal relationship defined)` and `Reciprocal:` absent.

`RelationshipsMap_WithSymmetricRelationship_ShowsBothDirections` — keep Father and Son. Assert `Reciprocal:` appears once. Assert Bob is not a heading (partner name under Alice is fine).

`RelationshipsMap_WithEmptyRelationshipList_RendersNoRelationships` — rename to `RelationshipsMap_WithEmptyRelationshipList_OmitsCharacter`. Only character is Loner. Assert header present; `Loner` and `(no relationships)` absent.

`RelationshipsMap_WithDeletedPartner_RendersUnknownCharacter` — unchanged except add: placeholder string absent.

New: `RelationshipsMap_WhenPartnerHasOtherRelationships_PrintsThoseOnce`. Alice→Bob Father, Bob→Alice Son, Bob→Carol Friend. Alice heading with nested Son. Bob heading with Friend only. No second Alice primary under Bob.

`Generate_WithCreateRelationshipsOnly_ProducesContentWithPageBreak` — still asserts header, `Mentor`, page break. Add: placeholder absent.

## Agents (after Design approval)

- **csharp-pro**: the formatter method.
- **test-automator**: the test file.

## Design tasks (first PIE section)

- Specify omit-missing-reciprocal, print-if-present, print-once.
- Specify omit empty character blocks, including characters whose only remaining edges were already printed as reciprocals.
- Specify creation-order walk and the directed-pair skip set.
- Lock the sample outputs above.

Code, Test, and Evaluate stay empty on the issue until Design is signed off.

## Out of scope

`FormatCharacterRelationshipReport` (per-character RTF). `PrintReports` wiring. Sorting. Changing how relationships are stored.

## Implementation notes

TDD order followed on `issue-1478-relationships-map-placeholders`.

1. Rewrote `ReportFormatterRelationshipsTests` first. `vstest.console.exe` against `StoryCADTests.dll`: 6 failed (red). Failures were the placeholders, the double Reciprocal line, and empty-character headings.
2. Changed only `FormatCharacterRelationshipsMapReport` in `ReportFormatter.cs`. Directed-pair `HashSet<(Guid, Guid)>`, omit missing Reciprocal, omit headings with an empty body.
3. Same 6 tests: 6 passed (green).
4. Full suite via `vstest.console.exe`: 1495 total, 1480 passed, 15 skipped, 0 failed. Results: `TestResults/tcox_MINERVA_2026-08-25_11_46_23_net10.0.trx`.

csharp-pro and test-automator were not launched. The change is one method plus its test class; a sequential red-green loop on the same files is faster than two agents.

## Verify

```
dotnet test StoryCADTests/StoryCADTests.csproj --settings StoryCADTests/mstest.runsettings --filter FullyQualifiedName~ReportFormatterRelationshipsTests
dotnet test StoryCADTests/StoryCADTests.csproj --settings StoryCADTests/mstest.runsettings --logger trx
```
