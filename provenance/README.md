# Provenance — MATRIX V2

## Source Archive
- FINAL.zip: 475 files from 8 historical steps (steps 1-8)
- FINAL.MD: Technical specification (657 lines)
- V1 Domain.cs: 16-line scaffold (baseline)

## Key Sources Read
- artifacts-1of2/matrix-workspace.schema.json (691 lines) — V1 JSON Schema contract
- artifacts-1of2/CONTEXT.md — V1 project context
- artifacts-1of2/neo.sample.json — Sample workspace data
- MATRIX_V1_bundle/MATRIX_V1/Domain.cs — V1 domain scaffold
- MATRIX_V1_bundle/MATRIX_V1/matrix-workspace.json — V1 workspace sample

## New Implementation
All C# source files in src/, tests in tests/, Python tests in tools/,
schemas, fixtures, scripts, CI workflows, and documentation are NEW_IMPLEMENTATION
based on the V1 contract (matrix-workspace.schema.json).

## Excluded from Public Repo
- FINAL.zip (475 files) — contains historical project artifacts, not needed in V2 repo
- Historical step documents — preserved in provenance only
- No secrets, private topology, or credentials included
