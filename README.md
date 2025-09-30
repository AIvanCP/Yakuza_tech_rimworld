# Yakuza Combat Mastery

Master the legendary combat techniques from the Yakuza series in RimWorld. This mod adds 10 reactive combat abilities that trigger automatically based on a pawn's skills and the combat situation.

## Table of contents

- [Features](#features)
- [Techniques](#techniques)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Mechanics](#mechanics)
- [Custom Injury Names](#custom-injury-names)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Changelog](#changelog)
- [License](#license)

## Features

- **10 Reactive Combat Techniques** - Automatic counters, dodges, and special attacks
- **Weapon-Specific Moves** - Each technique requires certain weapon types
- **Skill-Based Scaling** - Higher melee skill increases trigger chance and effectiveness
- **Optional Skill Uncap** - Allow skills beyond level 20 (compatible with other uncap mods)
- **Custom Injury Names** - Techniques show their names in the health tab (e.g., "Tiger Drop injury")
- **Visual Feedback** - Dust, sparks, and impact particles when techniques trigger
- **Sound Effects** - Custom audio cues (place sound files in `Sounds/Yakuza/` folder)
- **No Instant Kills** - Lethal damage is converted to downed state for balance
- **No Draft Requirement** - Works for both drafted and undrafted colonists
- **Player-Only Toggle** - Restrict techniques to player pawns
- **Individual Toggles** - Enable/disable each technique in mod settings
- **Harmony-Based** - Safe, non-destructive patching

## Techniques

### Defensive Techniques (trigger when attacked)

1. **Tiger Drop** (unarmed)
   - Counter-attack with devastating damage and stun
   - 30% chance for random debuff (slow/weaken/armor shred)
   - Shows as "Tiger Drop" damage in health tab

2. **Komaki Parry** (katanas, long swords)
   - Parry and counter with precise slashing attack
   - Shows as "Komaki Parry" damage in health tab

3. **Mad Dog Dodge Slash** (knife, dagger)
   - Dodge then counter-slash with enhanced bleeding chance
   - Shows as "Mad Dog Slash" damage in health tab

4. **Komaki Knockback** (clubs, maces, batons)
   - Blunt counter that staggers attacker
   - Shows as "Komaki Knockback" damage in health tab

### Environmental Techniques

1. **Wall Crush** (near walls with blunt weapons)
   - Slam enemy into walls for 50% bonus damage
   - Extended stun and armor shred chance
   - Shows as "Wall Crush" damage in health tab

2. **Majima Heat Spin** (when surrounded by 2+ enemies)
   - AoE spin attack hitting all adjacent enemies
   - Applies slow debuff to hit enemies
   - Shows as "Heat Spin" damage in health tab

### Mobility Techniques

1. **Komaki Breakfall** (any weapon)
   - Chance to avoid knockdown and recover immediately
   - Applies enhanced reflexes buff when successful

2. **Cat-like Reflexes** (unarmed vs ranged)
   - Chance to dodge incoming projectiles
   - Applies enhanced reflexes buff when successful

### Aggressive Techniques

1. **Mad Dog Lunge** (knife)
   - Dash attack with enhanced bleeding chance
   - Shows as "Mad Dog Lunge" damage in health tab

2. **Firearm Counter** (firearms)
   - Point-blank counter shot with disorientation effect
   - Shows as "Firearm Counter" damage in health tab

## Requirements

- RimWorld 1.4 or 1.5
- Harmony (bundled with mod)

## Installation

### Automatic

1. Download and extract the mod files
2. Copy the `yakuza_mov` folder into your RimWorld `Mods` directory
3. Enable the mod in RimWorld's mod list

### Manual (for developers)

1. Clone this repository
2. Build the project using PowerShell:

```powershell
dotnet build Source/YakuzaCombatMoves/YakuzaCombatMoves.csproj --configuration Release
```

3. Copy the resulting `Assemblies/YakuzaCombatMoves.dll` into the mod's `Assemblies` folder

## Configuration

Access mod settings through **Options → Mod Settings → Yakuza Combat Moves**

### Key Settings

- **Enable Mod** - Master on/off toggle
- **Player Only** - Restrict techniques to colonists only
- **Move Chance Multiplier** - Global multiplier for trigger chances (0.1x - 3.0x)
- **Enable Skill Uncap** - Allow this mod to handle skill uncapping (disable if using other uncap mods)
- **Enable Uncapped Scaling** - Allow technique chances to grow beyond level 20 (recommended: ON)
- **Individual Technique Toggles** - Enable/disable each of the 10 techniques

Settings apply immediately; no restart required.

## Mechanics

### Unified Scaling System

- **Base Chance**: 5% trigger chance at level 0
- **Level 1-20**: +1% per level (25% at level 20)
- **Level 20+**: +0.1% per level (if uncapped scaling enabled)
- **Hard Cap**: 50% maximum trigger chance
- **Damage Scaling**: Capped at level 20 for balance

### Compatibility with Other Mods

- **Skill Uncap Mods**: Set "Enable Skill Uncap" to OFF, "Enable Uncapped Scaling" to ON
- **Combat Overhauls**: Generally compatible; may need load order adjustment
- **Weapon Mods**: Auto-detects weapon types by name matching

### Safety Features

- **Trigger Timing** - Techniques only evaluate on confirmed hits (melee) or projectile impact (ranged)
- **No Draft Requirement** - Works for both drafted and undrafted colonists
- **Instant Kill Prevention** - Lethal outcomes are reduced to downed states
- **One Per Attack** - Only one technique can trigger per attack to prevent stacking
- **Mental State Protection** - Techniques disabled during mental breaks

## Custom Injury Names

When a technique damages a pawn, it appears in the health tab with the technique name:

- "Tiger Drop" injuries from Tiger Drop technique
- "Komaki Parry" cuts from Komaki Parry
- "Wall Crush" damage from Wall Crush technique
- And so on for all techniques

This makes it clear which technique caused specific injuries, helpful for medical prioritization and storytelling.

## Troubleshooting

**Techniques not triggering:**

- Ensure the mod is enabled and technique toggles are on
- Check that pawn meets weapon/skill requirements
- Verify attacks are actually hitting (techniques don't trigger on misses)
- Check if pawn is in mental state (techniques disabled during breaks)

**Mod settings not showing:**

- Verify mod is properly loaded in the mods list
- Check mod load order (should be after core game)
- Restart RimWorld if settings don't appear

**Skills not increasing beyond 20:**

- If using another uncap mod: set "Enable Skill Uncap" to OFF
- If using this mod's uncap: set "Enable Skill Uncap" to ON
- Existing colonists may need to gain XP to update

**Game crashes during combat:**

- Check for mod conflicts with other combat overhauls
- Try disabling other combat mods temporarily
- Report issues with full mod list and game log

Enable Developer Mode and check the RimWorld log for `[Yakuza Combat]` entries when diagnosing issues.

## Contributing

See `CONTRIBUTING.md` for development setup, coding guidelines, and how to submit pull requests.

## Changelog

### v1.0.0

- Initial release with 10 combat techniques
- Unified scaling system with level 20 damage cap
- Custom injury names for all techniques
- Compatible with other skill uncap mods
- No draft requirement for technique usage
- Enhanced visual and audio feedback

## License

This mod is provided as-is for the RimWorld modding community.

- **RimWorld** is the property of Ludeon Studios
- **Yakuza** is a property of SEGA
- This mod is created by fans for educational and entertainment purposes

---

**Enjoy mastering the Dragon of Dojima's techniques in your colony!**