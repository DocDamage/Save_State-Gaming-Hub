# 🤖 AI Assistant & Knowledge Base

**Status**: ✅ Implemented
**Last Updated**: January 2, 2026
**Layer**: Core + Infrastructure
**Related**: [ADR 010](../architecture/adrs/010-ai-knowledge-base.md)

---

## Overview

The AI system provides intelligent responses using RAG, web search, and auto-learning.

### Key Features

- **RAG**: Retrieval from 1,200+ knowledge files
- **Web Search**: Automatic fallback for missing knowledge
- **Auto-Save**: Search results persisted for future queries
- **Multi-Provider**: OpenAI, Groq support with fallback

## Architecture

```
User Query → AiOrchestrator
                 │
    ┌────────────┼────────────┐
    ▼            ▼            ▼
  RAG         Memory      Web Search
    │            │            │
    └────────────┴────────────┘
                 │
                 ▼
            LLM Provider
```

## Implementation Files

| Component | File |
|-----------|------|
| Orchestrator | `Infrastructure/Ai/AiOrchestrator.cs` |
| Knowledge Base | `Infrastructure/Ai/Knowledge/MarkdownKnowledgeBaseService.cs` |
| Web Search | `Infrastructure/Ai/Services/WebSearchService.cs` |

## Knowledge Base Location

```
%LOCALAPPDATA%/SaveStateReborn/KnowledgeBase/
├── cheats/           # Game cheat databases
├── emulators/        # Emulator docs
├── internet-search/  # Auto-saved searches
└── custom/           # User knowledge
```

## Configuration

```json
{
  "Ai": {
    "Provider": "OpenAI",
    "DefaultModel": "gpt-4",
    "OpenAi": { "ApiKey": "${OPENAI_API_KEY}" },
    "Groq": { "ApiKey": "${GROQ_API_KEY}" }
  }
}
```

## Usage

```csharp
var response = await orchestrator.ProcessRequestWithContextAsync(
    sessionId,
    new AiRequest { Prompt = "SNES cheat codes" },
    ct
);
```

---

See [ADR 010](../architecture/adrs/010-ai-knowledge-base.md) for full architecture details.
