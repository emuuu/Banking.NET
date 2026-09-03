# Contributing to Banking.NET

Thank you for considering contributing to Banking.NET.

## Reporting Issues

- **Bug Reports:** Use the [Bug Report template](https://github.com/emuuu/Banking.NET/issues/new?template=bug_report.yml) and include reproduction steps, expected/actual behavior, and your environment (.NET version, OS).
- **Feature Requests:** Use the [Feature Request template](https://github.com/emuuu/Banking.NET/issues/new?template=feature_request.yml) and describe the use case you're trying to solve.

## Development Setup

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Optional: a project in the [Commerzbank Developer Portal](https://developer.commerzbank.com/) with sandbox credentials for the Corporate Payments API. The integration tests run only when `COMMERZBANK_SANDBOX_CLIENT_ID` and `COMMERZBANK_SANDBOX_CLIENT_SECRET` are set and are skipped otherwise.

### Clone & Build

```bash
git clone https://github.com/emuuu/Banking.NET.git
cd Banking.NET
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

## Pull Request Guidelines

1. **Branch from `main`** — create a feature or fix branch (e.g. `feat/my-feature` or `fix/issue-42`).
2. **Write tests** — all new functionality should include unit tests.
3. **Fill out the PR template** — describe what changed and why.
4. **Keep PRs focused** — one logical change per PR. Avoid unrelated formatting or refactoring.
5. **Ensure CI passes** — all builds and tests must be green.
6. **Never commit credentials** — no client secrets, certificates, private keys or recorded API responses containing account data.

## Code Style

This project uses an `.editorconfig` for consistent formatting. Please ensure your editor respects it:

- 4 spaces indentation (no tabs)
- UTF-8 encoding, LF line endings
- PascalCase for public members, `_camelCase` for private fields
- `System` usings sorted first
- All code, comments, documentation and commit messages are written in English

## Commit Conventions

We follow [Conventional Commits](https://www.conventionalcommits.org/):

| Prefix | Purpose |
|--------|---------|
| `feat:` | New feature |
| `fix:` | Bug fix |
| `docs:` | Documentation only |
| `test:` | Adding or updating tests |
| `chore:` | Build, CI, dependencies |
| `refactor:` | Code change that neither fixes a bug nor adds a feature |

Example: `feat: add camt.054 notification reader`

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
