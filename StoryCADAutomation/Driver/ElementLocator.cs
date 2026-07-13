using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace StoryCADAutomation.Driver;

/// <summary>
///     Process-rooted element location with readiness waits. Search starts at the launched
///     process's top-level windows (main window, windowed popups/flyouts, and owned native
///     dialogs — Win32 common dialogs run in-process so they share the pid), never at the
///     desktop; that rule plus the foreground check is the design's input containment
///     (devdocs/issue_1421_dsl_design.md "Components").
/// </summary>
internal sealed class ElementLocator
{
    /// <summary>
    ///     AutomationId of the two runtime tree hosts. The XAML NavigationTree/TrashTree ids
    ///     never surface at runtime (ItemsRepeater creates no automation peer); rows live as
    ///     TreeItems under two Tree controls whose id is "ListControl" (#1420 runtime facts).
    /// </summary>
    private const string TreeHostAutomationId = "ListControl";

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan AbsencePollInterval = TimeSpan.FromMilliseconds(100);

    private readonly UIA3Automation _automation;
    private readonly int _processId;
    private readonly Action _ensureRunnable;

    /// <param name="ensureRunnable">
    ///     Called every poll; throws when the app died mid-run or the driver was torn down,
    ///     so waits fail fast instead of burning their whole timeout against a dead process.
    /// </param>
    public ElementLocator(UIA3Automation automation, int processId, Action ensureRunnable)
    {
        _automation = automation;
        _processId = processId;
        _ensureRunnable = ensureRunnable;
    }

    /// <summary>
    ///     Waits until the target is found AND satisfies <paramref name="need" /> (the design's
    ///     implicit wait: readiness, not existence). Throws AutomationStepException with the
    ///     last unmet condition on timeout.
    /// </summary>
    public AutomationElement WaitUntilReady(ElementAddress address, ReadinessRequirement need, TimeSpan timeout)
        => WaitUntilReadyCore(
            () =>
            {
                var element = TryFind(address, out var diagnosis);
                return (element, diagnosis);
            },
            address.ToString(), need, timeout);

    /// <summary>
    ///     Readiness wait scoped to <paramref name="container" />'s descendants plus any
    ///     process windows not present in <paramref name="windowsBefore" /> (a ComboBox popup
    ///     opens as a separate top-level window). Never searches the whole process: a
    ///     same-named SelectionItem elsewhere (a tree row, a list item on the page behind the
    ///     popup) must not be picked up instead of the container's item (batch-2 review SF-2).
    /// </summary>
    public AutomationElement WaitUntilReadyWithin(
        AutomationElement container, IReadOnlyList<AutomationElement> windowsBefore,
        ElementAddress address, ReadinessRequirement need, TimeSpan timeout)
    {
        var condition = ConditionFor(address)
                        ?? throw new AutomationStepException(
                            $"{address} cannot be scope-searched; use an AutomationId or Name address.");
        return WaitUntilReadyCore(
            () => (FindWithin(container, windowsBefore, condition),
                   $"no element matching {address} under the container or in a window opened after it expanded"),
            address.ToString(), need, timeout);
    }

    /// <summary>Snapshot of the process's current top-level windows, for scoped searches.</summary>
    public IReadOnlyList<AutomationElement> TopLevelWindows() => AppWindows();

