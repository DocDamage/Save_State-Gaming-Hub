# Cheat Engine Table Sources

This guide provides information on where to find Cheat Engine tables (.CT files) for importing into SaveStateReborn.

## Overview

Cheat Engine tables contain memory addresses and patterns that can be imported into SaveStateReborn's memory signature database. This allows the application to:

- Automatically detect game values (health, score, ammo, etc.)
- Track game progress and statistics
- Enable smart triggers based on in-game events
- Provide real-time game information overlays

## Importing Tables

To import Cheat Engine tables:

1. Open SaveStateReborn
2. Navigate to **Tools** > **Import Cheat Engine Table**
3. Drag and drop a `.CT` file or click **Browse** to select one
4. Review the detected entries
5. Select which entries to import
6. Click **Import**

## Popular Sources for Cheat Tables

### Fearless Revolution
**Website:** https://fearlessrevolution.com/

The largest community for Cheat Engine tables. Features:
- Tables for thousands of games
- Active community with regular updates
- Quality ratings and user reviews
- Advanced tables with Lua scripts

**How to download:**
1. Create a free account
2. Browse or search for your game
3. Download the attached `.CT` file
4. Import into SaveStateReborn

### Cheat Engine Forums
**Website:** https://forum.cheatengine.org/

The official Cheat Engine community:
- Tables organized by game
- Tutorials on table creation
- Support from experienced users
- Latest table format features

### GitHub Repositories

Many developers host cheat tables on GitHub:
- Search: `cheat engine table [game name]`
- Look for repositories with `.CT` files
- Check README for installation instructions

### Game-Specific Communities

#### Reddit
- r/CheatEngine
- Game-specific subreddits (e.g., r/darksouls, r/witcher)
- Search for "cheat table" or "CE table"

#### Discord Servers
- Fearless Revolution Discord
- Game-specific Discord communities
- Cheat Engine official Discord

### PCGamingWiki
**Website:** https://www.pcgamingwiki.com/

While primarily for fixes, some game pages include:
- Memory addresses for common values
- Links to cheat tables
- Community-contributed solutions

## Table Compatibility

### Supported Formats

SaveStateReborn supports:
- **Cheat Engine 6.0+ XML format** - Modern `.CT` files
- **Compressed tables** - GZip compressed tables (automatically detected)
- **Plain XML** - Uncompressed XML structure

### Supported Entry Types

| Type | Import Support | Notes |
|------|---------------|-------|
| 4 Bytes | ✅ Full | Integer values |
| Float | ✅ Full | Floating point |
| Double | ✅ Full | Double precision |
| Byte | ✅ Full | Single byte values |
| 2 Bytes | ✅ Full | Short integers |
| 8 Bytes | ✅ Full | Long integers |
| String | ⚠️ Limited | Basic support |
| Array of Byte | ❌ No | Not supported |
| Lua Scripts | ⚠️ Warning | Imported with warning flag |

### Address Types

- **Static addresses** - `Game.exe+123456`
- **Module+Offset** - `module.dll+ABC`
- **Pointer chains** - `[[[Base]+Offset1]+Offset2]`
- **Absolute addresses** - `0x7FF123456789`

## Best Practices

### Before Importing

1. **Scan for viruses** - Only download from trusted sources
2. **Check game version** - Tables are version-specific
3. **Read comments** - Community feedback on table quality
4. **Backup your database** - In case of import issues

### During Import

1. **Review entries** - Deselect entries you don't need
2. **Check game title** - Ensure the correct game name is set
3. **Handle scripts carefully** - Lua scripts require manual review
4. **Use tags** - Apply tags like "health", "ammo" for organization

### After Import

1. **Test signatures** - Launch the game and verify detection
2. **Adjust priorities** - Set higher priority for critical values
3. **Set value ranges** - Configure min/max for validation
4. **Share back** - Contribute improvements to the community

## Troubleshooting

### Table Won't Import

**Issue:** "File does not appear to be a valid Cheat Engine table"

**Solutions:**
- Ensure file extension is `.CT`
- Try opening the file in Cheat Engine first
- Check if the file is corrupted
- Some very old table formats may not be supported

### Entries Not Detected

**Issue:** Preview shows 0 entries

**Solutions:**
- The table may use advanced features not supported
- Try the "Include Lua scripts" option
- Check if the table requires specific Cheat Engine version
- The table structure may be encrypted

### Import Conflicts

**Issue:** "Duplicate entry" warnings

**Solutions:**
- Use "Overwrite existing" to update existing signatures
- Use "Skip duplicates" to keep existing signatures
- Manually review and merge conflicting entries

### Game Not Detected

**Issue:** Signatures don't work in-game

**Solutions:**
- Verify the game version matches the table
- Some games have anti-cheat that prevents memory reading
- Try creating your own signatures using the Auto Discovery feature
- Check if the game uses a different executable name

## Creating Your Own Tables

If you can't find a table for your game:

1. **Use Cheat Engine** - Download from https://cheatengine.org/
2. **Find addresses manually** - Tutorial: https://wiki.cheatengine.org/
3. **Save as .CT file** - Export your findings
4. **Import to SaveStateReborn** - Use the import dialog

## Contributing

If you create working signatures:

1. **Export from SaveStateReborn** - Save your database
2. **Share tables** - Upload to Fearless Revolution or GitHub
3. **Document** - Include game version and notes
4. **Credit** - Acknowledge original table authors

## Legal Notice

Using cheat tables may:
- Violate Terms of Service for online games
- Result in account bans
- Break game functionality

**Use responsibly:**
- Only use in single-player/offline modes
- Respect game developers' work
- Don't use for competitive advantage in multiplayer

## Support

For issues with importing:
- Check the SaveStateReborn documentation
- Visit the project GitHub issues page
- Join the community Discord

For table-specific issues:
- Contact the table author
- Check the source forum/thread
- Ask in the Cheat Engine community
