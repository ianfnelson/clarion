# Clarion - Claude Development Guide

This guide provides context for AI assistants (Claude) working on the Clarion codebase.

## Project Overview

Clarion is a .NET library that fetches and parses stock market announcements from Investegate. It provides a clean, typed interface for retrieving article summaries and full article content.

**Key Technologies:**
- .NET 10.0
- AngleSharp (HTML parsing)
- xUnit (testing)

**Repository:** https://github.com/ianfnelson/clarion

## Architecture

### Core Components

1. **ClarionClient** (`Clarion/ClarionClient.cs`)
   - Main entry point for consumers
   - Factory method `Create()` sets up default HttpClient and provider
   - Delegates to `IArticleProvider` for actual fetching

2. **Provider Pattern** (`Clarion/Providers/`)
   - `IArticleProvider` - Interface for article sources
   - `InvestegateProvider` - Implementation for Investegate.co.uk
   - Designed for future extensibility to other sources

3. **Models** (`Clarion/Models/`)
   - `ArticleSummary` - Lightweight list representation
   - `Article` - Full article with HTML and text content
   - Both use required properties with init-only setters

### Code Conventions

- C# 12 features enabled (ImplicitUsings, Nullable)
- All classes are sealed unless designed for inheritance
- Use required properties for mandatory fields
- XML documentation comments for public APIs
- Async/await with CancellationToken support
- UTC for all timestamps

### Testing

Tests are in `Clarion.Tests/` using xUnit. The project uses:
- Unit tests for parsing logic
- Integration tests against live Investegate data (when appropriate)

## Development Workflow

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### NuGet Packaging

The project is configured for NuGet publishing:
- Package ID: `Clarion`
- Version managed in `Clarion.csproj`
- Source Link enabled for debugging
- Symbol packages (snupkg) generated

## Common Tasks

### Adding a New Article Source

1. Create new provider in `Clarion/Providers/{SourceName}/`
2. Implement `IArticleProvider`
3. Add parser for source's HTML structure
4. Add tests in `Clarion.Tests/`
5. Consider adding factory method to `ClarionClient` if needed

### Modifying Models

- Keep models immutable (init-only setters)
- Maintain XML docs for all public properties
- Consider backward compatibility for NuGet consumers
- Update both `Article` and `ArticleSummary` if changes apply to both

### Working with HTML Parsing

- AngleSharp is used for parsing HTML
- Parsing logic lives in provider implementations
- Be defensive: sources may change their HTML structure
- Consider null checks and fallbacks

## CI/CD

GitHub Actions workflow handles:
- Build verification
- Test execution
- NuGet package publishing
- NuGet feed: GitHub Packages

## Design Principles

1. **Simplicity** - Keep the API surface small and intuitive
2. **Immutability** - Prefer immutable data structures
3. **Testability** - Design for easy testing
4. **Extensibility** - Provider pattern allows multiple sources
5. **Robustness** - Handle HTML changes and network issues gracefully

## Current State

The library currently:
- ✅ Fetches article summaries by ticker
- ✅ Retrieves full article content
- ✅ Parses HTML to plain text
- ⏳ Only supports Investegate (designed for extension)

## Future Considerations

- Additional article sources (RNS, PR Newswire, etc.)
- Caching/rate limiting
- Retry policies
- Search/filtering capabilities
- Historical data access

## Branch Strategy

- `main` - Production-ready code
- Feature branches use `feat/`, `fix/`, `chore/` prefixes
- PRs required for main branch

## Questions?

Refer to:
- Project README.md for user documentation
- Tests for usage examples
- GitHub Issues for known problems/enhancements
