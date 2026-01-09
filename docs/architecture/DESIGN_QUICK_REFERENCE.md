# 🎨 Quick Visual Reference - New Design System

## Color Swatches

### Brand Colors

```
█████ #7C3AED  Primary Purple (Vibrant)
█████ #8B5CF6  Primary Hover (Lighter)
█████ #06B6D4  Accent Cyan (Fresh)
█████ #22D3EE  Accent Hover (Bright)
```

### Backgrounds

```
█████ #0D0D0F  Background (Deep Black)
█████ #16161A  Surface (Slightly Lighter)
█████ #1C1C21  Card (Elevated)
█████ #242429  Card Hover (Interactive)
```

### Text Colors

```
█████ #FFFFFF  Primary Text (Pure White)
█████ #E0E0E6  Secondary Text (Light Gray)
█████ #A0A0AB  Tertiary Text (Medium Gray)
█████ #6B6B76  Muted Text (Subtle)
```

---

## Typography Examples

```
DISPLAY TEXT (36px, Bold)
This is a display header for major sections

PAGE HEADER (28px, Bold)
This is a page title for main views

Section Header (22px, SemiBold)
This is a section title for content areas

Subtitle Text (18px, Medium)
This is subtitle text for descriptions

Body Text (15px, Normal)
This is the default body text used throughout the application
for paragraphs and general content.

Body Secondary (14px, Normal)
This is secondary body text for less important information.

Small Text (13px)
This is small text for compact areas

LABEL TEXT (12px, SemiBold)
This is label text for form fields

Caption text (12px)
This is caption text for image descriptions
```

---

## Button Styles

```
┌─────────────────────────┐
│   🚀 PRIMARY BUTTON     │  ← Purple/Cyan Gradient
└─────────────────────────┘

┌─────────────────────────┐
│   SECONDARY BUTTON      │  ← Outlined, Card Background
└─────────────────────────┘

┌─────────────────────────┐
│   💎 ACCENT BUTTON      │  ← Cyan Gradient
└─────────────────────────┘

  Ghost Button              ← Minimal, Transparent

  [⚙️]                      ← Icon Button (40x40px)
```

---

## Container Examples

```
╔═══════════════════════════════╗
║  Glass Container              ║
║  Semi-transparent background  ║
║  16px border radius           ║
╚═══════════════════════════════╝

┌───────────────────────────────┐
│  Card Container               │
│  Solid background             │
│  Hover effect enabled         │
└───────────────────────────────┘

┌─────────────────┐
│   Game Card     │
│                 │
│   [Image]       │
│                 │
│   Title         │
│   Platform      │
└─────────────────┘
  ↑ Hover: Lifts 4px, purple border
```

---

## Spacing Scale

```
XS:   ▌ 4px
SM:   ▌▌ 8px
MD:   ▌▌▌ 12px
LG:   ▌▌▌▌ 16px
XL:   ▌▌▌▌▌ 20px
2XL:  ▌▌▌▌▌▌ 24px
3XL:  ▌▌▌▌▌▌▌▌ 32px
```

---

## Animation Timing

```
Fast:    ━━━━━━━━━━ 150ms  (Hover, Click)
Medium:  ━━━━━━━━━━━━━━ 200ms  (Cards, Transitions)
Slow:    ━━━━━━━━━━━━━━━━━━━━ 300ms  (Page Changes)
```

---

## Common Patterns

### Page Layout

```
┌─────────────────────────────────────┐
│  PAGE HEADER (28px, Bold)           │
│  Subtitle text (18px)               │
│  ─────────────────────────────────  │ ← Divider
│                                     │
│  Section Header (22px)              │
│  ┌─────────────────────────────┐   │
│  │  Card Content               │   │
│  │  Body text (15px)           │   │
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### Button Group

```
┌──────────────┐  ┌──────────────┐
│   PRIMARY    │  │  SECONDARY   │
└──────────────┘  └──────────────┘
```

### List Item

```
┌─────────────────────────────────────┐
│  Title (15px, SemiBold)             │
│  Description (14px)                 │
│  Caption (12px)                     │
└─────────────────────────────────────┘
  ↑ Hover: Subtle background change
```

---

## Status Colors

```
✓ Success:  █████ #10B981  (Green)
⚠ Warning:  █████ #F59E0B  (Amber)
✗ Error:    █████ #EF4444  (Red)
ℹ Info:     █████ #3B82F6  (Blue)
```

---

## Gradients

### Premium Gradient

```
╔═══════════════════════════════╗
║ ████████████████████████████ ║
║ Purple → Light Purple → Cyan ║
╚═══════════════════════════════╝
```

### Accent Gradient

```
┌───────────────────────────────┐
│ ████████████████████████████ │
│ Cyan → Bright Cyan           │
└───────────────────────────────┘
```

---

## Quick Copy-Paste

### Header Section

```xml
<StackPanel Spacing="16">
    <TextBlock Classes="Header" Text="Your Title" />
    <TextBlock Classes="Subtitle" Text="Your subtitle" />
    <Border Classes="Divider" />
</StackPanel>
```

### Action Buttons

```xml
<StackPanel Orientation="Horizontal" Spacing="12">
    <Button Classes="Primary" Content="Confirm" />
    <Button Classes="Secondary" Content="Cancel" />
</StackPanel>
```

### Info Card

```xml
<Border Classes="Card">
    <StackPanel Spacing="12">
        <TextBlock Classes="SectionHeader" Text="Title" />
        <TextBlock Classes="Body" Text="Description here..." />
    </StackPanel>
</Border>
```

---

**Quick Reference Version**: 1.1
**For**: SaveStateReborn Design System
**Updated**: January 8, 2026 (Dialog System Complete)
