# Issue #1332: Discord Server Subscriptions — Setup Reference

## Overview

StoryCAD Pro ($2.99/mo) is delivered entirely via Discord Server Subscriptions. Discord handles billing, role assignment, and channel gating. No custom backend, no app store integration, no identity bridging.

---

## Discord Server Subscriptions — Key Facts

| Property | Value |
|----------|-------|
| Minimum price per tier | **$2.99/month** |
| Maximum price per tier | $199.99/month |
| Maximum number of tiers | 3 |
| Billing frequency | Monthly only (no annual option) |
| Revenue to server owner | ~84% (after 10% Discord fee + ~6% payment processing) |
| iOS mobile purchases | Additional ~30% to Apple on top of Discord's cut |
| Payout minimum (first) | $100 earned |
| Payout minimum (subsequent) | $25 |
| Payout frequency | Monthly |

## Eligibility Requirements

### Server Owner
- US-based (US bank account + US identification)
- EIN works for nonprofits (instead of SSN) — StoryBuilder Foundation is 501(c)(3)
- 18+ years old
- 2FA enabled
- Email and phone verified
- Account in good standing

### Server
- Community features enabled
- MFA required for moderation actions
- No recent policy violations

## Revenue Split Comparison

| Platform | Developer Share | Notes |
|----------|----------------|-------|
| Discord Server Subscriptions | ~84% | Desktop/web; less on mobile |
| Microsoft Store | 85% | Non-game apps |
| Apple App Store | 85% | Small Business Program (<$1M revenue) |

## Nonprofit Considerations

- Discord has **no special provisions** for nonprofits
- 501(c)(3) uses Server Subscriptions under the same terms as anyone
- EIN provided instead of SSN for Stripe/tax setup
- **Sales tax**: Discord collects from buyers and remits
- **Income tax**: Server owner (Foundation) is responsible
- **Reporting**: Stripe issues 1099-K when IRS thresholds are met ($600+)
- **Tax-exempt status**: Subscription income may have implications for unrelated business income — consult tax advisor

## Available Perks (What Discord Supports)

- **Premium roles**: Automatically assigned/removed on subscribe/cancel
- **Exclusive channels**: Text, voice, forum, media — gated behind subscriber roles
- **Custom emoji and stickers**: Tier-specific
- **Off-platform perks**: Can describe external benefits (Discord doesn't enforce delivery)
- **Tiered access**: Higher tiers include all lower-tier perks

## Setup Steps

### 1. Check Eligibility and Enable
- Server Settings → Monetization / Server Subscriptions
- If not available, check eligibility and apply (region, compliance)

### 2. Prepare Roles and Channels
- Create `StoryCAD Pro` role
- Create "StoryCAD Pro" category with gated channels:
  - `pro-chat`, `feature-voting`, `critique-exchange`, `workshops`, `resource-drops`, `beta-access`, `pro-voice`
- Set permissions: only Pro role (and admin) can view/post

### 3. Configure Subscription Tier
- Server Settings → Server Subscriptions → Create new tier
- Name: **StoryCAD Pro**
- Price: **$2.99/month**
- Role granted: `StoryCAD Pro`
- Description: Include all perk categories (influence, critique, learning, recognition, community, resources, early access)

### 4. Test
- Use "View Server As Role" to verify:
  - What a non-subscriber sees (public channels only)
  - What a Pro subscriber sees (public + Pro channels)
- Adjust permissions as needed

### 5. Publish
- Publish the tier in the Subscriptions UI
- Pin "How to upgrade" message in announcements

## Reference Links

- [Announcing Server Subscriptions](https://discord.com/blog/server-and-creator-subscriptions)
- [Server Subscriptions for Members](https://support.discord.com/hc/en-us/articles/4415163187607-Server-Subscriptions-for-Members)
- [Server Shop for Server Owners](https://creator-support.discord.com/hc/en-us/articles/10423011974551-Server-Shop-For-Server-Owners-and-Admins)
- [Creator Revenue FAQ](https://creator-support.discord.com/hc/en-us/articles/10424143128343-Creator-Revenue-FAQ)
- [Monetization Terms](https://support.discord.com/hc/en-us/articles/5330075836311-Monetization-Terms)
- [Monetization Policy](https://support.discord.com/hc/en-us/articles/10575066024983-Monetization-Policy)
- [Localized Pricing](https://support.discord.com/hc/en-us/articles/4407269525911-Localized-Pricing-on-Discord)
- [How to Set Up Discord Subscriptions (Zapier guide)](https://zapier.com/blog/discord-server-subscriptions/)
