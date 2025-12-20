# Security Policy

Thank you for helping keep summerdawn.ai and the Mcpifier project secure. This document describes how to report security vulnerabilities and how we handle reports.

## Reporting a Vulnerability

Preferred reporting methods (in order):

1. GitHub Security Advisory (recommended)
   - Create a private Security Advisory in this repository: https://github.com/summerdawn-ai/mcpifier/security/advisories
   - GitHub will allow private communication and coordinated disclosure.

2. Email (if you cannot use GitHub)
   - Send an encrypted email to: security@summerdawn.ai
   - If you cannot encrypt, send to the same address and mark the message "Private - Security Report".

If you use email, please do NOT open a public issue or discuss the issue publicly. Public disclosure risks exposing users before a fix is available.

## What to Include

Provide as much of the following as possible:

- A clear, concise summary of the issue and the impact.
- Reproduction steps or a minimal proof-of-concept (PoC) demonstrating the vulnerability.
- Affected version(s) of Mcpifier (commit SHA, release tag, or date).
- Environment details (OS, .NET runtime version, hosting mode: http or stdio).
- Any logs, request/response samples, or stack traces that help reproduce the issue.
- Your contact information and preferred method for follow-up (email/GitHub handle).
- If you are providing exploit code, please encrypt it when emailing (PGP).

We ask reporters to avoid providing full exploit code in a public forum.

## Our Response Process

- Acknowledgement: We will acknowledge receipt within 48 hours (business days).
- Triage: We will triage and assess the severity and priority of the issue.
- Fix: We will work to provide a fix or mitigation, and publish a patched release as soon as reasonably possible.
- Coordination: We aim to coordinate disclosure with the reporter. We may request additional information to reproduce and validate the issue.
- CVE: We will request a CVE identifier for applicable, high-impact issues.
- Disclosure timeline: We generally aim to publish an advisory within 90 days of disclosure, ideally sooner once a fix is available. Timelines may vary depending on complexity and impact.

## Public Disclosure

Please do not publicly disclose the vulnerability before we have issued a fix or we've agreed on coordinated disclosure terms. If you publicly disclose prematurely, we may be forced to accelerate mitigations and disclosure.

## Safe Harbor / Legal

We welcome responsible security research. Reporters acting in good faith to help improve the security of Mcpifier will not be pursued legally for their reporting activities, provided they do not violate other laws or intentionally destroy data.

## Contact / Follow-up

- GitHub Security Advisories: https://github.com/summerdawn-ai/mcpifier/security/advisories
- Email (alternative): security@summerdawn.ai

If you don't receive a timely response by the SLA above, please follow up on the same channel or open a (private) GitHub Security Advisory and reference your original report.

Thank you for helping keep Mcpifier safe.