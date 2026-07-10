# AI Company Game — In-Game UI Design Specification

> Project: Argus (`korean-social-simulator`) / `unity/EmbodiedDebate`
> Surface: Unity uGUI (Screen Space - Overlay)
> Target: 2022.3 LTS, URP, desktop + mobile
> Date: 2026-07-09

## 1. Design intent

The game is a **casual party management sim**: 4 players run AI-minibot companies in a shared 3D office city. The UI must feel as friendly and readable as the minibot characters themselves — rounded, bouncy, and bright — while keeping the 3D playfield clear so players can read bot positions, moods, and rival movements at a glance.

- Fantasy: small startup CEO commanding cute robot employees
- Tone: humorous, energetic, Overcooked-like party chaos
- Priority: playfield first, then information
- Density rule: persistent HUD must cover <25% of desktop viewport; on mobile, one compact top cluster only

## 2. Visual identity

### 2.1 Color palette

| Token | Hex | Usage |
|---|---|---|
| `--ui-bg-primary` | `#1A1D26` | Panels, drawers, menus |
| `--ui-bg-secondary` | `#252A36` | Elevated cards, buttons idle |
| `--ui-bg-tertiary` | `#323A4A` | Hover, subtle separators |
| `--ui-accent` | `#4CC9F0` | Primary actions, player highlight, selected bot ring |
| `--ui-accent-warm` | `#F9C74F` | Coins, funds, rewards, positive events |
| `--ui-success` | `#90BE6D` | Good mood, task success, praise |
| `--ui-warning` | `#F8961E` | Caution, yellow mood band |
| `--ui-danger` | `#F94144` | Fire, scold, red mood, danger events |
| `--ui-text-primary` | `#F7F8FA` | Headings, labels |
| `--ui-text-secondary` | `#A8B1C4` | Body, helper text |
| `--ui-text-muted` | `#6B7280` | Disabled, timestamps |

Mood bands (world-space dots / detail drawer meter):
- Green: mood >= 80
- Yellow: 40 <= mood < 80
- Orange: 20 <= mood < 40
- Red: mood < 20

### 2.2 Typography

- Primary font: **Satoshi** or **Outfit** (geometric, friendly, readable at small sizes)
- Fallback: Unity default `LegacyRuntime.ttf` if custom font asset is not imported
- Scale (reference 1920x1080):
  - XS: 12 px — timestamps, helper hints
  - S: 14 px — button labels, mini-scoreboard rows
  - M: 16 px — body, status values
  - L: 20 px — drawer title, phase chip
  - XL: 28 px — pause menu title, major banners

### 2.3 Shape and material

- Corner radius: 12 px for cards, 8 px for buttons, 20 px for chips
- Shadow: soft drop shadow, y-offset 4 px, blur 16 px, black at 30% alpha
- Background: dark slate panel with 88% opacity so the world stays visible
- Glass effect: optional 4% white overlay on top edges for subtle sheen

## 3. Layout map

### 3.1 Desktop (16:9 and 21:9)

| Zone | Anchor | Size (ref 1920x1080) | Contents |
|---|---|---|---|
| Top-left | (0,1)-(0,1) pivot (0,1) | 420 x 120 | Company status strip: name, funds, CSAT, tasks |
| Top-center | (0.5,1)-(0.5,1) pivot (0.5,1) | 360 x 60 | Round/phase chip |
| Top-right | (1,1)-(1,1) pivot (1,1) | 360 x 140 | Mini-scoreboard |
| Bottom-center | (0.35,0)-(0.65,0) pivot (0.5,0) | 900 x 100 | Contextual command action bar (appears on bot select) |
| Right edge | (1,0)-(1,1) pivot (1,0.5) | 360 width, full height | Employee detail drawer (slide-in on select) |
| Center overlay | (0,0)-(1,1) pivot (0.5,0.5) | Full screen | Pause menu, full scoreboard, rumor log, death screens |
| Bottom-right | (0.65,0.05)-(0.98,0.35) | 380 x 240 | Toast stack |
| Top-center banner | (0.35,0.9)-(0.65,0.98) | Full width | State-sync / round transition banner |

### 3.2 Mobile (portrait, width < 900)

- Collapse top-center phase chip and top-right mini-scoreboard into the **top-left compact strip**.
- Bottom-center command bar becomes a **bottom sheet** occupying bottom 22% of screen.
- Right-edge detail drawer becomes a **full-screen modal sheet** sliding up from bottom.
- Center overlay remains full-screen for pause/scoreboard/rumor log.
- Toasts move to a **swipe-up event log** accessible from a small handle above the command bar.

## 4. Component specs

### 4.1 Company status strip (top-left)

- Background: `--ui-bg-primary`, 88% opacity
- Padding: 16 px
- Rows:
  - Company name (L, `--ui-text-primary`)
  - Funds: coin icon + value (M, `--ui-accent-warm`)
  - Customer satisfaction: heart icon + value / 100 (M, `--ui-success`)
  - Tasks: check icon + completed / failed (M, `--ui-text-secondary`)
- Mobile: keep only company name + funds; hide satisfaction/tasks until expanded.

### 4.2 Round/phase chip (top-center)

- Pill-shaped chip with `--ui-bg-secondary` background
- Label format: `Round {N} — {Phase}` (Morning, Work, Event, Evening)
- Phase color accent:
  - Morning: `--ui-accent-warm`
  - Work: `--ui-accent`
  - Event: `--ui-warning`
  - Evening: `--ui-text-secondary`

