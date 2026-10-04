namespace StoryCADAutomation.Driver;

/// <summary>
///     What "ready" means for a locating operation. Per the design's implicit-wait rule
///     (devdocs/issue_1421_dsl_design.md "Verb set v1"): readiness, not existence — the target
///     must be found AND enabled AND on-screen AND expose the pattern the calling verb needs.
///     Existence alone is not readiness; the known flake classes are readiness classes
///     (the 2026-06-12 navigation case; collapsed Expanders hiding RichEdit content).
/// </summary>
public enum ReadinessRequirement
{
    /// <summary>
    ///     Found only, no usability wait. Exists so the future "expect &lt;target&gt;
    ///     enabled|disabled" assertion can check state without waiting for usability.
    /// </summary>
    Exists,

    /// <summary>Found, enabled, and on-screen; no specific pattern.</summary>
    Interactive,

    /// <summary>Interactive plus a clickable point (real-pointer click realization).</summary>
    ClickablePoint,

    /// <summary>Interactive plus the Invoke pattern (pattern click realization).</summary>
    Invoke,

    /// <summary>Interactive plus a writable Value pattern and keyboard focusability (the set verb).</summary>
    SetValue,

    /// <summary>Interactive plus the SelectionItem pattern (the select verb).</summary>
    SelectionItem,

    /// <summary>Interactive plus the ExpandCollapse pattern (expand/collapse verbs).</summary>
    ExpandCollapse,

    /// <summary>Interactive plus the Toggle pattern (the toggle verb).</summary>
    Toggle,

    /// <summary>Interactive plus keyboard focusability (the focus verb).</summary>
    Focusable,
}

/// <summary>
///     How a click gets realized. The profile decision (which realization a verb uses) belongs
///     to the interpreter; the driver exposes both realizations plus
///     <see cref="StoryCADDriver.ResolveClickStrategy" /> for the known pattern-insufficient cases.
/// </summary>
public enum ClickStrategy
{
    /// <summary>UIA Invoke pattern.</summary>
    InvokePattern,

    /// <summary>UIA SelectionItem pattern.</summary>
    SelectionItemPattern,

    /// <summary>Real pointer input (eased move plus click).</summary>
    RealPointer,
}
