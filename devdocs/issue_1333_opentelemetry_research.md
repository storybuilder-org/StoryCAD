# Issue 1333: OpenTelemetry Research for StoryCAD Usage Statistics

**Date**: 2026-04-02  
**Context**: StoryCAD (.NET 10, WinUI 3 / UNO Platform), ~2,000 users, small volunteer team  
**Backend**: ScaleGrid MySQL with existing `users`, `preferences`, `versions` tables  
**Goal**: Evaluate OpenTelemetry for collecting session times, outline metadata, feature usage, and element counts

---

## 1. What Is OpenTelemetry and How Does It Work for Desktop/.NET Applications?

### Overview

OpenTelemetry (OTel) is a CNCF open-source observability framework that provides vendor-neutral APIs, SDKs, and tools for generating, collecting, and exporting telemetry data. It was designed primarily for cloud-native server applications but has expanded to cover client-side apps (mobile, desktop, kiosks).

### The Three Pillars

| Pillar | What It Captures | .NET API | Relevance to StoryCAD |
|--------|-----------------|----------|----------------------|
| **Traces** | Request/operation flow as "spans" with start time, end time, attributes, parent-child relationships | `System.Diagnostics.ActivitySource` / `Activity` | **Medium** -- could model sessions and outline-open/close as spans |
| **Metrics** | Numerical measurements: counters, histograms, gauges | `System.Diagnostics.Metrics.Meter` | **High** -- natural fit for element counts, feature use counts, session durations |
| **Logs** | Structured log events | `Microsoft.Extensions.Logging` + OTel bridge | **Low** -- StoryCAD already has NLog + elmah.io for error logging |

### Which Pillars Matter for Usage Statistics?

**Metrics** are the strongest fit. Usage statistics (element counts, feature use counts, session duration) are fundamentally measurements, not request traces. OTel metrics support:

- **Counters** -- monotonically increasing values (feature use count, elements added)
- **Histograms** -- distribution of values (session duration, outline open time)
- **UpDownCounters** -- values that go up and down (current element count by type)

**Traces** could model sessions as spans (a session span containing child outline-open spans), but this is an awkward fit. Traces are designed for distributed request flows, not "user had the app open for 45 minutes." The span model adds complexity without clear benefit for StoryCAD's use case.

### .NET SDK Packages

| Package | Purpose |
|---------|---------|
| `OpenTelemetry` (1.15.0) | Core SDK -- TracerProvider, MeterProvider |
| `OpenTelemetry.Api` (1.15.1) | API surface for instrumentation |
| `OpenTelemetry.Extensions.Hosting` | DI integration (`AddOpenTelemetry()`) |
| `OpenTelemetry.Exporter.Console` | Debug/development exporter |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | OTLP exporter (standard wire protocol) |

The .NET OTel SDK builds on native .NET APIs (`System.Diagnostics.ActivitySource` for traces, `System.Diagnostics.Metrics.Meter` for metrics), so instrumentation code uses standard .NET types rather than OTel-specific ones. This is a design advantage -- you can instrument with standard .NET APIs and optionally wire up OTel export later.

---

## 2. How Would OTel Collect the Specific Data StoryCAD Needs?

### Mapping Proposed Tables to OTel Concepts

#### sessions table (session_start, session_end, clock_time, cpu_time)

**OTel approach**: A trace span for the session, or a histogram metric recording session duration at session end.

```csharp
// Metrics approach (simpler)
var meter = new Meter("StoryCAD.Usage");
var sessionDuration = meter.CreateHistogram<double>("session.duration.seconds");

// At session end:
sessionDuration.Record(elapsed.TotalSeconds, 
    new KeyValuePair<string, object>("user_id", userId));
```

**Problem**: OTel metrics are aggregated (summed, histogrammed) before export. You lose individual session records. A histogram tells you "the average session was 23 minutes" but not "user X had a session from 2:00 to 2:45 on Tuesday." If you need per-session records (which the `sessions` table implies), OTel metrics are the wrong tool.

**Traces approach**: Model a session as a span with start/end times and attributes. This preserves individual records but requires a trace-oriented backend (Jaeger, Tempo) rather than MySQL.

**Verdict**: Neither OTel pillar maps cleanly to "write a row per session to MySQL."

#### outline_sessions table (outline_guid, open_time, close_time, elements_added/edited/deleted)

Same problem. This is per-event data. OTel metrics would aggregate the element counts across all outlines. OTel traces could model each outline-open as a child span, but again, this requires a trace backend.

#### outline_metadata table (genre, story_form, element_counts_by_type)

**OTel approach**: Could use an UpDownCounter per element type, with attributes for genre/story_form. This is a reasonable fit for aggregate statistics ("across all users, 60% of outlines are Mystery genre") but loses per-outline detail.

#### feature_usage table (feature_name, use_count)

