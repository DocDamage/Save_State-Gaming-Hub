# SaveState Plugin Ecosystem Roadmap

**Created:** 2026-01-17
**Status:** Planned
**Total Plugins:** 45
**Estimated Timeline:** Q1-Q4 2026

---

## Executive Summary

This document outlines the complete plugin ecosystem for SaveState, organized into 8 waves of development. Each plugin is designed to extend SaveState's capabilities while maintaining a cohesive user experience.

---

## Wave 1: Foundation Plugins (Q1 2026 - Weeks 1-4)

*Focus: Quick wins, high value, showcase plugin system*

### 1.1 Game Timer & Alarm

**Capability:** `Utilities` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** Set playtime limits with configurable warnings.

**Features:**

- [ ] Daily/per-session time limits
- [ ] Warning notifications at 15/5/1 minute marks
- [ ] "Overtime" tracking mode
- [ ] Weekly playtime budgets
- [ ] Parental control PIN protection

**Technical Notes:**

- Use `IPlugin.OnGameLaunched` / `OnGameClosed` events
- Store settings in plugin data directory
- System tray notifications via Avalonia

---

### 1.2 Screenshot Sorter

**Capability:** `Utilities` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** Auto-organize screenshots by game and date.

**Features:**

- [ ] Watch folder for new screenshots
- [ ] Auto-move to `Screenshots/{GameTitle}/{Date}/`
- [ ] Rename with timestamp pattern
- [ ] Support Steam, Xbox, Windows screenshots
- [ ] Duplicate detection (hash-based)

**Technical Notes:**

- `FileSystemWatcher` for real-time detection
- Read Steam's `screenshots.vdf` for metadata
- Xbox screenshots from `C:\Users\{User}\Videos\Captures`

---

### 1.3 Discord Rich Presence Pro

**Capability:** `SocialFeatures` | **Complexity:** Easy | **Est. Effort:** 3 days

**Description:** Enhanced Discord status with artwork and details.

**Features:**

- [ ] Custom status messages per game
- [ ] Show playtime in session
- [ ] Display current achievement progress
- [ ] "Ask to Join" game invites
- [ ] Spectate mode for supported games

**Technical Notes:**

- Use existing `IDiscordPresenceService`
- Extend with per-game customization
- Store Discord Application IDs per game

---

### 1.4 Game Pass Leaving Soon

**Capability:** `Analytics` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** Alert when Game Pass titles are leaving the service.

**Features:**

- [ ] Fetch leaving soon list from Xbox API
- [ ] Match against user's library
- [ ] Dashboard widget with countdown
- [ ] Push notification 7/3/1 days before
- [ ] "Add to Backlog Priority" quick action

**Technical Notes:**

- API: `https://catalog.gamepass.com/sigls/v2`
- Match by title + fuzzy matching
- Cache with 6-hour TTL

---

### 1.5 Retro CRT Theme

**Capability:** `ThemeProvider` | **Complexity:** Easy | **Est. Effort:** 3 days

**Description:** Nostalgic CRT monitor aesthetic.

**Features:**

- [ ] Scanline overlay effect
- [ ] Phosphor glow on text
- [ ] Slight screen curvature
- [ ] VHS tracking noise (subtle)
- [ ] Retro font options (Fixedsys, Terminal)

**Technical Notes:**

- Implement `ITheme` interface
- XAML ResourceDictionary for colors/styles
- Optional shader effects for game art

---

## Wave 2: Game Provider Plugins (Q1 2026 - Weeks 5-8)

*Focus: Expand library detection*

### 2.1 itch.io Importer

**Capability:** `GameProvider` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Import games from itch.io app and website.

**Features:**

- [ ] OAuth2 authentication with itch.io
- [ ] Scan local itch.io app installations
- [ ] Pull owned games from API
- [ ] Import game metadata (description, tags, screenshots)
- [ ] Track itch.io game jams

**Technical Notes:**

