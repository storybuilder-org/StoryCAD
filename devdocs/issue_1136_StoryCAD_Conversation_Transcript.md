# StoryCAD Collaborator & SaaS Architecture Conversation Transcript

This document summarizes the full design and planning conversation between Terry Cox and ChatGPT regarding the StoryCAD Collaborator plug-in, Cloudflare proxy setup, SaaS rollout strategy, and business modeling.

---

## 1. UNO Plug-in Architecture
- Discussed **UNO Platform** plug-in design for multi-platform deployment (Windows, macOS, iOS, WebAssembly).
- Noted App Store limitations: Apple forbids runtime plug-in downloads; plug-ins must be **bundled**.
- Windows allows more flexibility but should mirror Apple’s bundling strategy for parity.
- In-app purchases (IAPs) can be used to unlock bundled plug-ins across platforms.

---

## 2. Collaborator Plug-in Overview
- **StoryCAD** is a **WinUI 3 MVVM** app with `Shell.xaml` as its main window.
- The **Collaborator** plug-in provides LLM-assisted workflows for story development.
- Workflows guide users through structured creative tasks:
  - Idea → Concept → Premise
  - Premise → Problem (with protagonist/antagonist)
  - Character development via GMC (Goal, Motivation, Conflict)
  - Inner conflict generation and subplot creation
- **Collaborator** operates as a separate window within StoryCAD, hosting multiple workflow pages.
- Communication occurs through shared ViewModels and StoryCAD’s public API.

---

## 3. Integration with Semantic Kernel and Proxy
- Collaborator connects to LLMs through **Semantic Kernel (SK)**.
- SK requests are routed via a **Cloudflare Worker proxy** for security, scalability, and observability.
- Proxy functions:
  - Manage API keys securely.
  - Enforce rate limiting and authentication (JWTs).
  - Support streaming responses.
  - Route between OpenAI/Anthropic providers as needed.

**Flow:**
1. User triggers Collaborator workflow.
2. Collaborator → SK plugin → Cloudflare Worker.
3. Proxy validates JWT → forwards to LLM provider.
4. Response streamed back to Collaborator for UI binding.

---

## 4. Testing and Deployment Strategy
- **Unit tests:** Validate SK prompt logic and schema validation.
- **Integration tests:** Ensure Collaborator ↔ StoryCAD data consistency.
- **E2E tests:** Validate workflow creation from idea → problem → outline.
- **Proxy staging:** Run mock LLMs for deterministic testing.

---

## 5. Design Artifacts Generated
Documents prepared for AI coder integration:
- `StoryCAD_Collaborator_Spec.md` — full spec and design goals.
- `StoryCAD_Collaborator_ComponentMap.md` — component diagram (Mermaid).
- `StoryCAD_Collaborator_Sequence.md` — workflow interaction diagram.
- `StoryCAD_Collaborator_Schemas.json` — structured schema for AI output.
- `StoryCAD_Collaborator_Prompts.json` — LLM prompt templates.
- `StoryCAD_Collaborator_TestPlan.md` — testing plan.
- `WorkflowRunner.cs` — C# skeleton for workflow execution.
- `CollaboratorInterfaces.cs` — plugin/host interface.
- `SkProxyClient.cs` — Semantic Kernel HTTP client.
- `ProxyWorker_Skeleton.ts` — Cloudflare Worker boilerplate.

---

## 6. Cloudflare Proxy Setup (from Getting Started doc)
- Cloudflare Worker acts as a secure intermediary between Collaborator and external LLMs.
- Setup steps:
  1. Create Cloudflare account.
  2. Install Wrangler CLI.
  3. Deploy Worker with secret API keys.
  4. Add JWT validation logic.
  5. Connect Collaborator’s SK client to the proxy endpoint.
- Estimated costs: Free to $5/month for low-volume workloads.

---

## 7. SaaS Multiplatform Model
- UNO app targets Windows, macOS, iOS, and WebAssembly.
- Uses **in-app purchases** for monetization.
- CI/CD pipelines for cross-platform builds via GitHub Actions or Azure DevOps.
- Platform parity achieved via bundled plug-ins and uniform entitlement validation.
- Backend includes:
  - Cloudflare proxy.
  - Azure/AWS functions.
  - Persistent DB (CosmosDB or DynamoDB).
- Metrics tracked:
  - MRR, ARR, LTV, CAC, churn, NRR, payback, gross margin.

---

## 8. Business Model Program
Created a Python-based **interactive CLI tool** (`saas_modeler.py`) to model SaaS economics.

### Features:
- Input: pricing, churn, CAC, margin, costs.
- Output: MRR, ARR, LTV, CAC ratio, payback period, NRR, burn rate, break-even.
- Save and export scenarios (freemium, tiered, usage-based).

### Bundle included:
- `saas_modeler.py`
- `README.md`
- `preset_scenarios.csv`

---

## 9. Deliverables Summary
| Category | Deliverable | Description |
|-----------|--------------|--------------|
| Design | StoryCAD_Collaborator_Spec.md | Full plugin and architecture spec |
| Code Skeletons | WorkflowRunner.cs, SkProxyClient.cs | Core functional stubs |
| Proxy | ProxyWorker_Skeleton.ts | Cloudflare Worker bridge |
| Diagrams | ComponentMap.md, Sequence.md | System architecture and sequence flows |
| Testing | StoryCAD_Collaborator_TestPlan.md | QA and validation plan |
| Business | SaaS_Multiplatform_Getting_Started.md | SaaS deployment guide |
| Modeling | saas_modeler.py + README | Interactive SaaS financial modeler |

---

## 10. Next Steps
1. Integrate Collaborator proxy into StoryCAD for testing.
2. Deploy Cloudflare Worker with JWT auth.
3. Add entitlement validation for IAPs.
4. Use SaaS Modeler to validate pricing tiers.
5. Begin phased rollout to Windows/Mac stores.

---

**Author:** ChatGPT (GPT-5)  
**Client:** Terry Cox, StoryBuilder Foundation  
**Date:** 2025-10-15  
