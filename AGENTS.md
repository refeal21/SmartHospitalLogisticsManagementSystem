# Smart Hospital Logistics Management System

## First Principle

Build a powerful hospital logistics management system from zero to one. Do not build a demo shell. Every requirement, design, implementation, test, and repository action must serve real hospital logistics operations, BIM-based spatial operations, and long-term maintainability.

## Source Material Policy

- Use internal PPTs and company training documents only as local reference material.
- Do not commit original PPT files, exported images, company training files, customer data, credentials, server information, or raw internal notes.
- Commit only generated system code, sanitized project rules, architecture documents, tests, and necessary engineering configuration.

## Project Rules

- Read `docs/01-project-rules.md`, `docs/02-module-boundaries.md`, and `docs/03-top-level-architecture.md` before making architectural or product decisions.
- Keep development vertical-slice oriented: product workflow, domain model, API contract, frontend workflow, tests.
- Prefer stable, typed, auditable domain models over page-only mockups.
- Treat BIM, IoT, work orders, assets, energy, security, emergency response, and external hospital systems as integration boundaries with adapters.

## Engineering Discipline

- One coherent feature or document change per commit.
- Never commit secrets. Use environment variables and `.env.example`.
- Validate changes with build, type checks, tests, and browser checks when applicable.
- Use structured debugging: reproduction steps, expected vs actual behavior, hypotheses, verification, fix, regression test.
