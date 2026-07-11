# PR #1470 review findings: Collaborator billing, client half

Reviewed 2026-07-10 against branch `billing` at commit `e857747c`, base `dev`.

## How this document connects to the rest of the billing work

Collaborator subscription billing is split across two issues in the Collaborator repo. Issue #89 is the client half, which this PR implements: the subscribe dialog, the calls to each platform's store, and the exchange of a store receipt for an access token. Issue #90 is the server half: the Cloudflare Worker endpoint that verifies receipts with Apple or Microsoft and issues the tokens. The Worker does not exist yet, so this review judged the client code against the written activation contract (the request and response shapes both sides agreed to) rather than against a running server.

Findings that concern the Worker's design rather than this PR's code were split into a companion document, `devdocs/issue_90_design_review.md` in the Collaborator repo. If you are addressing the items below, read that document too: it records decisions that shape what this client code will eventually talk to, including one new requirement (per-workflow cost tracking) that arrived during review.

## Prerequisite before anything else: fix the red CI on dev

Both failing checks on this PR fail on a test that is broken on the `dev` branch itself, not on anything this PR changed. The test `DeveloperBuild_EnvPresentAndUnpackaged_ReturnsTrueWithoutThrowing` came in with PR #1459 and fails on the `net10.0-desktop` test head, which is the head CI runs. This is tracked as issue #1471 and will be fixed on `dev` first. Until that lands, this PR cannot show green CI no matter what changes here.

## Issues to address, in the order they should be fixed

### 1. The contract documents the code cites do not exist in the repository

Six places in the new code point readers at `devdocs/iap/activation-contract.md`, `devdocs/iap/shim-contract.md`, or `devdocs/iap/testing.md`:

- `StoryCADLib/Services/Store/ProxyActivationClient.cs:10` (with the instruction "do not rename members")
- `StoryCADLib/Services/Store/StoreKitInterop.cs:11`
- `StoryCADLib/Services/Store/WindowsStoreContextAdapter.cs:24`
- `src/macos/StoreKitShim/StoreKitShim.swift:5`
- `StoryCADTests/Services/Store/StoreKitPayloadsTests.cs:9`
- `StoryCADTests/Services/Store/StoreKitInteropSmokeTests.cs:13`

No `devdocs/iap/` directory exists on this branch or anywhere in the repo. These documents define the wire format the Worker must implement byte-for-byte, so they need a committed home that both the client and Worker tracks read. They now have one: the StoryCADWiki holds them as dated snapshots (`raw/iap-recon-2026-07-10.md`, `raw/iap-activation-contract-2026-07-10.md`, `raw/iap-shim-contract-2026-07-10.md`, `raw/iap-testing-2026-07-10.md`), cataloged by the source page `wiki/repos/Collaborator/sources/iap-billing-docs.md`.

**Fix:** update the six citations to point at the wiki. Cite the source page (`StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md`) rather than a dated snapshot; the snapshots gain new dated versions when the contract is revised, and the source page always lists the current set.

### 2. The subscribe dialog appears in builds that have no store

`IStoreService.IsSupported` exists so that callers can hide store UI when no platform store is available (its own doc comment says so, `IStoreService.cs:17`). `CollaboratorService.ShowSubscribeDialogAsync` never checks it. In a build using `NullStoreService` (any desktop build outside a store bundle), a user who reaches the Collaborator gate gets a subscribe dialog with no plans listed and a Subscribe button that can never succeed.

**Fix:** check `IStoreService.IsSupported` before showing the dialog. When false, either show a one-line message saying Collaborator requires the store edition, or restore the previous behavior of logging and returning.

### 3. The free-trial wording is hard-coded as "1-week"

`SubscribeDialogViewModel.PriceSummary` (`SubscribeDialogViewModel.cs:82`) prints "1-week free trial, then {price}" whenever the store reports that an introductory offer exists. The store payload only carries a yes/no flag for the offer, not its length, so if the trial configured in App Store Connect or Partner Center turns out to be anything other than one week, the dialog will state wrong terms. Apple's review guideline 3.1.2 requires the purchase screen to state subscription terms accurately.

**Fix:** either extend the shim contract so the store reports the offer's actual period (the Swift side already has it available at `StoreKitShim.swift:175`; the JSON contract, the C# DTO, and the fixture tests would each gain one field), or reword the summary so it does not claim a length, for example "Includes a free trial, then {price}/month." This must be correct before store submission; it is harmless until then.

