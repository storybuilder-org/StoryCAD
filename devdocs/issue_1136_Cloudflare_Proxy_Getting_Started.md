# Cloudflare Proxy for StoryCAD Collaborator

## Introduction
This document explains what a Cloudflare-based proxy is, why we use it for StoryCAD Collaborator's LLM workflows, and how to get started even if you have no prior Cloudflare experience.

---

## What is a Cloudflare Proxy?
A Cloudflare proxy is a service running at the Cloudflare edge (Cloudflare Workers) that sits between your app and the LLM provider (e.g., OpenAI, Anthropic). It:
- Receives API requests from clients (StoryCAD Collaborator).
- Verifies authentication and entitlements (JWT, rate limits).
- Forwards valid requests to the target LLM provider.
- Streams results back to the client.

### Why use a proxy?
- **Security:** Keeps provider API keys off client devices.
- **Control:** Centralized rate limiting, logging, and model routing.
- **Flexibility:** Swap providers or adjust prompts without redeploying the client.

---

## How Cloudflare Workers Work
Cloudflare Workers are JavaScript/TypeScript functions deployed on Cloudflare's global network.
- **Fast:** Code runs close to the user.
- **Scalable:** Automatically handles concurrent connections.
- **Affordable:** Pay for usage, not servers.

Workers can handle HTTP requests, maintain small amounts of state, and interact with other Cloudflare services like KV storage or Durable Objects.

---

## Getting Started

### Step 1: Sign up for Cloudflare
- Go to [https://dash.cloudflare.com/sign-up](https://dash.cloudflare.com/sign-up).
- Create a free account.

### Step 2: Install Wrangler CLI
Wrangler is the CLI tool to develop and deploy Workers.
```bash
npm install -g wrangler
wrangler login
```

### Step 3: Create a Worker
```bash
wrangler init storycad-collaborator-proxy
cd storycad-collaborator-proxy
```
Select **TypeScript** template if asked.

### Step 4: Write the Proxy Code
The Worker handles `/v1/chat/completions` requests, validates JWTs, and forwards to LLM APIs.

### Step 5: Configure Environment Variables
Set your LLM provider API keys and JWT public key in `wrangler.toml`:
```toml
[vars]
OPENAI_KEY="sk-..."
JWT_PUBLIC_JWK="{...}"
```

### Step 6: Deploy
```bash
wrangler publish
```

Your proxy is now live at `https://<your-worker>.<your-subdomain>.workers.dev`.

---

## Costs
- **Free Tier:** 100,000 requests/day, no monthly cost.
- **Paid Plans:** Starting ~$5/month for higher limits and advanced features.
- **KV Storage/Durable Objects:** Billed separately if used.

At StoryCAD's scale (thousands of users), expect to start free and move to a paid plan as usage grows.

---

## Phasing into StoryCAD Collaborator

### Phase 1: Development & Testing
- Deploy a dev proxy to handle test traffic.
- Point SK client in Collaborator to the dev proxy URL.

### Phase 2: Staging
- Deploy a staging proxy with rate limits and logging.
- Test with staging API keys and sample users.

### Phase 3: Production
- Deploy production proxy.
- Store API keys securely.
- Enable monitoring and alerts in Cloudflare dashboard.

### Phase 4: Scaling
- Add caching (KV) for repeated prompts.
- Implement model routing for performance/cost optimization.

---

## Next Steps
1. Set up your Cloudflare account.
2. Deploy a minimal Worker that logs incoming requests.
3. Add JWT verification.
4. Integrate with LLM providers and enable streaming.
5. Point StoryCAD Collaborator's SK client to the new proxy.