using UnityEngine;
using Verse;

namespace YakuzaCombatMoves
{
    public class YakuzaSettings : ModSettings
    {
        // Main toggle settings
        public bool enableMod = true;
        public bool playerOnly = false; // Default: enemies can also use moves
        public bool enableSkillUncap = true;
        
        // Move trigger chance modifiers
        public float moveChanceMultiplier = 1.0f;
        public float skillLevelInfluence = 1.0f;
        
        // Individual technique toggles
        public bool enableTigerDrop = true;
        public bool enableKomakiParry = true;
        public bool enableMadDogDodgeSlash = true;
        public bool enableKomakiKnockback = true;
        public bool enableMajimaHeatSpin = true;
        public bool enableKomakiBreakfall = true;
        public bool enableCatLikeReflexes = true;
        public bool enableWallCrush = true;
        public bool enableMadDogLunge = true;
        public bool enableFirearmCounter = true;
        
        // Effect settings
        public float debuffDurationMultiplier = 1.0f;
        public bool enableMoveText = true; // Show move names when triggered
        
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableMod, "enableMod", true);
            Scribe_Values.Look(ref playerOnly, "playerOnly", false);
            Scribe_Values.Look(ref enableSkillUncap, "enableSkillUncap", true);
            Scribe_Values.Look(ref moveChanceMultiplier, "moveChanceMultiplier", 1.0f);
            Scribe_Values.Look(ref skillLevelInfluence, "skillLevelInfluence", 1.0f);
            Scribe_Values.Look(ref enableTigerDrop, "enableTigerDrop", true);
            Scribe_Values.Look(ref enableKomakiParry, "enableKomakiParry", true);
            Scribe_Values.Look(ref enableMadDogDodgeSlash, "enableMadDogDodgeSlash", true);
            Scribe_Values.Look(ref enableKomakiKnockback, "enableKomakiKnockback", true);
            Scribe_Values.Look(ref enableMajimaHeatSpin, "enableMajimaHeatSpin", true);
            Scribe_Values.Look(ref enableKomakiBreakfall, "enableKomakiBreakfall", true);
            Scribe_Values.Look(ref enableCatLikeReflexes, "enableCatLikeReflexes", true);
            Scribe_Values.Look(ref enableWallCrush, "enableWallCrush", true);
            Scribe_Values.Look(ref enableMadDogLunge, "enableMadDogLunge", true);
            Scribe_Values.Look(ref enableFirearmCounter, "enableFirearmCounter", true);
            Scribe_Values.Look(ref debuffDurationMultiplier, "debuffDurationMultiplier", 1.0f);
            Scribe_Values.Look(ref enableMoveText, "enableMoveText", true);
            base.ExposeData();
        }
        
        public void DoWindowContents(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            // Main toggles
            listingStandard.CheckboxLabeled("Enable Yakuza Combat Moves", ref enableMod, 
                "Toggle the entire mod on/off");
            
            if (enableMod)
            {
                listingStandard.CheckboxLabeled("Player Only", ref playerOnly, 
                    "If checked, only player pawns can use Yakuza moves. If unchecked, enemies can also use them.");
                
                listingStandard.CheckboxLabeled("Enable Skill Uncap", ref enableSkillUncap, 
                    "Allow skills to exceed level 20 (up to level 999)");
                
                listingStandard.Gap();
                
                // Chance modifiers
                listingStandard.Label("Move Trigger Chance: " + (moveChanceMultiplier * 100f).ToString("F0") + "%");
                moveChanceMultiplier = listingStandard.Slider(moveChanceMultiplier, 0.1f, 3.0f);
                
                listingStandard.Label("Skill Level Influence: " + (skillLevelInfluence * 100f).ToString("F0") + "%");
                skillLevelInfluence = listingStandard.Slider(skillLevelInfluence, 0.1f, 2.0f);
                
                listingStandard.Gap();
                
                // Individual techniques
                listingStandard.Label("Combat Techniques:");
                listingStandard.CheckboxLabeled("Tiger Drop (Unarmed Counter)", ref enableTigerDrop);
                listingStandard.CheckboxLabeled("Komaki Parry (Katana Defense)", ref enableKomakiParry);
                listingStandard.CheckboxLabeled("Mad Dog Dodge Slash (Knife)", ref enableMadDogDodgeSlash);
                listingStandard.CheckboxLabeled("Komaki Knockback (Club)", ref enableKomakiKnockback);
                listingStandard.CheckboxLabeled("Majima Heat Spin (AoE)", ref enableMajimaHeatSpin);
                listingStandard.CheckboxLabeled("Komaki Breakfall (Recovery)", ref enableKomakiBreakfall);
                listingStandard.CheckboxLabeled("Cat-Like Reflexes (Dodge)", ref enableCatLikeReflexes);
                listingStandard.CheckboxLabeled("Wall Crush (Environmental)", ref enableWallCrush);
                listingStandard.CheckboxLabeled("Mad Dog Lunge (Dash Attack)", ref enableMadDogLunge);
                listingStandard.CheckboxLabeled("Firearm Counter (Gun Defense)", ref enableFirearmCounter);
                
                listingStandard.Gap();
                
                // Effect settings
                listingStandard.Label("Debuff Duration: " + (debuffDurationMultiplier * 100f).ToString("F0") + "%");
                debuffDurationMultiplier = listingStandard.Slider(debuffDurationMultiplier, 0.5f, 2.0f);
                
                listingStandard.CheckboxLabeled("Show Move Names", ref enableMoveText, 
                    "Display the name of the move when it's triggered");
            }
            
            listingStandard.End();
        }
    }
}