### 4.3 Mini-scoreboard (top-right)

- Background: `--ui-bg-primary`, 88% opacity
- List rank, company name (truncated), value
- Highlight local player with `--ui-accent` left border and bold text
- Maximum 4 rows; on mobile, hidden in top strip until expanded

### 4.4 Command action bar (bottom-center)

- Appears only when an employee is selected
- Background: `--ui-bg-primary`, 92% opacity, rounded top corners
- Grid: 2 rows x 6 columns (11 verbs + close), cell 120 x 40
- Button idle: `--ui-bg-secondary` with `--ui-text-primary`
- Button hover: `--ui-bg-tertiary`
- Button disabled: 50% opacity, `--ui-text-muted`
- Destructive actions (scold, fire) use `--ui-danger` accent on icon/left border
- Target-required commands (gossip, scout, assign_task) open a **target picker**

### 4.5 Target picker

- Centered modal overlay with `--ui-bg-primary` panel
- Title: action name
- Scrollable list of targets (employees or players)
- Each row shows avatar placeholder, name, and relevant stat
- Confirm/Cancel buttons

### 4.6 Employee detail drawer (right edge / mobile full-screen)

- Header: bot name, personality tags as chips
- Mood meter: horizontal bar with color band + numeric value
- Loyalty meter: same pattern, scale -100 to +100, center at 0
- Memory: scrollable text list of recent interactions
- Current task: label or "Idle"
- Close button bottom-center

### 4.7 Mood dots (world-space)

- SpriteRenderer or world-space Canvas above each minibot head
- Size: 24 px reference at 1080p
- Shape: circle
- Color by mood (see palette)
- Hide when off-screen or behind world geometry (use dot product / viewport cull)
- Selected bot: hide dot, show ground ring / outline instead

### 4.8 Toasts and banners

- Toast item: 260 x 60 rounded card, icon + title + one-line body
- Stack bottom-right, newest at bottom; max 5, oldest fades
- State-sync banner: top-center, auto-dismiss 3 seconds, slide+fade in/out
- Economy ticker: small bottom strip for major fund changes

### 4.9 Pause menu

- Full-screen overlay with 95% dark background
- Title: "AI Company Game"
- Buttons: Resume, Scoreboard, Rumor Log, Quit
- Sub-panels slide/fade in when selected

## 5. Interaction flows

### 5.1 Select employee and issue command

1. Player left-clicks / taps a minibot in the 3D office.
2. Mood dot hides; selection ring appears on ground.
3. Right-edge detail drawer slides in; command bar appears at bottom.
4. Player clicks a command.
5. If command needs a target, target picker opens.
6. Player confirms target; JSON command is sent via `GameBridgeReceiver`.
7. Drawer and command bar stay open until player clicks elsewhere or presses Escape.

### 5.2 Pause and inspect state

1. Player presses Escape or taps pause button.
2. Pause menu overlay opens; camera rotation and 3D selection are gated.
3. Player can open Full Scoreboard or Rumor Log sub-panels.
4. Resume returns to play; camera/input ungate.

### 5.3 React to events

1. Bridge fires `OnTaskUpdate`, `OnRumorEvent`, or `OnEconomyUpdate`.
2. Matching toast or banner appears with relevant icon/color.
3. State-sync banner appears on reconnect/round change.

## 6. Motion and animation

- Open/close drawers: 0.20 s ease-out translate/scale
- Command bar: 0.15 s slide up from bottom
- Toast: 0.25 s fade + slide in, 0.25 s fade out after 4 s
- Banner: 0.20 s slide down, hold 3 s, 0.20 s slide up
- Button hover: 0.10 s color/scale bump (1.03x)
- Mood dot: pulse 1.2x on mood change
- Prefer CanvasGroup alpha + anchored position tweens; avoid layout group rebuilds

## 7. Asset checklist

- [ ] 9-slice panel sprite (rounded rect, 24 px radius, 32 px border)
- [ ] 9-slice button sprite (rounded rect, 16 px radius)
- [ ] Mood dot sprite (32 x 32 circle, white so color can be tinted)
- [ ] Icon set (16 icons): coin, heart, check, warning, info, fire, scold, snack, raise, bonus, party, hire, gossip, scout, task, close
- [ ] Font asset: Satoshi or Outfit (TTF/OTF)
- [ ] Optional: per-company color chips for multiplayer differentiation

## 8. Input gating

- `InputGate.IsMenuOpen = true` while detail drawer, target picker, or pause menu is open.
- When true: camera rotation/drag, 3D click-to-select, and hotkeys are suspended.
- Transient toasts and banners do **not** set `IsMenuOpen`.

## 9. Accessibility notes (deferred)

- Controller/keyboard navigation deferred to later pass.
- Reduced-motion option can disable drawer slides and toast bounces by setting animation duration to 0.

## 10. QA checklist (manual, Unity Editor required)

- [ ] HUD covers <25% of 1920x1080 viewport during normal play.
- [ ] Clicking each minibot opens detail drawer and command bar.
- [ ] Each of the 11 commands is clickable and emits correct JSON.
- [ ] Mood dot colors update from `game.state_sync`.
- [ ] Pause menu suspends camera rotation.
- [ ] Mobile layout (1170x2532) collapses to compact top strip and bottom sheet.
- [ ] No text overlap or clipped buttons at 16:9, 21:9, and mobile portrait.