    private AutomationElement WaitUntilReadyCore(
        Func<(AutomationElement? Element, string Diagnosis)> find, string target,
        ReadinessRequirement need, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var last = "never found";
        while (true)
        {
            _ensureRunnable();
            var (element, findDiagnosis) = find();
            if (element is null)
            {
                last = findDiagnosis;
            }
            else
            {
                var unmet = Unmet(element, need);
                if (unmet is null)
                {
                    return element;
                }

                last = unmet;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AutomationStepException(
                    $"{target} was not ready for {need} within {timeout.TotalSeconds:0.#}s: {last}. " +
                    "Readiness is found + enabled + on-screen + the pattern the verb needs " +
                    "(devdocs/issue_1421_dsl_design.md, Verb set v1).");
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    ///     Settle-then-check absence: polls the bounded window and passes only if the target
    ///     never appears. Deliberately does not inherit the implicit wait, which would invert
    ///     the semantics into wait-for-the-error (design, Assertions).
    /// </summary>
    public void VerifyAbsent(ElementAddress address, TimeSpan settle)
    {
        var deadline = DateTime.UtcNow + settle;
        while (true)
        {
            _ensureRunnable();
            if (TryFind(address, out _) is not null)
            {
                throw new AutomationStepException(
                    $"{address} appeared within the {settle.TotalSeconds:0.#}s settle window; expect-no passes only if the target never appears.");
            }

            if (DateTime.UtcNow >= deadline)
            {
                return;
            }

            Thread.Sleep(AbsencePollInterval);
        }
    }

    /// <summary>
    ///     Readiness wait that first makes the target reachable by expanding collapsed
    ///     Expanders: collapsed Expanders keep RichEdit content out of the UIA tree entirely
    ///     (#1420 expand-before-type fact), so a plain wait would time out without ever
    ///     finding the control. If a quick probe misses, collapsed Expanders in the app's
    ///     windows are expanded one at a time until the target turns up.
    /// </summary>
    public AutomationElement WaitReadyRevealing(ElementAddress address, ReadinessRequirement need, TimeSpan timeout)
    {
        _ensureRunnable();
        var element = TryFind(address, out _);
        if (element is not null)
        {
            ExpandCollapsedAncestorExpanders(element);
        }
        else
        {
            foreach (var expander in CollapsedExpanders())
            {
                TryExpand(expander);
                if (TryFind(address, out _) is not null)
                {
                    break;
                }
            }
        }

        return WaitUntilReady(address, need, timeout);
    }

    /// <summary>Single-shot find with a diagnosis of why nothing matched.</summary>
    public AutomationElement? TryFind(ElementAddress address, out string diagnosis)
    {
        try
        {
            switch (address.Kind)
            {
                case ElementAddressKind.AutomationId:
                    diagnosis = $"no element with AutomationId '{address.Value}' in any window of the app process";
                    return FindFirst(cf => cf.ByAutomationId(address.Value));
                case ElementAddressKind.Name:
                    diagnosis = $"no element with Name '{address.Value}' in any window of the app process";
                    return FindFirst(cf => cf.ByName(address.Value));
                case ElementAddressKind.TreePath:
                    return TryResolveTreePath(address.Value, out diagnosis);
                default:
                    diagnosis = $"unknown address kind {address.Kind}";
                    return null;
            }
        }
        catch (Exception)
        {
            // Transient UIA churn: a window or element vanished between enumeration and read.
            // The caller's poll loop retries; a real absence surfaces as the wait timeout.
            diagnosis = "UIA read failed transiently (window or element vanished mid-search)";
            return null;
        }
    }

    /// <summary>
    ///     Per-control click strategy for the known pattern-insufficient cases. Founding
    ///     example (2026-06-12): UIA SelectionItem on a nav tree row reported success without
    ///     navigating; only Invoke or a real click navigates. Hence tree rows are
    ///     Invoke-never-Select, and the root outline row, which exposes only ExpandCollapse
    ///     (#1420), falls through to a real pointer click.
    /// </summary>
    public static ClickStrategy RecommendClickStrategy(AutomationElement element)
    {
        try
        {
            if (element.Properties.ControlType.ValueOrDefault == ControlType.TreeItem)
            {
                return element.Patterns.Invoke.IsSupported ? ClickStrategy.InvokePattern : ClickStrategy.RealPointer;
            }

            if (element.Patterns.Invoke.IsSupported)
            {
                return ClickStrategy.InvokePattern;
            }

            if (element.Patterns.SelectionItem.IsSupported)
            {
                return ClickStrategy.SelectionItemPattern;
            }
        }
        catch (Exception)
        {
            // Unreadable patterns: treat like a plain surface and click it for real.
        }

        return ClickStrategy.RealPointer;
    }

    /// <summary>Snapshot for the driver's public API (UIA types stay inside the driver).</summary>
    public static ElementState Snapshot(AutomationElement element)
    {
        var isEnabled = false;
        var isOffscreen = true;
        var isFocusable = false;
        string? name = null;
        string? text = null;
        var controlType = "Unknown";
        try
        {
            isEnabled = element.Properties.IsEnabled.ValueOrDefault;
            isOffscreen = element.Properties.IsOffscreen.ValueOrDefault;
            isFocusable = element.Properties.IsKeyboardFocusable.ValueOrDefault;
            name = element.Properties.Name.ValueOrDefault;
            controlType = element.Properties.ControlType.ValueOrDefault.ToString();
            if (element.Patterns.Value.IsSupported)
            {
                text = element.Patterns.Value.Pattern.Value.ValueOrDefault;
            }
            else if (element.Patterns.Text.IsSupported)
            {
                text = element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
            }
        }
        catch (Exception)
        {
            // Partial snapshots are acceptable; the element may have vanished mid-read.
        }

        return new ElementState(isEnabled, isOffscreen, isFocusable, name, text, controlType);
    }

    /// <summary>
    ///     Top-level windows of the launched process only. Includes WinUI windowed popups
    ///     (menus, flyouts) and in-process native dialogs; excludes everything else on the
    ///     desktop by construction.
    /// </summary>
    private IReadOnlyList<AutomationElement> AppWindows()
    {
        try
        {
            return _automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(_processId));
        }
        catch (Exception)
        {
            return Array.Empty<AutomationElement>();
        }
    }

    private AutomationElement? FindFirst(Func<ConditionFactory, ConditionBase> condition)
    {
        foreach (var window in AppWindows())
        {
            var element = window.FindFirstDescendant(condition);
            if (element is not null)
            {
                return element;
            }
        }

        return null;
    }

    private AutomationElement? FindWithin(
        AutomationElement container, IReadOnlyList<AutomationElement> windowsBefore,
        Func<ConditionFactory, ConditionBase> condition)
    {
        try
        {
            var inContainer = container.FindFirstDescendant(condition);
            if (inContainer is not null)
            {
                return inContainer;
            }

            foreach (var window in AppWindows())
            {
                if (windowsBefore.Any(known => IsSameElement(known, window)))
                {
                    continue;
                }

                var element = window.FindFirstDescendant(condition);
                if (element is not null)
                {
                    return element;
                }
            }
        }
        catch (Exception)
        {
            // Transient UIA churn; the caller's poll loop retries.
        }

        return null;
    }

    private static Func<ConditionFactory, ConditionBase>? ConditionFor(ElementAddress address)
        => address.Kind switch
        {
            ElementAddressKind.AutomationId => cf => cf.ByAutomationId(address.Value),
            ElementAddressKind.Name => cf => cf.ByName(address.Value),
            _ => null, // tree paths need the staged resolution in TryResolveTreePath
        };

    private static bool IsSameElement(AutomationElement a, AutomationElement b)
    {
        try
        {
            return a.Equals(b); // FlaUI compares UIA runtime ids
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string? Unmet(AutomationElement element, ReadinessRequirement need)
    {
        try
        {
            if (need == ReadinessRequirement.Exists)
            {
                return null;
            }

            if (!element.Properties.IsEnabled.ValueOrDefault)
            {
                return "found but disabled";
            }

            if (element.Properties.IsOffscreen.ValueOrDefault)
            {
                return "found but off-screen";
            }

            return need switch
            {
                ReadinessRequirement.Interactive => null,
                ReadinessRequirement.ClickablePoint =>
                    element.TryGetClickablePoint(out _) || !element.BoundingRectangle.IsEmpty
                        ? null
                        : "found but has no clickable point",
                ReadinessRequirement.Invoke =>
                    element.Patterns.Invoke.IsSupported ? null : "found but does not expose the Invoke pattern",
                ReadinessRequirement.SetValue =>
                    !element.Patterns.Value.IsSupported ? "found but does not expose the Value pattern"
                    : element.Patterns.Value.Pattern.IsReadOnly.ValueOrDefault ? "found but its Value pattern is read-only"
                    : !element.Properties.IsKeyboardFocusable.ValueOrDefault ? "found but not keyboard-focusable"
                    : null,
                ReadinessRequirement.SelectionItem =>
                    element.Patterns.SelectionItem.IsSupported ? null : "found but does not expose the SelectionItem pattern",
                ReadinessRequirement.ExpandCollapse =>
                    element.Patterns.ExpandCollapse.IsSupported ? null : "found but does not expose the ExpandCollapse pattern",
                ReadinessRequirement.Toggle =>
                    element.Patterns.Toggle.IsSupported ? null : "found but does not expose the Toggle pattern",
                ReadinessRequirement.Focusable =>
                    element.Properties.IsKeyboardFocusable.ValueOrDefault ? null : "found but not keyboard-focusable",
                _ => $"unknown readiness requirement {need}",
            };
        }
        catch (Exception)
        {
            return "went stale during the readiness check";
        }
    }

    // --- tree path resolution -----------------------------------------------------------

    private AutomationElement? TryResolveTreePath(string path, out string diagnosis)
    {
        var segments = TreePathParser.Parse(path);
        diagnosis = $"no Tree control with AutomationId '{TreeHostAutomationId}' found " +
                    "(the runtime tree hosts; XAML NavigationTree/TrashTree ids never surface, #1420)";
        foreach (var window in AppWindows())
        {
            var hosts = window.FindAllDescendants(cf =>
                cf.ByAutomationId(TreeHostAutomationId).And(cf.ByControlType(ControlType.Tree)));
            foreach (var host in hosts)
            {
                var element = TryResolveUnderHost(host, segments, out var hostDiagnosis);
                if (element is not null)
                {
                    diagnosis = string.Empty;
                    return element;
                }

                diagnosis = hostDiagnosis;
            }
        }

        return null;
    }

    private AutomationElement? TryResolveUnderHost(
        AutomationElement host, IReadOnlyList<TreePathSegment> segments, out string diagnosis)
    {
        AutomationElement scope = host;
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var candidates = ChildRows(host, scope, out var structureDiagnosis);
            if (candidates is null)
            {
                diagnosis = structureDiagnosis;
                return null;
            }

            var matches = candidates.Where(c => NameOf(c) == segment.Name).ToList();
            if (matches.Count < segment.Index)
            {
                diagnosis = $"segment '{segment}' matched {matches.Count} row(s) under " +
                            $"'{(ReferenceEquals(scope, host) ? "the tree root" : NameOf(scope))}'";
                return null;
            }

            scope = matches[segment.Index - 1];
            if (i < segments.Count - 1)
            {
                // Children only realize (and only enter the UIA tree) once the parent row is
                // expanded, so descend expands as it goes.
                TryExpand(scope);
            }
        }

        diagnosis = string.Empty;
        return scope;
    }

    /// <summary>
    ///     Rows one level below <paramref name="scope" />. Handles both UIA shapes a WinUI
    ///     tree can present: nested (TreeItems parented under their parent TreeItem) and flat
    ///     (all realized rows are siblings under the host, depth conveyed by the Level
    ///     property). Which shape the live app presents gets pinned when the runner batch
    ///     drives the smoke script; if the tree turns out flat without Level support, this
    ///     returns null with a diagnosis instead of guessing depths from row order.
    /// </summary>
    private IReadOnlyList<AutomationElement>? ChildRows(
        AutomationElement host, AutomationElement scope, out string diagnosis)
    {
        diagnosis = string.Empty;
        var directChildren = scope.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem));
        if (directChildren.Length > 0)
        {
            if (!ReferenceEquals(scope, host))
            {
                return directChildren; // nested shape
            }

            // Host scope: in the nested shape these are exactly the root rows; in the flat
            // shape they are ALL realized rows, so filter to the shallowest level to avoid
            // binding a deep row that shares the root's name (the Hamlet sample has two
            // nodes named Hamlet).
            var levels = directChildren.Select(TryGetLevel).ToList();
            if (levels.All(l => l is null))
            {
                return directChildren; // no Level info: treat as nested root rows
            }

            var minLevel = levels.Where(l => l is not null).Min();
            return directChildren.Where((_, idx) => levels[idx] is null || levels[idx] == minLevel).ToList();
        }

        if (ReferenceEquals(scope, host))
        {
            diagnosis = "the tree host has no realized TreeItem rows";
            return null;
        }

        // No nested children: flat shape. Find the scope row in the host's flat row list and
        // take the following rows exactly one level deeper, stopping at the next sibling.
        var scopeLevel = TryGetLevel(scope);
        if (scopeLevel is null)
        {
            diagnosis = "the tree exposes neither nested TreeItems nor the Level property; " +
                        "path resolution below this row needs live pinning (runner batch)";
            return null;
        }

        var allRows = host.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem));
        var children = new List<AutomationElement>();
        var seenScope = false;
        foreach (var row in allRows)
        {
            if (!seenScope)
            {
                seenScope = row.Equals(scope);
                continue;
            }

            var level = TryGetLevel(row);
            if (level is null || level <= scopeLevel)
            {
                break;
            }

            if (level == scopeLevel + 1)
            {
                children.Add(row);
            }
        }

