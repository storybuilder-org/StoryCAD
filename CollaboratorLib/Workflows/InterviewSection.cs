namespace StoryCollaborator.Workflows;

/// <summary>
/// One pickable stretch of a character's life (#119).
///
/// Ids only. The questions themselves live in the Worker's CharacterInterview
/// template per ADR-005 — this repo is public and the source checklist is private
/// craft material.
/// </summary>
/// <param name="Id">Stable key sent to the Worker as the section to ask.</param>
/// <param name="Title">Picker row label.</param>
/// <param name="Blurb">One line telling the writer what the section is for.</param>
/// <param name="RequiresModernSetting">
/// True when the section assumes compulsory schooling with curricula and sports.
/// Verified against Macbeth: those questions are not hard for an eleventh-century
/// warlord, they are meaningless.
/// </param>
/// <param name="NeedsProblem">
/// True when a linked Problem sharpens the section. Not a hard requirement.
/// </param>
public sealed record InterviewSection(
    string Id,
    string Title,
    string Blurb,
    bool RequiresModernSetting = false,
    bool NeedsProblem = false);
