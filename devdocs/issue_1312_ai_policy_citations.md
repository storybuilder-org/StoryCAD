# AI Policy Citations — Issue #1312

Collected references for updating the StoryBuilder Foundation Responsible AI Usage Policy.

## Writers' Organizations

### SFWA (Science Fiction & Fantasy Writers Association)
- **URL**: https://www.sfwa.org/2023/06/13/current-statement-on-ai-ml-use/
- **Date**: June 13, 2023 (last modified December 30, 2025)
- **Summary**: Principles-based approach applying established ethical guidelines to AI/ML. Barred staff/volunteers from using generative AI tools. Banned generative AI from Nebula Award eligibility.

### SFWA — Nebula Awards LLM Disclosure Policy
- **URL**: https://www.sfwa.org/2025/12/19/press-release-december-19-2025/
- **Date**: December 19, 2025
- **Summary**: SFWA Board guidance on Nebula Awards and LLM use. Key policy: "Works that used LLMs at any point during the writing process must disclose this upon acceptance of the nomination, and the nature of the technology's use will be made clear to voters on the final ballot."

### Authors Guild
- **URL**: https://authorsguild.org/advocacy/artificial-intelligence/
- **Summary**: Mass copying of copyrighted books for AI training does not qualify as fair use. Filed class action lawsuit against OpenAI and Microsoft. Launched "Human Authored" certification mark. Advocates for voluntary licensing arrangements controlled by authors, not publishers or tech companies.

### PEN America
- **URL**: https://pen.org/report/speech-in-the-machine/
- **Summary**: Published "Speech in the Machine: Generative AI's Implications for Free Expression." Warns that displacement of writers by machines threatens not only creative artists but the public as a whole. Frames AI as a free expression issue.

### National Writers Union (NWU)
- **URL**: https://nwu.org/issues-we-care-about/generative-ai/
- **Summary**: Calls for worker-responsive standards on corporate AI use. Submitted testimony to U.S. Copyright Office. Supports California AB 412 requiring AI developers to disclose training data sources.

