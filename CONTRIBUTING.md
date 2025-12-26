# Contributing to Mcpifier

We welcome contributions to Mcpifier! This document provides guidelines for contributing to the project.

## Welcome

Thank you for your interest in contributing to Mcpifier. Whether you're fixing a bug, adding a feature, improving documentation, or reporting an issue, your contributions are appreciated.

## Development Environment Setup

Please refer to the corresponding section in [README.md](README.md#development) to get started.

## How to Submit Changes

### Fork and Branch

1. Fork the repository on GitHub
2. Create a new branch from `main` for your changes:
```bash
git checkout -b feature/your-feature-name
```

Use descriptive branch names:
- `feature/add-new-tool-type` for new features
- `fix/header-forwarding-bug` for bug fixes
- `docs/update-readme` for documentation

### Making Changes

1. Make your changes in your branch
2. Follow the coding standards (see below)
3. Write or update tests as needed
4. Ensure all tests pass
5. Commit your changes with clear, descriptive commit messages

### Pull Request Process

1. Push your branch to your fork
2. Open a Pull Request against the `main` branch
3. Fill in the PR template with:
   - Description of the changes
   - Related issue numbers (if applicable)
   - Testing performed
4. Wait for code review
5. Address any feedback from reviewers
6. Once approved, a maintainer will merge your PR

## Coding Standards

This project follows specific C# coding conventions documented in [AGENTS.md](AGENTS.md). Key points:

- Use primary constructors for classes unless special initialization logic is needed
- Use camelCase for private fields without underscore prefix
- Use explicit access modifiers on all members
- Use explicit types for primitive types (not `var` for `bool`, `int`, `string`, etc.)
- Use collection expressions (`[]`, `[1, 2, 3]`) for creating collections
- Do not use fully qualified type names; add `using` statements instead
- Do not add unnecessary `using` statements

Please review [AGENTS.md](AGENTS.md) for complete coding style guidelines.

## Testing Requirements

- All new features must include unit tests
- Bug fixes should include tests that verify the fix
- Maintain or improve code coverage
- Tests must pass before PR can be merged
- Integration tests should be added for significant features

## Code Review Process

1. All submissions require review from at least one maintainer
2. Reviewers will check:
   - Code quality and adherence to standards
   - Test coverage
   - Documentation updates
   - Breaking changes
3. Address review comments promptly
4. Be open to feedback and discussion
5. Reviewers may request changes before approval

## Documentation

- Update README files if adding new features
- Add XML documentation comments for public APIs
- Update relevant documentation in `/docs` if applicable
- Keep code examples up to date

## Questions and Help

If you need help or have questions:

- Open an issue on GitHub for bugs or feature requests
- Start a discussion in GitHub Discussions for questions
- Check existing issues and discussions first

## License

By contributing to Mcpifier, you agree that your contributions will be licensed under the same license as the project (MIT License).
