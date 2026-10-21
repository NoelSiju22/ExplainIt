# ExplainIt Feature Expansion

Implemented features: 1-25, 27 and 28. Feature 26 (selected-text explanation) is intentionally skipped.

## Before running
1. Run `FeatureExpansion.sql` against the existing ExplainIt Supabase/PostgreSQL database.
2. Restart the ASP.NET Core backend.

Example user secret:

## Included
- Document status and attention states
- Deadline management and overdue states
- In-app reminders
- Simple-language explanation workspace
- What/why/action/consequence explanation fields
- Important terms
- PDF preview
- Document timeline/events
- Advanced search across terms/actions/requirements/tags
- TXT export and browser Print/PDF export
- Version snapshots
- Version comparison
- Tags
- Trash/restore/permanent delete
- Favorites/important documents
- Recently viewed documents
- Shareable explanation summary links
- Security/ownership checks on document endpoints
- Onboarding experience
- AI summary/structured analysis
- AI term extraction
- AI action extraction
- AI deadline extraction
- Ask ExplainIt
- Explanation levels: Simple / Detailed / Very detailed
- Next-step workflow
- Document action board
- Reminder creation

## Intentionally excluded
Feature 26: explain selected text. No selected-text/highlight feature has been added.

> Current implementation: ExplainIt uses offline rule-based NLP. No OpenAI/Gemini API key is needed. See `EXPLAINIT_NLP_SETUP.md` for capabilities and limitations.