- API: `https://itch.io/api/1/{key}/my-games`
- Local app path: `%APPDATA%\itch\apps`
- OAuth callback via local HTTP server

---

### 2.2 Humble Bundle Library

**Capability:** `GameProvider` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Import Humble Bundle purchases.

**Features:**

- [ ] OAuth2 login via Humble
- [ ] Pull complete purchase history
- [ ] Detect redeemed vs. unclaimed keys
- [ ] Link to direct download (DRM-free)
- [ ] Track Humble Choice subscription

**Technical Notes:**

- API: `https://www.humblebundle.com/api/v1/user/order`
- Requires session cookie extraction
- Map to Steam/GOG when keys are redeemed

---

### 2.3 Amazon/Prime Gaming

**Capability:** `GameProvider` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** Detect Prime Gaming claimed games.

**Features:**

- [ ] Detect Amazon Games Launcher
- [ ] Parse local game installations
- [ ] Pull claimed Prime Gaming offers
- [ ] Show in-game loot availability
- [ ] Notify of new claims

**Technical Notes:**

- Local path: `%LOCALAPPDATA%\Amazon Games\App.db`
- SQLite database parsing
- Web scrape for claiming status

---

### 2.4 Playnite Import

**Capability:** `Importer` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** One-click import from Playnite.

**Features:**

- [ ] Read Playnite's SQLite database
- [ ] Import games, playtime, completion status
- [ ] Map platforms and categories
- [ ] Import custom metadata fields
- [ ] Preserve Playnite IDs for future sync

**Technical Notes:**

- DB: `%APPDATA%\Playnite\library\games.db`
- Schema well-documented
- Incremental sync support

---

### 2.5 LaunchBox Import

**Capability:** `Importer` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** Import from LaunchBox/BigBox.

**Features:**

- [ ] Parse LaunchBox XML database
- [ ] Import games, emulators, media
- [ ] Map platforms to SaveState cores
- [ ] Import playlists as collections
- [ ] Preserve LaunchBox IDs

**Technical Notes:**

- XML: `{LaunchBox}\Data\Platforms\*.xml`
- Large media library consideration
- Platform mapping config

---

## Wave 3: Social & Streaming Plugins (Q2 2026 - Weeks 9-12)

*Focus: Content creation and social features*

### 3.1 OBS Integration

**Capability:** `UIExtension` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Deep OBS Studio integration.

**Features:**

- [ ] Auto-start/stop recording on game launch
- [ ] Switch scenes per game profile
- [ ] Update text sources (game title, playtime)
- [ ] Trigger replay buffer saves
- [ ] Stream deck-style quick actions

**Technical Notes:**

- Use OBS WebSocket protocol v5
- `obs-websocket` native support
- Scene collection per game category

---

### 3.2 Twitter/X Auto-Post

**Capability:** `SocialFeatures` | **Complexity:** Easy | **Est. Effort:** 3 days

**Description:** Share achievements and screenshots to Twitter/X.

**Features:**

- [ ] OAuth2 authentication
- [ ] Post on achievement unlock
- [ ] Share screenshots with game tags
- [ ] Customizable post templates
- [ ] Schedule posts for optimal times

**Technical Notes:**

- Twitter API v2
- Media upload via `/media/upload`
- Rate limit: 300 tweets/3 hours

---

### 3.3 Mastodon Integration

**Capability:** `SocialFeatures` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** Fediverse sharing support.

**Features:**

- [ ] Multi-instance support
- [ ] OAuth2 authentication
- [ ] Post with CW/content warnings
- [ ] Share via ActivityPub
- [ ] Follow gaming hashtags

**Technical Notes:**

- Standard Mastodon API
- Instance discovery via WebFinger
- Media attachments up to 40MB

---

### 3.4 Playtime Leaderboards

**Capability:** `SocialFeatures` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** Compare playtime with friends.

**Features:**

