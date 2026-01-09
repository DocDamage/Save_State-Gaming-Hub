# 🎨 SaveStateReborn Design System

**Version**: 2.1 - WeMod-Inspired Premium Theme
**Updated**: January 8, 2026 (Dialog System Complete)

---

## 🌟 Design Philosophy

SaveStateReborn's design system is inspired by **WeMod's modern, premium aesthetic** with:

- **High Contrast** - All text is easily readable
- **Vibrant Accents** - Purple/cyan gradients for premium feel
- **Smooth Animations** - Micro-interactions enhance UX
- **Modern Typography** - Larger, clearer fonts throughout
- **Dark Theme** - Optimized for extended gaming sessions

---

## 🎨 Color Palette

### Background Colors

```
Background:     #0D0D0F  (Deep Black)
Surface:        #16161A  (Slightly Lighter)
Card:           #1C1C21  (Elevated Surface)
Card Hover:     #242429  (Interactive State)
```

### Brand Colors

```
Primary:        #7C3AED  (Vibrant Purple)
Primary Hover:  #8B5CF6  (Lighter Purple)
Accent:         #06B6D4  (Cyan)
Accent Hover:   #22D3EE  (Bright Cyan)
```

### Text Colors (High Contrast)

```
Primary Text:   #FFFFFF  (Pure White)
Secondary Text: #E0E0E6  (Light Gray)
Tertiary Text:  #A0A0AB  (Medium Gray)
Muted Text:     #6B6B76  (Subtle Gray)
```

### Status Colors

```
Success:        #10B981  (Green)
Warning:        #F59E0B  (Amber)
Error:          #EF4444  (Red)
Info:           #3B82F6  (Blue)
```

### Gradients

```css
Premium:    linear-gradient(135deg, #7C3AED 0%, #8B5CF6 50%, #06B6D4 100%)
Accent:     linear-gradient(90deg, #06B6D4 0%, #22D3EE 100%)
Success:    linear-gradient(90deg, #10B981 0%, #34D399 100%)
```

---

## 📝 Typography System

### Font Stack

```
Primary: Segoe UI, Inter, -apple-system, BlinkMacSystemFont, Roboto, Helvetica Neue, Arial, sans-serif
```

### Type Scale (Increased for Visibility)

| Style | Size | Weight | Usage |
|-------|------|--------|-------|
| **Display** | 36px | Bold | Extra large headers |
| **Header** | 28px | Bold | Page titles |
| **Section Header** | 22px | SemiBold | Section titles |
| **Subtitle** | 18px | Medium | Subtitles |
| **Body** | 15px | Normal | Default text |
| **Body Secondary** | 14px | Normal | Secondary text |
| **Small** | 13px | Normal | Small text |
| **Label** | 12px | SemiBold | Labels (UPPERCASE) |
| **Caption** | 12px | Normal | Captions |

### Usage Examples

```xml
<!-- Page Title -->
<TextBlock Classes="Header" Text="Game Library" />

<!-- Section Title -->
<TextBlock Classes="SectionHeader" Text="Recently Played" />

<!-- Body Text -->
<TextBlock Classes="Body" Text="Description goes here..." />

<!-- Label -->
<TextBlock Classes="Label" Text="STATUS" />
```

---

## 🎛️ Component Styles

### Buttons

#### Primary Button (Main Actions)

```xml
<Button Classes="Primary" Content="Launch Game" />
```

- **Background**: Premium Gradient
- **Padding**: 20px 12px
- **Font Size**: 15px
- **Border Radius**: 10px
- **Hover**: Scale 1.03, Lift 1px
- **Press**: Scale 0.98

#### Secondary Button (Alternative Actions)

```xml
<Button Classes="Secondary" Content="View Details" />
```

- **Background**: Card Background
- **Border**: 1px Light Border
- **Hover**: Background changes, Scale 1.02

#### Accent Button (Special Actions)

```xml
<Button Classes="Accent" Content="Upgrade Now" />
```

- **Background**: Accent Gradient
- **Same interactions as Primary**

#### Ghost Button (Minimal)

```xml
<Button Classes="Ghost" Content="Cancel" />
```

- **Background**: Transparent
- **Hover**: Subtle background

#### Icon Button (Icon-Only)

```xml
<Button Classes="Icon" Content="⚙️" />
```

- **Size**: 40x40px
- **Padding**: 10px

### Containers

#### Glass Container (Frosted Glass)

```xml
<Border Classes="GlassContainer">
    <!-- Content -->
</Border>
```

- **Background**: Semi-transparent
- **Border Radius**: 16px
- **Padding**: 20px

#### Card (Elevated Surface)

```xml
<Border Classes="Card">
    <!-- Content -->
</Border>
```

- **Background**: Card Background
- **Border Radius**: 16px
- **Hover**: Background lightens

#### Game Card (Interactive Tile)

```xml
<Border Classes="GameCard">
    <!-- Game info -->
</Border>
```

- **Hover**: Scale 1.03, Lift 4px, Purple border
- **Cursor**: Hand

