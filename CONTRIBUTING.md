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

## Code Style

Code style preferences and rules for Mcpifier are defined in [.editorconfig](.editorconfig) and are automatically evaluated on build.

### Code Cleanup

You can run this command to automatically apply code style preferences and rules to your code:

```bash
dotnet format
```

Run this before committing to ensure your code adheres to the project's formatting standards.

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
