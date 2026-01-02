# Security & Risk Assessment

**Status**: ✅ Active
**Last Updated**: January 2, 2026
**Maintained By**: Security Team
**Next Review**: February 1, 2026

---

## Table of Contents

- [Threat Model Overview](#threat-model-overview)
- [Attack Surface Analysis](#attack-surface-analysis)
- [Data Security](#data-security)
- [External Service Security](#external-service-security)
- [AI & Knowledge Base Security](#ai--knowledge-base-security)
- [Plugin Security](#plugin-security)
- [Mitigations Implemented](#mitigations-implemented)
- [Known Limitations](#known-limitations)
- [Incident Response](#incident-response)

---

## Threat Model Overview

### Assets Protected

| Asset | Sensitivity | Location |
|-------|-------------|----------|
| Game library data | Medium | SQLite database |
| User preferences | Low | SQLite database |
| API keys | **Critical** | Environment variables / appsettings.json |
| Session tokens | High | In-memory only |
| Knowledge base content | Medium | Local filesystem |
| Cloud sync credentials | High | Encrypted storage |

### Threat Actors

| Actor | Motivation | Capability |
|-------|------------|------------|
| Malicious plugin | Data exfiltration | Medium |
| Local malware | Credential theft | High |
| Network attacker | API key interception | Medium |
| Insider threat | Data modification | Low |

---

## Attack Surface Analysis

### Primary Attack Vectors

| Vector | Risk Level | Mitigation Status |
|--------|------------|-------------------|
| SQL Injection | Low | ✅ EF Core parameterization |
| API Key Exposure | Medium | ✅ Environment variables |
| Privilege Escalation | Low | ✅ OAuth2 per service |
| Man-in-the-Middle | Medium | ✅ HTTPS enforced |
| Plugin Code Execution | Medium | ⚠️ Sandboxing planned |
| Web Search Data Injection | Medium | ✅ Content sanitization |
| Memory Scraping | High | ⚠️ Windows-only limitation |

### Input Validation

| Input Source | Validation | Status |
|--------------|------------|--------|
| User text (AI prompts) | Sanitized before LLM | ✅ |
| File paths | Path traversal prevention | ✅ |
| Game import data | Schema validation | ✅ |
| Web search results | HTML stripping, markdown only | ✅ |
| Plugin data | Interface contracts | ✅ |

---

## Data Security

### At Rest

| Data Type | Encryption | Backup |
|-----------|------------|--------|
| SQLite database | SQLCipher (optional) | ✅ Cloud sync |
| Knowledge base (.md) | None (plaintext) | ✅ Cloud sync |
| Configuration | None (local only) | ❌ Manual |
| Logs | None | ❌ Local only |

### In Transit

| Communication | Protocol | Certificate |
|---------------|----------|-------------|
| External APIs | HTTPS/TLS 1.2+ | System trust store |
| Cloud sync | HTTPS | Service provider |
| Discord RPC | Local IPC | N/A |
| Steam/GOG APIs | HTTPS | Service provider |

### Data Retention

| Data | Retention | Deletion |
|------|-----------|----------|
| Game sessions | Indefinite | Soft delete |
| AI conversation history | Session-scoped | Memory cleared on exit |
| Web search cache | 24 hours | Automatic expiry |
| Knowledge base | User-managed | Manual delete |

---

## External Service Security

### API Key Management

| Service | Key Storage | Rotation Policy |
|---------|-------------|-----------------|
| OpenAI/Groq | Environment variable | User-managed |
| IGDB | Environment variable | Annual |
| SteamGridDB | Environment variable | Annual |
| Discord | Environment variable | On compromise |
| RetroAchievements | Environment variable | User-managed |

### Service Authentication

| Service | Auth Method | Token Lifetime |
|---------|-------------|----------------|
| Steam | OAuth2 PKCE | Session |
| GOG | OAuth2 | Session |
| Epic | OAuth2 | Session |
| Discord | OAuth2 + RPC | Session |
| Cloud Sync | OAuth2 | 1 hour (refresh) |

### Rate Limiting

| Service | Rate Limit | Handling |
|---------|------------|----------|
| OpenAI/Groq | Varies by tier | Polly retry + circuit breaker |
| IGDB | 4 req/sec | Request queue |
| SteamGridDB | 60 req/min | Request queue |
| RetroAchievements | 120 req/min | Request queue |

---

## AI & Knowledge Base Security

### LLM Security

| Concern | Mitigation |
|---------|------------|
| Prompt injection | Input sanitization, role separation |
| Data leakage | No PII in prompts |
| Model hallucination | RAG grounding with local knowledge |
| API key exposure | Server-side only, never client |

### Web Search Security

| Concern | Mitigation | Status |
|---------|------------|--------|
| Malicious content injection | HTML stripping, markdown conversion | ✅ |
| Tracking/fingerprinting | No cookies, user-agent rotation | ✅ |
| Phishing links | URL validation, domain allowlist (planned) | ⚠️ |
| Content poisoning | Knowledge base review (manual) | ⚠️ |

### Knowledge Base Security

| Concern | Mitigation |
|---------|------------|
| Path traversal | Restricted to `%LOCALAPPDATA%/SaveStateReborn/` |
| Filename injection | Regex sanitization of query strings |
| Large file attacks | Size limits on saved content |
| Sensitive data storage | No credentials in knowledge base |

---

## Plugin Security

### Current Model

| Aspect | Implementation | Risk |
|--------|----------------|------|
| Loading | Automatic from `Plugins/` directory | Medium |
| Isolation | None (shared AppDomain) | High |
| Permissions | Full trust | High |
| Code signing | Not implemented | High |

### Planned Improvements (V2)

- [ ] Plugin sandboxing via separate processes
- [ ] Permission manifest system
- [ ] Code signing verification
- [ ] Capability-based access control

### Known Plugin Risks

| Plugin | Risk | Mitigation |
|--------|------|------------|
| MugenManagerPlugin | Manual HttpClient | Planned fix |
| ItchGameProviderPlugin | Manual HttpClient | Planned fix |
| All plugins | Full filesystem access | V2 sandboxing |

---

## Mitigations Implemented

### Infrastructure

- ✅ **EF Core Parameterization**: All database queries use parameterized statements
- ✅ **IHttpClientFactory**: Centralized HTTP client management with lifecycle control
- ✅ **Polly Resilience**: Retry, circuit breaker, and timeout policies on all external calls
- ✅ **Configuration Validation**: `.ValidateOnStart()` for all options
- ✅ **Structured Logging**: No sensitive data in logs (API keys, tokens masked)

### Application

- ✅ **Result Pattern**: No exception abuse for business logic
- ✅ **Guard Clauses**: Input validation at all entry points
- ✅ **Soft Delete**: Data recovery possible, audit trail maintained
- ✅ **Session Isolation**: Conversation context per-session, cleared on exit

### External

- ✅ **OAuth2 PKCE**: Secure authentication flows for game platforms
- ✅ **HTTPS Enforcement**: All external API calls over TLS
- ✅ **Token Refresh**: Automatic token refresh for long-running sessions

---

## Known Limitations

### Architectural

| Limitation | Impact | Planned Resolution |
|------------|--------|-------------------|
| Game memory reading Windows-only | Platform limitation | None (Windows feature) |
| No end-to-end encryption for backups | Data exposure risk | V2 encryption layer |
| Voice recognition requires cloud API | Privacy concern | Local model option (V2) |
| Plugin full trust | Security risk | V2 sandboxing |

### Operational

| Limitation | Impact | Workaround |
|------------|--------|------------|
| API keys in config file | Exposure if file shared | Use environment variables |
| Knowledge base not encrypted | Local access risk | Filesystem permissions |
| No audit logging | Compliance gap | V2 audit trail |

---

## Incident Response

### Detection

| Indicator | Detection Method |
|-----------|------------------|
| Unusual API usage | Rate limit triggers |
| Failed authentication | Structured logging |
| Database corruption | SQLite integrity checks |
| Plugin misbehavior | Exception monitoring |

### Response Procedures

1. **API Key Compromise**
   - Revoke key at provider immediately
   - Rotate to new key
   - Audit recent API usage

2. **Database Corruption**
   - Restore from cloud sync backup
   - Run `PRAGMA integrity_check`
   - Report issue for investigation

3. **Plugin Security Issue**
   - Remove plugin from `Plugins/` directory
   - Restart application
   - Report to plugin maintainer

---

## Compliance Considerations

| Regulation | Applicability | Status |
|------------|---------------|--------|
| GDPR | EU users | ⚠️ Data export needed |
| CCPA | CA users | ⚠️ Privacy policy needed |
| COPPA | Under-13 users | ✅ Not targeted at children |

---

**Document Owner**: Security Team
**Last Security Audit**: January 2, 2026
**Next Scheduled Review**: February 1, 2026