- [ ] Weekly/monthly/all-time leaderboards
- [ ] Friend comparison view
- [ ] Achievements unlocked rankings
- [ ] Genre-specific leaderboards
- [ ] Publishable profile card images

**Technical Notes:**

- Requires SaveState account sync
- Privacy controls (public/friends/private)
- WebSocket for real-time updates

---

### 3.5 Live Game Stats Widget

**Capability:** `UIExtension` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** Stream overlay with real-time statistics.

**Features:**

- [ ] Transparent overlay window
- [ ] Current session stats
- [ ] Achievement progress bar
- [ ] Recent screenshots ticker
- [ ] Customizable layout/colors

**Technical Notes:**

- Always-on-top transparent Avalonia window
- OBS window capture friendly
- Chroma key background option

---

## Wave 4: AI-Powered Plugins (Q2 2026 - Weeks 13-16)

*Focus: Intelligent recommendations and automation*

### 4.1 AI Game Recommender

**Capability:** `AIService` | **Complexity:** Hard | **Est. Effort:** 7 days

**Description:** "What should I play tonight?" based on mood and context.

**Features:**

- [ ] Mood-based suggestions ("relaxing", "challenging", "social")
- [ ] Time-aware ("I have 30 minutes")
- [ ] Weather/season integration
- [ ] Learning from play patterns
- [ ] "Surprise me" random pick with reasoning

**Technical Notes:**

- Use existing `IAiOrchestrator`
- Embeddings for game similarity
- Context: time of day, recent plays, completion %

---

### 4.2 Smart Playlist Generator

**Capability:** `AIService` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** Auto-generate game playlists like Spotify.

**Features:**

- [ ] "Cozy Sunday" playlist
- [ ] "Quick Sessions" (games under 30 min)
- [ ] "Finish These" (90%+ complete)
- [ ] Genre mixes
- [ ] Seasonal playlists

**Technical Notes:**

- Rule-based + AI hybrid
- User feedback loop (like/dislike)
- Playlist sharing via export

---

### 4.3 AI Backlog Prioritizer

**Capability:** `AIService` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Rank backlog by personalized priority score.

**Features:**

- [ ] HLTB estimate integration
- [ ] "Leaving Game Pass" boost
- [ ] Friend activity influence
- [ ] Price drop detection
- [ ] Mood matching score

**Technical Notes:**

- Weighted scoring algorithm
- Daily recalculation
- Manual override support

---

### 4.4 Screenshot Caption Generator

**Capability:** `AIService` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** AI-describe screenshots for accessibility and sharing.

**Features:**

- [ ] Auto-generate alt text
- [ ] Detect game from screenshot
- [ ] Identify notable moments (boss, cutscene)
- [ ] Suggest social media captions
- [ ] Batch process existing screenshots

**Technical Notes:**

- Use `IImageAnalysisService` (Google Vision)
- Cache captions in metadata
- User editable outputs

---

### 4.5 Game Summary Generator

**Capability:** `AIService` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** AI-written story recaps for RPGs.

**Features:**

- [ ] "What happened last time" summary
- [ ] Character/quest tracking
- [ ] Major decision log
- [ ] Session notes auto-generation
- [ ] Exportable journal format

**Technical Notes:**

- LLM prompt engineering
- Session event tracking
- Integration with game-specific APIs (if available)

---

## Wave 5: Utility Plugins (Q2-Q3 2026 - Weeks 17-20)

*Focus: Quality of life improvements*

### 5.1 Game Backup Manager

**Capability:** `CloudStorage` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Automated save game backup to cloud.

**Features:**

- [ ] Per-game save location detection
- [ ] Auto-backup on game close
- [ ] Version history (last 10 saves)
- [ ] Cloud sync (Google Drive, OneDrive, Dropbox)
- [ ] One-click restore

**Technical Notes:**

- Use PCGamingWiki save paths database
- Incremental backups (rsync-style)
- Zip compression with game name/date

---

### 5.2 Mod Manager Pro

