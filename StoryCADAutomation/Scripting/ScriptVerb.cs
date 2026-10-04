namespace StoryCADAutomation.Scripting;

/// <summary>
///     Verb set v1, exactly the table in devdocs/issue_1421_dsl_design.md. The five expect
///     forms are separate members so the interpreter dispatches one handler per member with
///     no re-parsing. Verbs missing during #1422 translation are added here by PR, never as
///     per-script workarounds.
/// </summary>
public enum ScriptVerb
{
    // structural (reporting, not execution)
    Script,
    Step,

    // session
    Launch,
    Close,
    ExpectExit,

    // pointer and keyboard
    Click,
    DoubleClick,
    RightClick,
    Drag,
    Press,
    Type,
    Focus,

    // content
    Set,
    Select,
    Toggle,

    // navigation
    OpenNode,
    Expand,
    Collapse,
    Menu,
    ContextMenu,
    Tab,

    // dialogs
    SaveFileDialog,
    OpenFileDialog,
    Dialog,

    // waits and pacing
    Wait,
    WaitWindow,
    Pause,

    // assertions
    ExpectExists,
    ExpectText,
    ExpectEnabled,
    ExpectDisabled,
    ExpectTreeContains,
    ExpectWindow,
    ExpectNo,

    // presentation
    Narrate,

    // capture (#1421 Milestone 2)
    Screenshot,
    ScreenshotDialog,
}
