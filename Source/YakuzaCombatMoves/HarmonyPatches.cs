using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
using System.Linq;
using System;

namespace YakuzaCombatMoves
{
    /// <summary>
    /// Harmony patches to integrate Yakuza techniques into RimWorld's combat system
    /// Compatible with other combat mods by using postfix patches and stable methods
    /// </summary>
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        private static bool processingTechnique = false; // Prevent recursion
        
        // NOTE: Using Pawn_HealthTracker.PreApplyDamage for RimWorld 1.5/1.6 compatibility
        // This runs BEFORE damage is applied, allowing us to prevent/modify damage
        
        /// <summary>
        /// Primary patch for damage application - catches all melee and ranged attacks
        /// This is the MAIN hook for all techniques (RimWorld 1.5+ compatible)
        /// </summary>
        [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
        [HarmonyPrefix]
        public static void Pawn_HealthTracker_PreApplyDamage_Prefix(ref DamageInfo dinfo, out bool absorbed, Pawn ___pawn)
        {
            absorbed = false;
            
            if (processingTechnique || ___pawn == null || ___pawn.Dead) return;
            
            Pawn victim = ___pawn;
            
            try
            {
                // Only handle melee and ranged damage from other pawns
                if (dinfo.Instigator is Pawn attacker && attacker != victim)
                {
                    // Check if this is melee damage (close range, blunt/cut damage)
                    bool isMelee = (dinfo.Def == DamageDefOf.Cut || dinfo.Def == DamageDefOf.Blunt || dinfo.Def == DamageDefOf.Stab) &&
                                   attacker.Position.DistanceTo(victim.Position) <= 2f;
                    
                    if (isMelee)
                    {
                        // Log.Message($"[Yakuza Combat] Damage application: {attacker.LabelShort} → {victim.LabelShort} ({dinfo.Def.defName})");
                        
                        // Try to trigger defensive technique
                        processingTechnique = true;
                        bool techniqueTriggered = YakuzaTechniqueSystem.TryTriggerOnMeleeReceived(victim, attacker, dinfo);
                        processingTechnique = false;
                        
                        if (techniqueTriggered)
                        {
                            // Log.Message($"[Yakuza Combat] Damage negated by technique!");
                            absorbed = true; // Signal to game that damage was absorbed
                            dinfo.SetAmount(0f); // Zero out damage
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                processingTechnique = false;
                CompatibilityPatches.HandleModConflict(e, "Damage application patch");
            }
        }
        
        /// <summary>
        /// Patch ranged projectile impact to trigger dodge techniques
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "Impact")]
        [HarmonyPrefix]
        public static bool Projectile_Impact_Prefix(Thing hitThing, Projectile __instance)
        {
            try
            {
                if (hitThing is Pawn targetPawn)
                {
                    // Get launcher through reflection safely
                    var launcherField = typeof(Projectile).GetField("launcher", BindingFlags.NonPublic | BindingFlags.Instance);
                    var launcher = launcherField?.GetValue(__instance) as Thing;
                    
                    if (launcher is Pawn shooterPawn)
                    {
                        var dinfo = new DamageInfo(
                            __instance.def.projectile.damageDef,
                            __instance.def.projectile.GetDamageAmount(__instance),
                            __instance.def.projectile.GetArmorPenetration(__instance),
                            -1f,
                            shooterPawn,
                            null,
                            launcher?.def
                        );
                        
                        // Try to trigger ranged defense technique
                        if (YakuzaTechniqueSystem.TryTriggerOnRangedReceived(targetPawn, shooterPawn, dinfo))
                        {
                            // If technique triggered, negate the projectile
                            return false;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                CompatibilityPatches.HandleModConflict(e, "Projectile impact patch");
            }
            
            return true;
        }
        
        // Breakfall technique is handled through the regular damage system
        // No separate knockdown patch needed - techniques can prevent knockdown by healing/buffing
        
        /// <summary>
        /// Patch skill display to show true level
        /// </summary>
        [HarmonyPatch(typeof(SkillRecord), "get_LevelDescriptor")]
        [HarmonyPostfix]
        public static void SkillRecord_LevelDescriptor_Postfix(SkillRecord __instance, ref string __result)
        {
            try
            {
                if (!YakuzaCombatMod.settings.enableSkillUncap) return;
                
                int trueLevel = SkillUncap.GetTrueSkillLevel(__instance);
                if (trueLevel > 20)
                {
                    __result = trueLevel.ToString();
                }
            }
            catch (System.Exception e)
            {
                CompatibilityPatches.HandleModConflict(e, "Skill display patch");
            }
        }
        
        /// <summary>
        /// Safety initialization
        /// </summary>
        [HarmonyPatch(typeof(Game), "FinalizeInit")]
        [HarmonyPostfix]
        public static void Game_FinalizeInit_Postfix()
        {
            // Log.Message("[Yakuza Combat Mastery] Techniques initialized and ready for combat!");
        }
    }
    
    /// <summary>
    /// Additional patches for mod compatibility
    /// </summary>
    [HarmonyPatch]
    public static class CompatibilityPatches
    {
        private static bool modCompatibilityChecked = false;
        private static bool cqcModDetected = false;
        private static bool combatExtendedDetected = false;
        
        /// <summary>
        /// Check for conflicting mods and adjust behavior
        /// </summary>
        public static void InitializeCompatibility()
        {
            if (modCompatibilityChecked) return;
            
            try
            {
                // Check for CQC mod
                cqcModDetected = ModLister.AllInstalledMods.Any(m => 
                    m.Name.ToLower().Contains("cqc") || 
                    m.Name.ToLower().Contains("close quarters") ||
                    m.PackageId.ToLower().Contains("cqc"));
                
                // Check for Combat Extended
                combatExtendedDetected = ModLister.AllInstalledMods.Any(m => 
                    m.Name.ToLower().Contains("combat extended") ||
                    m.PackageId.ToLower().Contains("combatextended"));
                
                if (cqcModDetected)
                {
                    // Log.Message("[Yakuza Combat] CQC mod detected - enabling compatibility mode");
                }
                
                if (combatExtendedDetected)
                {
                    // Log.Message("[Yakuza Combat] Combat Extended detected - using alternative patches");
                }
                
                modCompatibilityChecked = true;
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error checking mod compatibility: {e.Message}");
            }
        }
        
        /// <summary>
        /// Check if techniques should be disabled due to mod conflicts
        /// </summary>
        public static bool ShouldSkipTechnique(string techniqueName)
        {
            // If CQC mod is active, reduce technique frequency to avoid conflicts
            if (cqcModDetected && Rand.Value > 0.3f)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// General error handling to prevent mod conflicts
        /// </summary>
        public static void HandleModConflict(System.Exception e, string context)
        {
            Log.Error($"[Yakuza Combat Mastery] Error in {context}: {e.Message}");
            Log.Error($"[Yakuza Combat Mastery] Stack trace: {e.StackTrace}");
        }
    }
}