**Capability:** `UIExtension` | **Complexity:** Hard | **Est. Effort:** 10 days

**Description:** Nexus Mods + ModDB integration.

**Features:**

- [ ] Nexus Mods OAuth login
- [ ] Browse mods from app
- [ ] Download with Vortex integration
- [ ] Mod load order management
- [ ] Conflict detection

**Technical Notes:**

- Nexus API: `https://api.nexusmods.com/v1`
- ModDB scraping (no official API)
- LOOT integration for Bethesda games

---

### 5.3 Controller Remapper

**Capability:** `InputProvider` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Per-game controller profiles.

**Features:**

- [ ] Custom button mappings
- [ ] Stick sensitivity curves
- [ ] Trigger dead zones
- [ ] Macro button combinations
- [ ] Auto-switch on game launch

**Technical Notes:**

- SDL2 GameController API
- Profile storage per game ID
- Visual controller diagram

---

### 5.4 FPS Unlocker

**Capability:** `SystemOptimization` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Auto-apply FPS patches for older games.

**Features:**

- [ ] Database of known FPS locks
- [ ] Memory patching for common engines
- [ ] Config file modifications
- [ ] Safe mode (backup original)
- [ ] User-submitted patches

**Technical Notes:**

- Process memory access
- Pattern scanning for frame limiters
- Community patch database (JSON)

---

### 5.5 Launch Time Optimizer

**Capability:** `LaunchExperience` | **Complexity:** Easy | **Est. Effort:** 3 days

**Description:** Pre-launch scripts and system optimization.

**Features:**

- [ ] Close resource-heavy apps before launch
- [ ] Set process priority/affinity
- [ ] Disable Windows features temporarily
- [ ] Network optimization (disable updates)
- [ ] Post-game cleanup scripts

**Technical Notes:**

- Process management via WMI
- PowerShell script execution
- User-defined app kill list

---

## Wave 6: Emulation & Retro Plugins (Q3 2026 - Weeks 21-24)

*Focus: Retro gaming enhancements*

### 6.1 Shader Preset Manager

**Capability:** `Emulation` | **Complexity:** Easy | **Est. Effort:** 3 days

**Description:** Quick-swap CRT/LCD shaders per core.

**Features:**

- [ ] Preset library (CRT Royale, LCD, etc.)
- [ ] Per-game shader selection
- [ ] One-click shader toggle
- [ ] Custom shader import
- [ ] Preview thumbnails

**Technical Notes:**

- RetroArch shader preset format
- Hot-swap via `IRetroArchService`
- Screenshot comparison view

---

### 6.2 RetroAchievements Overlay

**Capability:** `Emulation` | **Complexity:** Medium | **Est. Effort:** 5 days

**Description:** Achievement pop-up overlay.

**Features:**

- [ ] Pop-up on unlock (customizable position)
- [ ] Sound effects
- [ ] Progress bar for challenges
- [ ] Rich Presence display
- [ ] Leaderboard submit notifications

**Technical Notes:**

- Use existing `IRetroAchievementsClient`
- Overlay via window composition
- Audio playback for jingles

---

### 6.3 ROM Hack Database

**Capability:** `Emulation` | **Complexity:** Hard | **Est. Effort:** 8 days

**Description:** Browse and apply popular ROM hacks.

**Features:**

- [ ] RHDN integration
- [ ] Patch application (IPS, BPS, UPS)
- [ ] Hack categories (translation, improvement)
- [ ] Parent ROM verification
- [ ] Rollback/unpatch support

**Technical Notes:**

- Scrape romhacking.net
- Patch formats: Floating IPS library
- Original ROM hash verification

---

### 6.4 Box Art Generator

**Capability:** `AIService` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** AI-generate missing cover art.

**Features:**

- [ ] Style matching (NES, SNES, Genesis)
- [ ] Title text integration
- [ ] Multiple aspect ratios
- [ ] Manual seed/regenerate
- [ ] Batch generation

**Technical Notes:**

