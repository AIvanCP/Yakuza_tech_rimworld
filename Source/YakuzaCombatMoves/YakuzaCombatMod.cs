using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace YakuzaCombatMoves
{
    public class YakuzaCombatMod : Mod
    {
        public static YakuzaSettings settings;
        
        public YakuzaCombatMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<YakuzaSettings>();
            
            // Apply Harmony patches
            var harmony = new Harmony("yakuza.combat.moves");
            harmony.PatchAll();
            
            Log.Message("[Yakuza Combat Moves] Mod loaded successfully!");
        }
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
        
        public override string SettingsCategory()
        {
            return "Yakuza Combat Moves";
        }
    }
}