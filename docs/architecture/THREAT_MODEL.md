# Security & Risk Assessment

## Threat Model
- SQL injection: ✅ EF Core parameterization
- API key exposure: ✅ Environment variables only
- Privilege escalation: ✅ OAuth2 per service
- Data loss: ✅ WAL-enabled SQLite + cloud sync

## Known Limitations
- Game memory reading on Windows only
- No end-to-end encryption for backups
- Voice recognition requires cloud API