- Use image generation API
- Style prompts per console era
- User approval before applying

---

### 6.5 Cheat Code Manager

**Capability:** `Emulation` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** GameShark/Action Replay database.

**Features:**

- [ ] Searchable cheat database
- [ ] Format conversion (GS, AR, raw)
- [ ] Per-game cheat file generation
- [ ] Safe cheats marking (cosmetic only)
- [ ] Community submissions

**Technical Notes:**

- Parse gamehacking.org database
- Generate RetroArch .cht files
- Hash-based game identification

---

## Wave 7: Analytics Deep Dive (Q3 2026 - Weeks 25-28)

*Focus: Data insights and external sync*

### 7.1 Backloggd Sync

**Capability:** `Analytics` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** Two-way sync with Backloggd.com.

**Features:**

- [ ] OAuth login
- [ ] Push playtime and ratings
- [ ] Pull reviews and lists
- [ ] Sync completion status
- [ ] Import Backloggd wishlist

**Technical Notes:**

- Backloggd API (if available) or scraping
- Conflict resolution strategy
- Batch sync with rate limiting

---

### 7.2 RAWG.io Integration

**Capability:** `MetadataScraper` | **Complexity:** Easy | **Est. Effort:** 3 days

**Description:** Pull ratings, reviews, and trending data.

**Features:**

- [ ] Metacritic-style aggregate scores
- [ ] User reviews preview
- [ ] "Trending Now" section
- [ ] Similar games suggestions
- [ ] Release calendar

**Technical Notes:**

- Free API key tier
- 20,000 requests/month limit
- Cache with 24-hour TTL

---

### 7.3 Twitch Drops Tracker

**Capability:** `Analytics` | **Complexity:** Medium | **Est. Effort:** 4 days

**Description:** Alert when owned games have active Twitch drops.

**Features:**

- [ ] Scan active campaigns
- [ ] Match against library
- [ ] Show drop details (what you get)
- [ ] Direct link to claim
- [ ] Watch time estimator

**Technical Notes:**

- Twitch API drops endpoint
- Title matching with fuzzy logic
- Background 30-minute refresh

---

### 7.4 Genre Deep Dive

**Capability:** `AIService` | **Complexity:** Hard | **Est. Effort:** 6 days

**Description:** AI-powered genre preference analysis.

**Features:**

- [ ] Playtime distribution by genre
- [ ] Preference evolution over time
- [ ] Underexplored genre suggestions
- [ ] "Gaming personality" profile
- [ ] Shareable infographic

**Technical Notes:**

- Clustering algorithm for patterns
- Natural language insight generation
- Exportable image cards

---

### 7.5 Manual Viewer

**Capability:** `UIExtension` | **Complexity:** Easy | **Est. Effort:** 2 days

**Description:** PDF/image viewer for game manuals.

**Features:**

- [ ] PDF rendering in-app
- [ ] Image gallery for scanned manuals
- [ ] Bookmark pages
- [ ] Search within manual
- [ ] Link to online manual databases

**Technical Notes:**

- PDF.js or SkiaSharp PDF rendering
- Manual path auto-detection
- ReplacementDocs.com integration

---

## Wave 8: Cross-Platform & Advanced (Q4 2026 - Weeks 29-36)

*Focus: Multi-device and advanced integrations*

### 8.1 Steam Deck Sync

**Capability:** `SteamDeckIntegration` | **Complexity:** Hard | **Est. Effort:** 8 days

**Description:** Sync library and settings with Steam Deck.

**Features:**

- [ ] SSH-based connection to Deck
- [ ] Sync non-Steam game shortcuts
- [ ] Controller layout sync
- [ ] Playtime merge from SteamOS
- [ ] Remote wake and launch

**Technical Notes:**

- SSH via Paramiko equivalent
- Steam shortcuts.vdf manipulation
- Deck discovery via mDNS

---

### 8.2 Mobile Companion App