        if (!seenScope)
        {
            diagnosis = "could not relocate the parent row in the flat row list (rows re-realized mid-walk)";
            return null;
        }

        return children;
    }

    private static int? TryGetLevel(AutomationElement element)
    {
        try
        {
            var level = element.Properties.Level;
            if (level.IsSupported)
            {
                var value = level.ValueOrDefault;
                if (value > 0)
                {
                    return value;
                }
            }
        }
        catch (Exception)
        {
            // Property unsupported or element gone; caller treats null as "no Level info".
        }

        return null;
    }

    private static string NameOf(AutomationElement element)
    {
        try
        {
            return element.Properties.Name.ValueOrDefault ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    // --- Expander handling ----------------------------------------------------------------

    /// <summary>
    ///     All currently collapsed WinUI Expanders across the app's windows (matched by UIA
    ///     ClassName so ComboBoxes and tree rows, which also expose ExpandCollapse, are never
    ///     expanded as a side effect).
    /// </summary>
    private IEnumerable<AutomationElement> CollapsedExpanders()
    {
        foreach (var window in AppWindows())
        {
            AutomationElement[] expanders;
            try
            {
                expanders = window.FindAllDescendants(cf => cf.ByClassName("Expander"));
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var expander in expanders)
            {
                if (IsCollapsed(expander))
                {
                    yield return expander;
                }
            }
        }
    }

    private void ExpandCollapsedAncestorExpanders(AutomationElement element)
    {
        try
        {
            var walker = _automation.TreeWalkerFactory.GetControlViewWalker();
            var current = walker.GetParent(element);
            // Bounded walk: a page never nests anywhere near this deep.
            for (var depth = 0; current is not null && depth < 25; depth++)
            {
                if (string.Equals(current.Properties.ClassName.ValueOrDefault, "Expander", StringComparison.Ordinal)
                    && IsCollapsed(current))
                {
                    TryExpand(current);
                }

                current = walker.GetParent(current);
            }
        }
        catch (Exception)
        {
            // Ancestor walk is best-effort; the readiness wait that follows is the real gate.
        }
    }

    private static bool IsCollapsed(AutomationElement element)
    {
        try
        {
            return element.Patterns.ExpandCollapse.IsSupported
                   && element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.ValueOrDefault
                   == ExpandCollapseState.Collapsed;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void TryExpand(AutomationElement element)
    {
        try
        {
            if (IsCollapsed(element))
            {
                element.Patterns.ExpandCollapse.Pattern.Expand();
                Wait.UntilInputIsProcessed();
            }
        }
        catch (Exception)
        {
            // A refused expand shows up as the subsequent find/readiness failure, with a
            // better diagnosis than this call site could produce.
        }
    }
}