**OTel approach**: Best fit. A Counter per feature, incremented on each use.

```csharp
var featureCounter = meter.CreateCounter<long>("feature.usage");
featureCounter.Add(1, new KeyValuePair<string, object>("feature", "NarrativeTool"));
```

**Problem**: Same aggregation issue. OTel exports the total count, not per-session breakdowns. The proposed `feature_usage` table has a `session_id` foreign key, implying per-session granularity that OTel metrics do not naturally provide.

### Summary of Fit

| Proposed Table | OTel Fit | Issue |
|---------------|----------|-------|
| `sessions` | Poor | Per-session rows need individual records, not aggregates |
| `outline_sessions` | Poor | Per-outline-open rows need individual records |
| `outline_metadata` | Partial | Aggregate stats work; per-outline detail does not |
| `feature_usage` | Partial | Aggregate counts work; per-session counts do not |

---

## 3. OTel Pipeline: Collection to Storage

### Standard Pipeline

```
Desktop App (OTel SDK) 
    --> OTLP (gRPC or HTTP)
        --> OTel Collector (optional intermediary)
            --> Backend (Jaeger, Prometheus, Grafana Tempo, vendor SaaS)
```

### Can OTel Export Directly to MySQL?

**No.** There is no standard MySQL exporter for OpenTelemetry. The standard exporters target:

- OTLP endpoints (Jaeger, Grafana Tempo, vendor backends)
- Prometheus (metrics pull model -- requires a running Prometheus server)
- Console (debugging only)
- Vendor-specific (Azure Monitor, Datadog, New Relic, etc.)

You could write a **custom exporter** that implements the OTel exporter interface and writes to MySQL, but at that point you have built most of a custom telemetry pipeline anyway, with OTel adding ceremony rather than value.

### Would StoryCAD Need an OTel Collector?

If using the standard OTel pipeline, yes. The Collector is a service that receives telemetry from apps and routes it to backends. It handles batching, retry, and filtering.

**Hosting cost and requirements**:
- Minimum ~4 GB RAM node
- Needs to be always-on and internet-accessible
- For StoryCAD's scale (~2,000 users), a $5-10/month VPS could handle it
- But it is a new service to maintain, monitor, and secure
- The team would also need a trace/metrics backend (Jaeger, Prometheus, or a SaaS)

**The OTel documentation itself states**: for small teams (under 10 engineers) without platform/SRE expertise, the operational overhead of self-hosted observability often exceeds vendor costs.

### Could They Use OTel SDK for Structure, Then Write to MySQL?

Technically yes -- you could use OTel's `Meter` and `ActivitySource` APIs to instrument the code, then write a custom exporter that transforms OTel data into MySQL INSERT calls. But this approach:

- Requires writing a custom exporter (non-trivial)
- Loses the main OTel benefit (plug-and-play backends)
- Adds the OTel SDK dependency (~5 NuGet packages) for what amounts to a custom data pipeline
- The "structure" OTel provides (semantic conventions, attribute naming) could be replicated with a simple C# class hierarchy in far less code

---

## 4. Comparison: OpenTelemetry vs. Custom Telemetry

### What OTel Gives You

| Benefit | Value to StoryCAD |
|---------|-------------------|
| Vendor-neutral standard | Low -- StoryCAD has one backend (MySQL), no vendor to switch |
| Rich ecosystem of exporters | Low -- none target MySQL; StoryCAD does not use Jaeger/Prometheus |
| Auto-instrumentation for HTTP, DB, etc. | None -- StoryCAD is a desktop app, not a web service |
| Distributed tracing across services | None -- StoryCAD is a single desktop app |
| Standardized semantic conventions | Marginal -- helpful for naming consistency, but achievable without OTel |
| Community and tooling | Marginal -- primarily relevant for server-side observability |

### What a Custom Approach Gives You

| Benefit | Value to StoryCAD |
|---------|-------------------|
| Direct MySQL writes via existing `IMySqlIo` | High -- reuses existing infrastructure |
| Per-session/per-outline granularity | High -- exactly the data model proposed |
| No new services to host | High -- no Collector, no trace backend |
| Simple C# code the team already understands | High -- stored procedures, `MySqlConnection` |
| Consent gating via existing `PreferencesModel` | High -- trivial to add a `bool UsageStats` flag |
| Minimal new dependencies | High -- no new NuGet packages needed |

### Is OTel Overkill for StoryCAD?

**Yes.** The evidence strongly points to OTel being a poor fit for this specific use case:

