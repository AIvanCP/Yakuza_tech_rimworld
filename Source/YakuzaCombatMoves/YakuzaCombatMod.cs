using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace YakuzaCombatMoves
{
    [StaticConstructorOnStartup]
    public class YakuzaCombatMod : Mod
    {
        public static YakuzaSettings settings;
        
        public YakuzaCombatMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<YakuzaSettings>();
            
            try
            {
                // Initialize compatibility checking
                CompatibilityPatches.InitializeCompatibility();
                
                // Apply Harmony patches
                var harmony = new Harmony("yakuza.combat.moves");
                // Use PatchAll but guard against failures so a single bad patch doesn't abort the whole mod
                try
                {
                    harmony.PatchAll();
                    Log.Message("[Yakuza Combat Moves] Mod loaded successfully!");
                }
                catch (System.Exception patchEx)
                {
                    // Log detailed info about the patch exception, including inner exceptions
                    Log.Error("[Yakuza Combat Moves] Harmony PatchAll threw an exception.");
                    System.Exception inner = patchEx;
                    int depth = 0;
                    while (inner != null && depth < 10)
                    {
                        Log.Error($"[Yakuza Combat Moves] Patch exception (depth {depth}): {inner.GetType().FullName}: {inner.Message}");
                        Log.Error(inner.StackTrace ?? "(no stacktrace)");
                        inner = inner.InnerException;
                        depth++;
                    }
                    // Still continue; individual patches may be fixed later
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[Yakuza Combat Moves] Failed to initialize: {e.Message}");
                Log.Error($"[Yakuza Combat Moves] Stack trace: {e.StackTrace}");
            }
        }
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
        
        public override string SettingsCategory()
        {
            return "Yakuza Combat Moves";
        }
        
        public override void WriteSettings()
        {
            base.WriteSettings();
            Log.Message("[Yakuza Combat Moves] Settings saved.");
        }
    }
}