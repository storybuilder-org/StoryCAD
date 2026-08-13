using System.Collections.Generic;
using System.Linq;

namespace StoryCollaborator.Workflows;

/// <summary>
/// The ten interview sections (#119). Nine follow the source checklist's life arc;
/// the tenth is new, because a career interview has nothing connecting a character
/// to a story.
/// </summary>
public static class InterviewSectionCatalog
{
    // Blurbs are written in second person, addressed to the character, because that is
    // how the question will actually be put to them (Terry, 2026-08-11: "Not 'they',
    // but 'you'"). They are also written out rather than clipped: "what it costs them"
    // was flagged as too terse to parse.
    public static readonly IReadOnlyList<InterviewSection> All = new List<InterviewSection>
    {
        new("PresentWork", "Present role and work",
            "What you do, what you answer for, and what the work takes out of you."),
        new("Origin", "Origin and family",
            "Where you were born, who raised you, and whether those years were good ones."),
        new("Schooling", "Schooling and training",
            "How you were taught, what you chose to study, and whether any of it took.",
            RequiresModernSetting: true),
        new("FirstWork", "First work and the path here",
            "The first work you did for money, and how you got from there to what you do now."),
        new("Formative", "The people who shaped you",
            "The one person who changed your direction, and the thing you are proudest of."),
        new("LowPoint", "The low point",
            "The worst stretch of your life, what you did about it, and what you would undo."),
        new("OutsideWork", "Life outside the work",
            "Your family now, what you do when you are free, what you believe, "
            + "and what your body will no longer let you do."),
        new("WhatItTakes", "What it takes, and what is next",
            "The skill your work really demands, and where you think you end up when it stops."),
        new("Advice", "Closing advice",
            "What you would tell someone young who is just starting out."),
        new("StoryYouAreIn", "The story you are in",
            "What you want most right now, who could ruin you, "
            + "and what you have never told anyone.",
            NeedsProblem: true)
    };

    /// <summary>
    /// Sections offered for this outline's setting. A pre-modern or secondary-world
    /// story never sees the schooling section rather than being asked to skip it.
    /// </summary>
    public static IReadOnlyList<InterviewSection> ForSetting(bool isModernSetting) =>
        isModernSetting
            ? All
            : All.Where(s => !s.RequiresModernSetting).ToList();
}