1. **Data model mismatch**: StoryCAD needs per-session, per-outline rows. OTel produces aggregated metrics or trace spans designed for distributed systems.
2. **No backend alignment**: OTel has no MySQL exporter. StoryCAD's backend is MySQL. Bridging that gap requires either a custom exporter or an entirely new observability stack.
3. **Operational overhead**: An OTel Collector and trace backend are new infrastructure for a team that currently manages one MySQL database.
4. **Scale mismatch**: OTel is designed for organizations generating millions of spans per second. StoryCAD has ~2,000 users generating maybe 100 sessions per day.
5. **Desktop app, not distributed system**: OTel's core strength -- correlating traces across microservices -- provides zero value to a single desktop application.

### Complexity/Maintenance Tradeoff

| Approach | Initial Effort | Ongoing Maintenance | Dependencies |
|----------|---------------|--------------------| -------------|
| **OTel full pipeline** | High (Collector setup, backend, custom exporter) | High (infrastructure monitoring) | ~5 NuGet packages + hosted services |
| **OTel SDK + custom MySQL exporter** | Medium (custom exporter code) | Medium (exporter maintenance through OTel version upgrades) | ~5 NuGet packages |
| **Custom telemetry (recommended)** | Low (new stored procedures + C# methods on `IMySqlIo`) | Low (same patterns as existing code) | 0 new packages |

### Simpler Alternatives That Follow OTel Standards

If the team wants some OTel alignment without the full framework:

- **Use OTel semantic conventions for naming** (e.g., `session.duration`, `feature.usage.count`) without importing the SDK
- **Use `System.Diagnostics.Metrics`** (built into .NET, no NuGet packages) to instrument internally, and write a simple periodic flush to MySQL -- this gives you the .NET-native metrics API without the OTel export machinery
- **Export to a JSON file** using OTel conventions, then batch-upload to MySQL on a schedule

None of these require the OTel SDK or Collector.

---

## 5. Privacy and Consent Integration

### How OTel Handles Opt-In/Opt-Out

OTel itself has **no built-in consent management**. The official documentation states that consent is the application developer's responsibility. The SDK provides:

- **Attribute redaction**: You can strip or hash attributes via processors before export
- **Opt-in attribute levels**: Some semantic conventions mark attributes as "opt-in" for privacy reasons
- **Sampling**: You can sample (drop a percentage of telemetry) to reduce data volume

But the actual consent gate -- "has this user agreed to share usage data?" -- must be implemented by the application. This is identical to the custom approach.

### How StoryCAD Should Handle Consent (Either Approach)

StoryCAD already has a consent model in `PreferencesModel` with `ErrorCollectionConsent` (elmah) and `Newsletter` flags. The approach is straightforward:

1. Add a `UsageStatsConsent` boolean to `PreferencesModel`
2. Add a checkbox to the Preferences dialog
3. Gate all telemetry collection/export on that flag
4. Store the consent in the `preferences` table (add a column)

This works identically whether using OTel or custom telemetry. OTel adds no advantage here.

### Data Minimization

For either approach:
- Collect only what is needed (session duration, element counts -- not content)
- Do not transmit outline text, character names, or plot details
- Aggregate on the client where possible (send element counts, not element details)
- Allow users to see what data is collected (transparency)

---

## 6. User ID: Integer vs. GUID for Anonymity

### Current State

The `users` table uses `user_id INT AUTO_INCREMENT` as the primary key. The table also stores `name` and `email`. The proposed usage tables (`sessions`, `outline_sessions`, etc.) reference `user_id`.

### Would a GUID Protect User Anonymity?

**No. Switching from integer to GUID is pseudonymity, not anonymity.**

The critical distinction:

| Concept | Definition | Example |
|---------|-----------|---------|
| **Anonymous** | No way to link data back to a person | Usage data has no user identifier at all |
| **Pseudonymous** | Data uses a consistent identifier that does not directly reveal identity, but can be re-linked via a lookup table | Usage data uses GUID `a3f7...`, users table maps `a3f7...` to `john@example.com` |
| **Identified** | Data directly contains identifying information | Usage data contains `john@example.com` |

Both integer `user_id = 42` and GUID `user_id = a3f7b2c1-...` are pseudonymous. As long as the `users` table maps either identifier to an email address, the user is identifiable by anyone with database access. The GUID simply prevents someone from guessing other user IDs by incrementing -- it does not provide anonymity.

### What Does Anonymity Actually Require?

True anonymity means the usage data **cannot be linked back to a specific person**, even by the database administrator. This requires one of:

1. **No user identifier in usage data at all** -- aggregate only
2. **A separate, unlinkable identifier** -- a `usage_id` that has no foreign key to the `users` table and no lookup table connecting them
3. **Differential privacy** -- adding statistical noise so individual records cannot be isolated (complex, overkill for StoryCAD)

### Approach Comparison

| Approach | Anonymity Level | Pros | Cons |
|----------|----------------|------|------|
| **Integer user_id (current)** | Pseudonymous | Simple, existing pattern, enables per-user analysis | Guessable IDs, directly linkable to email |
| **GUID user_id** | Pseudonymous | Non-guessable IDs | Still linkable to email via users table; requires schema migration; worse DB index performance |
| **Separate usage_id (GUID, no FK to users)** | Near-anonymous | Cannot join usage data to user identity; satisfies privacy-conscious users | Cannot correlate usage with user demographics; cannot delete usage data per user (GDPR right to erasure); cannot debug individual user issues |
| **No user identifier** | Anonymous | Maximum privacy | Cannot track longitudinal usage patterns; cannot correlate sessions to the same user; severely limits analytical value |

### Practical Analysis

The question "should we use GUID instead of integer?" is asking the wrong question. The privacy-relevant question is: **should usage data be linkable to user identity at all?**

If the answer is "yes, we need per-user usage tracking" (for support, for longitudinal analysis), then integer vs. GUID is irrelevant to privacy -- both are pseudonymous and linkable. The integer is simpler and already exists.

If the answer is "no, usage data should be anonymous," then neither integer nor GUID helps -- you need to omit the user identifier entirely or use an unlinkable `usage_id`.

### Recommendation

For StoryCAD's situation:

1. **Keep the integer `user_id`** for usage tables. It is simpler, already exists, and the privacy difference vs. GUID is zero when the `users` table exists.
2. **Gate collection on explicit consent** (the `UsageStatsConsent` flag). This is the real privacy protection -- users choose whether to participate.
3. **If stronger anonymity is desired later**, consider a separate `usage_id` (random GUID generated on the client, stored only in local preferences, never linked to the `users` table). This provides genuine unlinkability but sacrifices per-user analysis.
4. **Do not store usage data for users who have not consented.** This is more important than any identifier scheme.

---

## 7. Summary and Recommendation

### Recommendation: Custom Telemetry, Not OpenTelemetry

OpenTelemetry is a powerful framework for the wrong problem. StoryCAD needs to write per-session, per-outline usage rows to a MySQL database. OTel is designed to export aggregated metrics and distributed traces to observability backends. The mismatch is fundamental, not just a matter of configuration.

### Recommended Architecture

```
StoryCAD Desktop App
    |
    | (direct MySQL via existing IMySqlIo pattern)
    |
    v
ScaleGrid MySQL Database
    - sessions table
    - outline_sessions table  
    - outline_metadata table
    - feature_usage table
```

### Implementation Path

1. **Add `UsageStatsConsent` to PreferencesModel** and the Preferences dialog
2. **Create the four new MySQL tables** with stored procedures (following existing `spAddUser` / `spAddOrUpdatePreferences` patterns)
3. **Add methods to `IMySqlIo`**: `RecordSession()`, `RecordOutlineSession()`, `UpdateOutlineMetadata()`, `RecordFeatureUsage()`
4. **Add a `UsageTrackingService`** that collects data in memory during the session and flushes to MySQL on session end (or periodically)
5. **Gate all collection on `UsageStatsConsent`** -- if false, the service is a no-op
6. **Keep integer `user_id`** for simplicity; rely on consent as the privacy mechanism

### What to Tell Jake/Rarisma

OpenTelemetry was a reasonable suggestion -- it is the industry standard for application observability. However, OTel's strengths (distributed tracing, vendor-neutral export, aggregated metrics) do not align with StoryCAD's needs (per-session row inserts into a specific MySQL database). The custom approach:

- Reuses the existing `IMySqlIo` / `BackendService` / stored procedure pattern
- Requires zero new infrastructure (no Collector, no trace backend)
- Produces exactly the data model the issue specifies
- Adds zero new NuGet dependencies
- Is maintainable by the existing team

---

## Sources

- [OpenTelemetry Official Site](https://opentelemetry.io/)
- [OpenTelemetry .NET Metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/)
- [OpenTelemetry .NET Getting Started](https://opentelemetry.io/docs/languages/dotnet/getting-started/)
- [OpenTelemetry Client-Side Apps](https://opentelemetry.io/docs/platforms/client-apps/)
- [OpenTelemetry Handling Sensitive Data](https://opentelemetry.io/docs/security/handling-sensitive-data/)
- [OpenTelemetry .NET SDK on GitHub](https://github.com/open-telemetry/opentelemetry-dotnet)
- [.NET Observability with OpenTelemetry - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
- [OpenTelemetry Collector Documentation](https://opentelemetry.io/docs/collector/)
- [What Is OpenTelemetry?](https://opentelemetry.io/docs/what-is-opentelemetry/)
- [Pseudonymous Identity - Privacy Patterns](https://privacypatterns.org/patterns/Pseudonymous-identity)
- [OpenTelemetry NuGet Profile](https://www.nuget.org/profiles/OpenTelemetry)
- [MySQL Connector/.NET OpenTelemetry Tracing](https://dev.mysql.com/doc/connector-net/en/connector-net-programming-telemetry.html)