**Capability:** `UIExtension` | **Complexity:** Hard | **Est. Effort:** 15 days

**Description:** Browse library from phone.

**Features:**

- [ ] Read-only library view
- [ ] Remote game launch
- [ ] Notification center
- [ ] Achievement tracker
- [ ] Wishlist management

**Technical Notes:**

- .NET MAUI mobile app
- REST API + WebSocket for real-time
- Push notifications via Firebase

---

### 8.3 Cloud Save Converter

**Capability:** `CloudStorage` | **Complexity:** Hard | **Est. Effort:** 7 days

**Description:** Convert saves between platforms.

**Features:**

- [ ] Steam ↔ GOG ↔ Epic save conversion
- [ ] Console save import (where possible)
- [ ] Save format documentation
- [ ] Checksum recalculation
- [ ] Preview before apply

**Technical Notes:**

- Per-game save format parsers
- Community-contributed converters
- Sandboxed conversion environment

---

### 8.4 Voice Commands

**Capability:** `AIService` | **Complexity:** Hard | **Est. Effort:** 10 days

**Description:** "Hey SaveState, launch Elden Ring"

**Features:**

- [ ] Wake word detection
- [ ] Natural language game launch
- [ ] Voice-controlled navigation
- [ ] "What should I play?" voice query
- [ ] Accessibility mode integration

**Technical Notes:**

- Vosk for offline wake word
- Google Speech-to-Text for commands
- Intent classification model

---

### 8.5 Twitch Chat Overlay

**Capability:** `SocialFeatures` | **Complexity:** Hard | **Est. Effort:** 7 days

**Description:** In-game chat overlay while streaming.

**Features:**

- [ ] Transparent overlay window
- [ ] Emote support (BTTV, FFZ, 7TV)
- [ ] Chat filters (mod-only, sub-only)
- [ ] TTS for selected messages
- [ ] Quick reply panel

**Technical Notes:**

- Twitch IRC via WebSocket
- PubSub for subscriptions
- Emote image caching

---

## Implementation Priority Matrix

| Priority | Plugins | Rationale |
|----------|---------|-----------|
| **P0 (MVP)** | Game Timer, Screenshot Sorter, Discord RPC Pro, Game Pass Leaving | Quick wins, high user value |
| **P1 (Core)** | itch.io Importer, OBS Integration, Game Backup Manager | Major feature gaps filled |
| **P2 (Growth)** | AI Recommender, Mod Manager, Backloggd Sync | Differentiation from competitors |
| **P3 (Polish)** | Themes, Overlays, Voice Commands | Premium experience |

---

## Resource Requirements

| Role | Estimated Hours | Notes |
|------|-----------------|-------|
| Plugin Developer | ~400 hours | Core implementations |
| UI/UX Designer | ~80 hours | Themes, overlays, mobile |
| API Integration | ~60 hours | OAuth, external APIs |
| QA/Testing | ~120 hours | Plugin compatibility |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Plugins shipped | 45 | Count |
| Marketplace downloads | 10,000 | Cumulative |
| User rating | 4.5+ stars | Average |
| Active plugin usage | 70% | Users with 2+ plugins |

---

## Appendix: API Keys Required

| Service | Plugin(s) | How to Get |
|---------|-----------|------------|
| Discord | Discord RPC Pro | <https://discord.com/developers> |
| Twitter/X | Twitter Auto-Post | <https://developer.twitter.com> |
| Twitch | Drops Tracker, Chat Overlay | <https://dev.twitch.tv> |
| Nexus Mods | Mod Manager Pro | <https://www.nexusmods.com/users/myaccount?tab=api> |
| RAWG.io | RAWG Integration | <https://rawg.io/apidocs> |
| itch.io | itch.io Importer | <https://itch.io/user/settings/api-keys> |
| IsThereAnyDeal | Price Tracker | <https://isthereanydeal.com/dev/app> |

---

*This roadmap is a living document. Plugin priorities may shift based on user feedback and community requests.*
