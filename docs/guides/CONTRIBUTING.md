# 🤝 Contributing to SaveState Reborn

Welcome! We're excited that you're interested in contributing to SaveState Reborn. This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

### Quick Setup
```bash
git clone https://github.com/yourusername/SaveStateReborn.git
cd SaveStateReborn
dotnet restore
dotnet build
dotnet test
dotnet run --project src/SaveState.Presentation
```

## 📝 Code Standards

### Architecture Compliance
SaveState Reborn follows **Clean Architecture** principles:
- **Result Pattern**: Never return `null` - use `Result<T>` instead
- **CQRS**: Separate commands (write) from queries (read)
- **Async Safety**: Use `async Task`, avoid `async void` except in UI event handlers

### Naming Conventions
- **Classes**: PascalCase (e.g., `GameLibraryService`)
- **Methods**: PascalCase (e.g., `GetGamesAsync`)
- **Properties**: PascalCase (e.g., `Title`)
- **Private Fields**: camelCase with underscore (e.g., `_gameRepository`)

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SaveState.Core.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🔄 Pull Request Process

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/your-feature-name`)
3. **Commit** your changes (`git commit -m 'feat: add amazing feature'`)
4. **Push** to the branch (`git push origin feature/your-feature-name`)
5. **Open** a Pull Request

### PR Requirements
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All tests pass
- [ ] Code follows project standards
- [ ] Documentation updated

## 🐛 Issue Reporting

**Good Bug Report** includes:
- Clear title describing the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, etc.)
- Screenshots/logs if applicable

## 📚 Documentation

When contributing:
- Update relevant docs for new features
- Follow existing documentation patterns
- Test all code examples

## 🌐 Community

- **Issues**: [GitHub Issues](https://github.com/yourusername/SaveStateReborn/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/SaveStateReborn/discussions)
- **Documentation**: See [docs/](docs/) for detailed guides

---

*Thank you for contributing to SaveState Reborn! 🎮*