#### List Item (Selectable Row)

```xml
<Border Classes="ListItem">
    <!-- Row content -->
</Border>
```

- **Border Radius**: 10px
- **Hover**: Subtle background

### Navigation

#### Sidebar Button

```xml
<Button Classes="SidebarButton" Content="🎮 Library" />
```

- **Alignment**: Left
- **Font Size**: 15px
- **Hover**: Background + Foreground change

#### Active Sidebar Button

```xml
<Button Classes="SidebarButton Active" Content="🎮 Library" />
```

- **Background**: Premium Gradient
- **Font Weight**: SemiBold

---

## 🎬 Animation Guidelines

### Timing

- **Fast**: 150ms (Hover states, clicks)
- **Medium**: 200ms (Card hovers, transitions)
- **Slow**: 300ms (Page transitions)

### Easing

- **Default**: Ease-in-out
- **Hover**: Ease-out
- **Press**: Ease-in

### Transform Effects

```xml
<!-- Hover Lift -->
RenderTransform="scale(1.03) translateY(-2px)"

<!-- Press -->
RenderTransform="scale(0.98)"

<!-- Card Hover -->
RenderTransform="scale(1.03) translateY(-4px)"
```

---

## 📐 Spacing System

### Padding Scale

```
XS:  4px
SM:  8px
MD:  12px
LG:  16px
XL:  20px
2XL: 24px
3XL: 32px
```

### Margin Scale

```
XS:  4px
SM:  8px
MD:  16px
LG:  24px
XL:  32px
2XL: 48px
```

### Border Radius

```
SM:  8px   (Buttons, inputs)
MD:  10px  (List items)
LG:  12px  (Small cards)
XL:  16px  (Large cards)
```

---

## 🎯 Usage Examples

### Modern Game Card

```xml
<Border Classes="GameCard" Width="200" Height="280">
    <Grid RowDefinitions="*, Auto">
        <!-- Game Cover -->
        <Image Grid.Row="0" Source="{Binding CoverUrl}" Stretch="UniformToFill" />

        <!-- Game Info -->
        <StackPanel Grid.Row="1" Padding="12" Background="{StaticResource CardBackgroundBrush}">
            <TextBlock Classes="Body" Text="{Binding Title}" FontWeight="SemiBold" />
            <TextBlock Classes="Caption" Text="{Binding Platform}" Margin="0,4,0,0" />
        </StackPanel>
    </Grid>
</Border>
```

### Premium Action Button

```xml
<Button Classes="Primary" Padding="24,14">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="🚀" FontSize="18" />
        <TextBlock Text="Launch Game" FontSize="16" FontWeight="SemiBold" />
    </StackPanel>
</Button>
```

### Section Header with Divider

```xml
<StackPanel>
    <TextBlock Classes="SectionHeader" Text="Recently Played" Margin="0,0,0,16" />
    <Border Classes="Divider" />
</StackPanel>
```

---

## 🎨 Before & After Comparison

### Old Design (Terminal-like)

- ❌ Small fonts (12-14px)
- ❌ Low contrast text (#888)
- ❌ Minimal styling
- ❌ Basic buttons
- ❌ No animations

### New Design (WeMod-inspired)

- ✅ Larger fonts (15-36px)
- ✅ High contrast (#FFFFFF)
- ✅ Premium gradients
- ✅ Interactive buttons
- ✅ Smooth animations
- ✅ Modern spacing
- ✅ Vibrant accents

---

## 🚀 Implementation Checklist

### Core Files Updated

- [x] `Styles/Brushes.axaml` - Color palette
- [x] `Styles/Controls.axaml` - Component styles
- [x] `App.axaml` - Global theme & fonts

### Next Steps

1. **Test Visibility** - Verify all text is readable
2. **Check Animations** - Ensure smooth interactions
3. **Review Components** - Update any custom views
4. **Accessibility** - Test with screen readers
5. **Performance** - Monitor animation performance

---

## 💡 Design Tips

### Do's ✅

- Use semantic class names (Header, Body, Primary)
- Maintain consistent spacing
- Apply hover states to interactive elements
- Use gradients for premium features
- Keep animations subtle and fast

### Don'ts ❌

- Don't use font sizes below 12px
- Don't use low-contrast text colors
- Don't skip hover states on buttons
- Don't overuse animations
- Don't mix different border radius values

---

## 🎯 Accessibility

### Contrast Ratios

- **Primary Text**: 21:1 (WCAG AAA)
- **Secondary Text**: 14:1 (WCAG AAA)
- **Tertiary Text**: 7:1 (WCAG AA)

### Font Sizes

- **Minimum**: 12px (Labels only)
- **Body**: 15px (Optimal readability)
- **Headers**: 22px+ (Clear hierarchy)

### Interactive Elements

- **Minimum Target Size**: 40x40px
- **Hover Feedback**: Always visible
- **Focus States**: Clear outlines

---

**Design System Version**: 2.1
**Last Updated**: January 8, 2026 (Dialog System Complete)
**Maintained By**: SaveStateReborn Team
