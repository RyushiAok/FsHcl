# Agent guidelines

- For semantic F# queries (type-check, symbol search, compile order), use the F# MCP with `FsHcl.slnx` as project context

## Format

- Run `dotnet fantomas` to format code (`src/`, `tests/`, `examples/`).
- Follow F# 10 coding style.
- Follow ASD-STE100 for technical English

## Docs

- Regenerate API references: run `pnpm generate:api` in `docs/`.
- When adding a package: update `filePages` in `scripts/generate-api-mdx.mts` and `docs/app/ui/nav/nav-data.ts`.
