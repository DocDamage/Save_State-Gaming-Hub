# Quick Start: Phase 6 Optional Features

## Voice Commands

### Enable Voice Listening
```
In UI: Click "Start Listening" button
Keyboard: Alt+V
Voice: "Start listening"
```

### Available Voice Commands
| Command | Alternative | Action |
|---------|-------------|--------|
| Launch [Game] | Play [Game], Start [Game] | Launches game |
| Save state | Save game, Save progress | Creates save state |
| Load state | Load game, Resume | Loads recent save |
| Sync cloud | Sync, Upload | Cloud synchronization |
| Show settings | Open settings | Settings dialog |

### Register Custom Command
1. Open Voice Command settings
2. Click "Register New Command"
3. Enter command phrase (e.g., "Launch Elden Ring")
4. Add alternatives (optional)
5. Click "Save"

---

## Analytics Dashboard

### View Heatmap
1. Go to Analytics tab
2. Select year from dropdown
3. View GitHub-style heatmap
4. Green = active, lighter = less active

### See Predictions
1. Open Analytics
2. Scroll to "Completion Predictions"
3. See estimated completion % and time
4. Get personalized recommendations

### Check Play Patterns
1. Analytics tab
2. "Play Patterns" section
3. View frequency, session length
4. See identified habits

### Find At-Risk Games
1. Analytics
2. Search for games not played recently
3. High abandonment risk shown
4. Get re-engagement suggestions

---

## Accessibility Features

### Enable Screen Reader
1. Settings → Accessibility
2. Toggle "Screen Reader"
3. App will announce changes

### Enable Text-to-Speech
1. Settings → Accessibility
2. Toggle "Text-to-Speech"
3. Adjust rate and volume
4. Settings read aloud automatically

### Apply Color Blind Mode
1. Settings → Accessibility
2. Enable "Color Blind Mode"
3. Select mode from dropdown:
   - Normal (no filter)
   - Protanopia (red-blind)
   - Deuteranopia (green-blind)
   - Tritanopia (blue-blind)
   - Achromatopsia (no color)
4. Apply to UI

### Scale UI
1. Settings → Accessibility
2. Adjust "UI Scale" (50%-300%)
3. Click "Apply"

### Adjust Font Size
1. Settings → Accessibility
2. Adjust "Font Size" (0.8x-2.0x)
3. Click "Apply"

### Reduce Motion
1. Settings → Accessibility
2. Toggle "Reduce Motion"
3. Animations minimized

---

## Audio Optimization

### Create Game Profile
1. Settings → Audio Optimization
2. Configure audio settings
3. Click "Optimize for Game"
4. Select game
5. Profile created

### Save Audio Profile
1. Audio settings configured
2. Enter profile name
3. Click "Save Profile"
4. Reusable for any game

### Load Audio Profile
1. Settings → Audio Optimization
2. See "Saved Profiles" list
3. Click profile to load
4. Settings applied

### Adjust Latency
1. Audio settings
2. Select latency mode:
   - Default (~20ms)
   - Low (~5ms)
   - Ultra (~2ms)
3. Apply settings

### Configure Audio Device
1. Audio settings
2. Select device from dropdown
3. Configure channels, sample rate
4. Apply

---

## Settings Reference

### Voice Settings
- **Listening Status**: On/Off
- **Default Language**: en-US (configurable)
- **Confidence Threshold**: 0.7 (70%)
- **Command Timeout**: 5 seconds

### Analytics Settings
- **Cache Duration**: 1 hour
- **History**: Last 180 days
- **Export Format**: CSV, JSON
- **Heatmap Year**: Selectable

### Accessibility Settings
- **Screen Reader**: On/Off
- **TTS Rate**: 0.5x - 2.0x
- **TTS Volume**: 0-100%
- **UI Scale**: 50%-300%
- **Font Size**: 0.8x - 2.0x
- **High Contrast**: On/Off
- **Color Blind Mode**: 5 options
- **Reduce Motion**: On/Off

### Audio Settings
- **Sample Rate**: 44.1-192 kHz
- **Bit Depth**: 16, 24, 32-bit
- **Latency Mode**: Default, Low, Ultra
- **Exclusive Mode**: On/Off
- **Spatial Audio**: On/Off
- **Master Volume**: 0-100%
- **Game Volume**: 0-100%
- **UI Volume**: 0-100%
- **Dialogue Volume**: 0-100%

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Alt+V | Start/Stop voice listening |
| Alt+A | Open Analytics |
| Alt+S | Open Settings |
| Alt+Shift+A | Toggle Accessibility |
| Ctrl+. | Command Palette |
| Ctrl+H | Show/Hide HUD |
| Ctrl+K | Voice command help |

---

## Tips & Tricks

### Voice Commands
- Speak clearly and naturally
- Use the exact command phrase for best results
- Alternative phrases work too
- Command history helps learn what works
- Register custom commands for your games

### Analytics
- Check heatmap monthly for trends
- Review play patterns to understand habits
- Use completion predictions for backlog planning
- Get early alerts for games at abandonment risk

### Accessibility
- Combine multiple features as needed
- Color blind modes can be toggled instantly
- UI scaling resets on restart
- Motion reduction works with all themes

### Audio
- Create profiles before gaming sessions
- Low latency for competitive games
- Cinematic for story games
- Esports preset for shooters
- Custom profiles for unique needs

---

## Troubleshooting

### Voice Not Working
✓ Check microphone in Sound settings
✓ Ensure microphone permissions granted
✓ Test mic works in Sound settings
✓ Restart app if needed

### Analytics Not Showing
✓ Need at least 3 sessions per game
✓ Check data file permissions
✓ Wait for cache refresh (1 hour)
✓ Manual refresh available

### Accessibility Feature Not Applying
✓ Some require restart (UI scale)
✓ Check OS accessibility settings
✓ Verify feature available on OS
✓ Check app logs

### Audio Profile Not Saving
✓ Check disk space available
✓ Verify file permissions
✓ Ensure device still connected
✓ Check audio service running

---

## Need Help?

- **Voice**: See "Voice Command Help" (Ctrl+K)
- **Analytics**: Hover over metrics for info
- **Accessibility**: Settings have detailed descriptions
- **Audio**: Presets explain each setting

---

## For Developers

To integrate Phase 6 features:

```csharp
// Inject services
var voiceVm = services.GetRequiredService<VoiceCommandViewModel>();
var analyticsVm = services.GetRequiredService<AdvancedAnalyticsViewModel>();
var a11yVm = services.GetRequiredService<AccessibilityViewModel>();
var audioVm = services.GetRequiredService<AudioOptimizationViewModel>();

// Initialize
await voiceVm.InitializeAsync();
await analyticsVm.InitializeAsync();
await a11yVm.InitializeAsync();
await audioVm.InitializeAsync();
```

See `PHASE6_OPTIONAL_FEATURES_GUIDE.md` for full documentation.

---

**Status**: ✅ All features production-ready
**Version**: Phase 6 Complete
**Last Updated**: January 13, 2026