### Romance Writers of America (RWA)
- **URL**: https://rwacontest.org/index.php/ai-policy/
- **Summary**: Contest policy prohibits AI-generated narrative, dialogue, or descriptive text without substantive human creative input. Permits AI assistive tools like grammar checks. (Note: RWA has filed for bankruptcy; organization's future uncertain.)

### Horror Writers Association (HWA)
- **URL**: https://file770.com/draft-ai-policy-roils-horror-writers-association/
- **Summary**: Draft policy distinguishing "AI-assisted" (grammar/editing tools) from "AI-generated" (works originating from AI). Generated controversy among members. No finalized public policy found.

### Society of Authors (UK)
- **URL**: https://www.thebookseller.com/news/society-of-authors-writes-to-ai-firms-demanding-appropriate-remuneration-and-consent-for-authors
- **Date**: August 2024
- **Summary**: Wrote directly to Microsoft, Google, OpenAI, Apple, and Meta demanding consent and compensation before using authors' works for AI training. 2024 survey found a third of translators and a quarter of illustrators already losing work to AI. Part of Creative Rights in AI Coalition opposing UK government's proposed opt-out model.

### Writers Guild of America (WGA)
- **URL**: https://www.wga.org/contracts/know-your-rights/artificial-intelligence
- **Date**: 2023 contract (after 148-day strike)
- **Summary**: Landmark AI protections: AI cannot be credited as a writer, AI-generated material cannot be considered "literary material," companies cannot require writers to use AI, companies must disclose AI-generated materials given to writers.

## Publishers

### Penguin Random House
- **URL**: https://www.penguin.co.uk/discover/articles/penguins-approach-to-generative-artificial-intelligence
- **Date**: October 2024
- **Summary**: First major publisher to add AI training prohibition language to copyright pages globally: "no part of this book may be used or reproduced in any manner for the purpose of training artificial intelligence technologies or systems."

### HarperCollins
- **URL**: https://authorsguild.org/news/harpercollins-ai-licensing-deal/
- **Summary**: First major publisher AI licensing deal — limited backlist non-fiction titles, opt-in for authors, $2,500 each to author and publisher for a 3-year license, with guardrails limiting output to 200 consecutive words or 5% of book text. Mixed reactions from author community.

### Hachette Book Group
- **URL**: https://www.hbgauthorresources.com/landing-page/info-for-authors-home/info-for-authors-author-ai-faq/
- **Date**: March 20, 2025 (last updated)
- **Summary**: Most transparent of the Big Five. Published detailed FAQ distinguishing "operational" AI uses (metadata, plagiarism detection, marketing) from "creative" AI uses. Requires authors to disclose AI tool usage at submission. Offers contract language protecting works from AI training on request. Employees/contractors prohibited from inputting author content into public AI models. Filed lawsuit against Google over Gemini training (January 2026).

### Macmillan / Pan Macmillan (UK)
- **URL**: https://www.panmacmillan.com/ai-at-pan-macmillan
- **Date**: 2024
- **Summary**: Seven AI principles centered on "We are a publisher of human stories, by human writers." Opposes unauthorized AI training. Offers optional contract language on request (May 2024). Active in Creative Rights in AI Coalition (CRAIC) and Publisher's Association AI Taskforce. Partners with Holtzbrinck's CHAPTR for ethical operational AI tools.

### Simon & Schuster
- **URL**: None (no public AI policy page)
- **Summary**: Weakest position of the Big Five. Spokesperson says they "take these concerns seriously" but no published contract protections or policy page. Notably absent from May 2024 report of publishers adding AI contract language. Dutch subsidiary controversially announced AI translation program for commercial fiction (November 2024). Under KKR private equity ownership since August 2023.
- **Source**: https://www.npr.org/2025/06/28/nx-s1-5449166/authors-publishers-ai-letter

## Other Organizations

### Creative Commons
- **URL**: https://creativecommons.org/ai-and-the-commons/
- **Summary**: Notable outlier. Acknowledges AI training is "often permitted by copyright" and CC license conditions have "limited application to machine reuse." Developing "CC preference signals" framework. Maintains that fair use (U.S.) and text/data mining exceptions (EU) likely protect AI training uses.

## Legal Landscape

### Google AI Summary (search result, February 2026)
> "Using AI to write a book isn't illegal—but the legal protections are limited unless you put in the work yourself. AI is a tool, not a co-author. If you want lasting ownership of your creative work, your human input is what makes it legally yours."

This aligns with U.S. Copyright Office guidance: purely AI-generated works are not copyrightable, but human-authored works that use AI as a tool can be. Copyright attaches to human expression, not AI output.

## YouTube References

- https://www.youtube.com/watch?v=RpCziNOhggU — Jane Friedman reports on a BookPub article about AI and copyright for authors.

## Collaborator and Copyrighted Works in LLM Training Data

A key concern for writers: will AI tools inject fragments of existing copyrighted works into their output?

There are two distinct issues:

1. **LLM training data** — Commercial LLM providers (OpenAI, Anthropic, etc.) trained their models on broad datasets that almost certainly include published copyrighted works. This is the basis of the Authors Guild lawsuits. When prompted, an LLM *may* generate text that echoes or closely paraphrases training material, though providers claim this is rare and unintentional.

2. **Collaborator's architecture** — Collaborator does not store, index, or retrieve from any copyrighted works. It does not maintain a vector database of novels. It sends structured prompts to an LLM API and receives responses. Collaborator is designed to assist with story *structure and outlining*, not to generate prose. This architectural choice minimizes the risk of copyrighted material surfacing in output.

**Proposed policy language**: "Collaborator does not store, index, or retrieve from any copyrighted works. However, the underlying AI models were trained on broad datasets that may include published works. Collaborator is designed to assist with story structure, not to generate prose, which minimizes this risk."

## Consensus

The overwhelming consensus across writers' organizations:
- **Consent required** — AI companies should not use copyrighted works for training without explicit permission
- **Compensation owed** — Authors must be fairly compensated when their works are used
- **Transparency demanded** — AI training datasets must be disclosed
- **Human authorship protected** — AI should not replace or undermine human creative credit
- **Copyright enforcement** — Existing copyright law should be enforced, not weakened with new exceptions
