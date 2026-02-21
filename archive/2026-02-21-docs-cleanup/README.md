# Documentation Cleanup Archive - February 21, 2026

This archive contains documentation that was moved from the `docs/` folder during the cleanup on February 21, 2026.

## Cleanup Summary

**Date:** February 21, 2026  
**Reason:** Remove outdated documentation and consolidate archive structure  
**Criteria:** Files older than February 18, 2026 (with exceptions for timeless architecture docs) were archived

### Statistics

| Metric | Before | After |
|--------|--------|-------|
| docs/ folders | 17 | 8 |
| docs/ files (approx) | 4,650 | ~120 |
| Archive folders created | - | 12 |

## Archive Structure

```
archive/2026-02-21-docs-cleanup/
├── README.md (this file)
├── docs-archive-merged/          # Previous docs/archive contents
│   ├── 2026-01-16-pre-final-scan/
│   ├── 2026-02-20-documentation-refresh/
│   ├── 2026-02-20-technical-debt-plan-refresh/
│   ├── build_logs/
│   ├── implementation_summaries/
│   ├── logs/
│   ├── status/
│   └── technical_debt/
├── outdated-features/            # Old feature docs (2/13)
├── outdated-guides/              # Old phase guides (2/13)
├── outdated-reports/             # Old reports
├── outdated-logs/                # Log files from reports/logs
├── outdated-root-docs/           # AI_PROJECT_INDEX.md, etc.
├── outdated-planning/            # Old planning docs (2/13)
├── outdated-status/              # Old status docs (2/13)
├── outdated-fixes/               # docs/fixes folder
├── outdated-implementation/      # docs/implementation folder
├── outdated-images/              # docs/images folder
├── outdated-plugins/             # docs/plugins folder
├── outdated-rebuild/             # docs/rebuild folder
├── outdated-resources/           # docs/resources folder
└── knowledge/                    # docs/knowledge (4520 files)
```

## What Was Kept in docs/

### Current Documentation (as of Feb 21, 2026)

| Folder | Files Retained |
|--------|---------------|
| **ai/** | AI_MASTER_CONTEXT.md |
| **architecture/** | All 20 files (ADRs 000-011, patterns, engineering rules, etc.) |
| **features/** | 10 recent files: authentication.md, FEATURES_AND_FUNCTIONS_SURFACE.md, MUGEN_*.md, NEW_FEATURES_2_5_SUMMARY.md, ROM_VALIDATION_FEATURE12.md, SMART_LAUNCHER*.md |
| **guides/** | 12 recent files: AI_QUICK_START.md, API_DOCUMENTATION.md, CHEAT_TABLE_SOURCES.md, CLOUD_SIGNATURE_DATABASE.md, CONTRIBUTING.md, LINUX_SETUP.md, MACOS_SETUP.md, MEMORY_INTELLIGENCE.md, PLATFORM_FEATURE_MATRIX.md, QUICK_REFERENCE.md, STEAM_DECK.md, WINDOWS_ONLY_FEATURES.md |
| **planning/** | 3 recent files: LESSONS_LEARNED.md, POST_DEBT_FEATURES_ROADMAP.md, ROM_EMULATOR_ENHANCEMENT_PROPOSAL.md |
| **plans/** | All 7 files (current refactoring plans and roadmap) |
| **reference/** | All 3 files (quick references) |
| **reports/** | 6 key files: CODEBASE_LOCK.md, COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_20.md, COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_21.md, MANAGER_PATTERN_REFACTORING_SUMMARY.md, PERFORMANCE_BASELINE.md, PROJECT_METRICS.md |
| **status/** | 5 recent files: CHANGELOG.md, DEVELOPMENT_STATUS.md, REFACTORING_SUMMARY_2026-02-20.md, RELEASE_STATUS.md, TECHNICAL_DEBT_AUDIT_2026-02-20.md |
| **root** | CURRENT_DOCUMENTATION_INDEX.md |

## What Was Archived

### Features (outdated-features/)
- achievements.md, ai-assistant.md, analytics.md, character-api.md
- docker_setup.md, game-library.md, ikemen_integration.md
- mugen-plugins.md, RETROARCH_INTEGRATION.md
- mugen/ subfolder (integration guide, quick ref, testing guide)
- overlays/ subfolder (overlays dialogs implementation)

### Guides (outdated-guides/)
- Phase guides: PHASE2_MIGRATION_GUIDE.md through PHASE7_DEVELOPMENT_GUIDE.md
- DIALOG_INTEGRATION_GUIDE.md, EMULATOR_SETUP.md, PLUGIN_SDK.md, PHASE6_OPTIONAL_FEATURES_GUIDE.md

### Reports (outdated-reports/)
- AVALONIA_BEHAVIORS_MIGRATION_RESEARCH.md
- COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_15.md (superseded by newer audits)
- github_issues_to_create.md
- INTEGRATION_TEST_REPORT.md

### Planning (outdated-planning/)
- ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md
- character_development_integration_plan.md
- DECISION_LOG.md (old version, not the current DECISIONS_LOG.md)
- DOCUMENTATION_MAINTENANCE.md
- Various MUGEN/IKEMEN analysis documents
- MUGEN_ENHANCEMENT_PROPOSAL.md, MUGEN_IKEMEN_ENHANCEMENT_ROADMAP.md
- NEXT_STEPS.md
- PERFORMANCE_LOGGING_MIGRATION_PLAN.md
- PROJECT_COMPLETION_ROADMAP.md
- REMAINING_TODOS.txt, REMAINING_TODOS_LIST.txt
- V2_3_0_IMPLEMENTATION_PLAN.md
- V2_FEATURE_ROADMAP.md, V2_FEATURE_ROADMAP_temp.md
- visual_resources_implementation_summary.md
- visual_resources_integration_plan.md

### Status (outdated-status/)
- PHASE7_PROGRESS.md
- PHASE7_REQUIRED_ENHANCEMENTS.md
- PROJECT_COMPLETION_STATUS.md

### Other Folders
- **fixes/**: ARCHITECTURE_RESTORATION_DETAILED.md
- **implementation/**: AUDIO_OPTIMIZER_WINDOWS_CORE_AUDIO_IMPLEMENTATION.md
- **images/**: 2 image files
- **knowledge/**: 4,520 files (data sources, intelligence docs)
- **plugins/**: WAVE1-8 implementation docs
- **rebuild/**: 13 files (quick-start, troubleshooting, phase guides, etc.)
- **resources/**: GLOSSARY.md

### Root Docs (outdated-root-docs/)
- AI_PROJECT_INDEX.md (outdated, referenced non-existent folders)
- sync-config.yaml (old config)

## Recovery

If you need to recover any archived file:

```bash
# Example: Restore a specific file
cp archive/2026-02-21-docs-cleanup/outdated-guides/PLUGIN_SDK.md docs/guides/

# Example: Restore an entire folder
cp -r archive/2026-02-21-docs-cleanup/outdated-features/mugen docs/features/
```

## See Also

- `docs/CURRENT_DOCUMENTATION_INDEX.md` - Current active documentation index
- `AGENTS.md` (root) - Main project guidelines
- `archive/2026-02/` - Previous archive from February 2026