### 4. The Worker-facing HTTP client has no tests

`ProxyActivationClient` is the one class that speaks the activation contract over the wire: it decides which HTTP statuses count as a real refusal versus a try-again-later, and it parses the `ok`/`jwt`/`expiresAt`/`reason` response fields. It is exactly the code the "do not rename members" warning protects, and it has zero test coverage because it makes real HTTP calls.

**Fix:** unit-test it with a fake `HttpMessageHandler` (no network needed). Worth covering: a 200 with a token, a 403 with each refusal reason, an unexpected status (500, 429) raising the unreachable exception, an unparseable body raising the unreachable exception, and a success response missing `expiresAt`.

### 5. CI never compiles the Swift shim

The shim (`src/macos/StoreKitShim/StoreKitShim.swift`) is only compiled by the `_BuildAndCopyStoreKitShim` target during macOS publish packaging, which PR CI does not run. The smoke test that would load the built library skips itself when the library is absent, and it was skipped in this PR's CI run. Net effect: a Swift compile error would go unnoticed until someone cuts a release package.

**Fix:** add a step to the macOS CI job that runs `bash src/macos/StoreKitShim/build.sh` before the test step. The GitHub macOS runners already have `swiftc` and `lipo`. This also lets the smoke test actually run instead of skipping.

### 6. The entitlements method is missing from the interface

`MacStoreService` and `WindowsStoreService` both expose a public `GetCurrentEntitlementsAsync`, but `IStoreService` does not declare it. Callers holding the interface cannot reach it, so the two public methods serve only each service's own internals and the tests.

**Fix:** either add it to `IStoreService` (and give `NullStoreService` the empty-list implementation) or make it private on both services.

### 7. Quitting the app mid-activation shows an error message

The catch block in `StoreActivationService.RefreshActivationAsync` (`StoreActivationService.cs:140`) treats every exception from the Worker call as "activation unreachable" and posts a user-visible status message. That includes `OperationCanceledException`, so an activation cancelled because the app is shutting down would flash a network-trouble warning.

**Fix:** catch `OperationCanceledException` first and return quietly.

## Notes recorded, no change requested

- `CollaboratorService` resolves two services through `Ioc.Default` instead of constructor injection (`CollaboratorService.cs:132` and `:152`). The comments explain why (avoiding a wider constructor), and it matches the accepted precedent for such cases, but it adds to the debt issue #1063 tracks.
- `.gitignore` now ignores `wrangler.toml` everywhere in the tree. Fine as protection against committing local Worker config; if Worker configuration ever needs to live in a repo, the pattern to use is a committed `wrangler.example.toml`.
- `PreferencesModel.StoreUserGuid` is never populated by anything in this PR; the code handles that by logging and declining to activate, which is the right behavior. Provisioning that identifier is a prerequisite tracked on the server side; the sequencing constraint is recorded in the issue #90 companion document.

## What passed review

For anyone extending this code, these points were checked and are settled:

- The activation state machine matches the contract. A Worker that cannot be reached is never treated as a refusal, so a paying user is not locked out by a network blip; a still-valid cached token keeps them active through an outage. An expired subscription shows the resubscribe path rather than an error; a revoked one denies access.
- The code fails closed everywhere it should: unknown entitlement states from the store deny access rather than grant it, receipts that fail StoreKit's own verification never surface, and a missing user identifier blocks activation with a logged warning instead of sending an unbindable receipt.
- The platform split follows the repo's established pattern: compile-time guards match `Windowing.cs`, all WinRT calls sit behind a mockable adapter so the Windows logic unit-tests on any platform, and a missing StoreKit library on macOS falls back to the no-store service instead of crashing startup.
- The 52 new tests run headless on every head, follow the repo's naming convention, and their JSON fixtures match the contract document verbatim, including the malformed-payload and fail-closed paths.
- The purchase dialog contains the elements Apple's review checks for: per-plan price from the store, the auto-renewal disclosure, cancellation wording, Terms of Use and Privacy Policy links, and a Restore Purchases action.
- The three new preferences fields are additive with safe defaults; no story file format, public API, or existing behavior changes outside the Collaborator gate.
