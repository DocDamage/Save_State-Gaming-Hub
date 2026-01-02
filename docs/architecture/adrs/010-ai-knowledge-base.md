# ADR 010: AI Knowledge Base Architecture

## Status

Accepted

## Date

January 2, 2026

## Context

The application requires intelligent AI assistance that can:

1. Answer questions about gaming, emulators, cheats, and MUGEN
2. Learn from new information discovered online
3. Maintain context across conversations
4. Provide accurate, grounded responses (avoid hallucination)

Traditional stateless LLM calls cannot satisfy these requirements. We need a system that combines local knowledge retrieval with the ability to search the internet and persist new discoveries.

## Decision

Implement a **RAG + Web Search + Auto-Save** architecture with the following components:

### 1. Retrieval-Augmented Generation (RAG)

- Store knowledge as Markdown files in `%LOCALAPPDATA%/SaveStateReborn/KnowledgeBase/`
- Use `SqliteVectorStore` for semantic search indexing
- Query local knowledge before every AI request
- Inject relevant context into system prompts

### 2. Short-Term Memory (BMAD)

- Use `EnhancedShortTermMemory` for conversation context
- Maintain session-scoped conversation history
- Store recent exchanges for reference in subsequent queries

### 3. Web Search Fallback

- Implement `IWebSearchService` for internet queries
- Trigger web search when:
  - Local knowledge returns insufficient results
  - User explicitly mentions "search" or "internet"
- Use `HttpClient` with `IHttpClientFactory` for resilient HTTP calls

### 4. Auto-Save to Knowledge Base

- Automatically persist valuable search results as Markdown files
- Use `IKnowledgeBaseService.SaveToKnowledgeBaseAsync`
- Format: `Search_{timestamp}_{sanitized_query}.md`
- Immediately index new files for RAG availability

### 5. Central Orchestration

- Route all requests through `IAiOrchestrator`
- Apply Polly resilience policies (retry, circuit breaker, timeout)
- Support multiple LLM providers (OpenAI, Groq) with fallback

## Architecture Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                        User Query                             │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                     AiOrchestrator                            │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │ RAG: Knowledge │  │ BMAD: Short-   │  │ Web: Internet  │  │
│  │ Base Query     │  │ Term Memory    │  │ Search         │  │
│  └───────┬────────┘  └───────┬────────┘  └───────┬────────┘  │
│          │                   │                   │            │
│          ▼                   ▼                   ▼            │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              Combined Context Injection                  │ │
│  └─────────────────────────────────────────────────────────┘ │
│                           │                                   │
│                           ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │           LLM Provider (OpenAI/Groq)                     │ │
│  └─────────────────────────────────────────────────────────┘ │
│                           │                                   │
│                           ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │    Auto-Save Search Results to Knowledge Base           │ │
│  └─────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

## Consequences

### Positive

- ✅ Grounded responses using local knowledge (reduced hallucination)
- ✅ Continuous learning from web searches
- ✅ Session-aware conversations
- ✅ Resilient with fallback providers
- ✅ Extensible knowledge base (1,200+ cheat files already indexed)
- ✅ Semantic search for relevant context retrieval

### Negative

- ⚠️ Increased complexity in orchestration logic
- ⚠️ Storage growth from auto-saved content
- ⚠️ Potential for outdated knowledge if not refreshed
- ⚠️ Web search results may contain inaccurate information

### Risks Mitigated

- Prompt injection: Input sanitization before LLM calls
- Data poisoning: Manual review capability for knowledge base
- API costs: Semantic caching for repeated queries
- Provider outages: Multi-provider fallback

## Alternatives Considered

### 1. Stateless LLM Calls Only

- **Rejected**: Cannot maintain context or learn new information
- No knowledge persistence between sessions

### 2. Full Vector Database (Pinecone, Weaviate)

- **Rejected**: Overkill for local application
- Adds cloud dependency and cost
- SQLite vector store sufficient for scale

### 3. Fine-tuned Local Model

- **Rejected**: Requires significant compute resources
- Difficult to update with new information
- RAG provides similar benefits with flexibility

### 4. Manual Knowledge Entry Only

- **Rejected**: Too slow for dynamic information
- Web search auto-save enables continuous learning

## Implementation Files

| Component | File |
|-----------|------|
| Orchestrator | `src/SaveState.Infrastructure/Ai/AiOrchestrator.cs` |
| Web Search Service | `src/SaveState.Infrastructure/Ai/Services/WebSearchService.cs` |
| Knowledge Base Service | `src/SaveState.Infrastructure/Ai/Knowledge/MarkdownKnowledgeBaseService.cs` |
| Vector Store | `src/SaveState.Infrastructure/Ai/Knowledge/SqliteVectorStore.cs` |
| Short-Term Memory | `src/SaveState.Infrastructure/Ai/Memory/EnhancedShortTermMemory.cs` |
| Resilience Policy | `src/SaveState.Infrastructure/Ai/Resilience/AiResiliencePolicy.cs` |

## Compliance

- **ENGINEERING_RULES.md**: ✅ Uses IHttpClientFactory, Result pattern
- **ADR 001 (Clean Architecture)**: ✅ AI services in Infrastructure layer
- **ADR 007 (Result Pattern)**: ⚠️ Some methods still return null (tracked in debt audit)

## References

- [Retrieval-Augmented Generation (RAG)](https://arxiv.org/abs/2005.11401)
- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)
- [Polly Resilience Patterns](https://github.com/App-vNext/Polly)
