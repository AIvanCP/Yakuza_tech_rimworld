using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace YakuzaCombatMoves
{
    /// <summary>
    /// Harmony patches to integrate Yakuza techniques into RimWorld's combat system
    /// </summary>
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        /// <summary>
        /// Patch melee attacks to potentially trigger defensive techniques
        /// NOTE: This only triggers on successful hits - dodged attacks never reach this method
        /// </summary>
        [HarmonyPatch(typeof(Verb_MeleeAttack), "ApplyMeleeDamageToTarget")]
        [HarmonyPrefix]
        public static bool Verb_MeleeAttack_ApplyMeleeDamageToTarget_Prefix(LocalTargetInfo target, Verb_MeleeAttack __instance)
        {
            try
            {
                // Safety checks to prevent null reference exceptions
                if (__instance?.CasterPawn == null || !(target.Thing is Pawn targetPawn) || targetPawn.Dead)
                {
                    return true; // Continue with normal damage if invalid state
                }
                
                // Prevent infinite loops - don't trigger techniques during technique execution
                if (targetPawn.health?.hediffSet?.HasHediff(HediffDefOf.Anesthetic) == true ||
                    targetPawn.stances?.stunner?.Stunned == true)
                {
                    return true; // Continue normal damage if already processing effects
                }
                
                // Create damage info for technique calculation
                var dinfo = new DamageInfo(
                    __instance.tool?.capacities?.FirstOrDefault(c => c.defName == "Cut") != null ? DamageDefOf.Cut : DamageDefOf.Blunt,
                    __instance.tool?.power ?? 10f,
                    0f,
                    -1f,
                    __instance.CasterPawn,
                    null,
                    __instance.EquipmentSource?.def
                );
                
                // Try to trigger defensive technique on the target
                // This only happens if the attack successfully hit (not dodged)
                if (YakuzaTechniqueSystem.TryTriggerOnMeleeReceived(targetPawn, __instance.CasterPawn, dinfo))
                {
                    // If a defensive technique was triggered, skip normal damage
                    return false;
                }
            }
            catch (System.Exception e)
            {
                CompatibilityPatches.HandleModConflict(e, "Melee attack patch");
            }
            
            return true; // Continue with normal damage
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
        
        /// <summary>
        /// Patch knockdown attempts to trigger breakfall
        /// </summary>
        [HarmonyPatch(typeof(Pawn_HealthTracker), "SetPawnDowned")]
        [HarmonyPrefix]
        public static bool Pawn_HealthTracker_SetPawnDowned_Prefix(Pawn_HealthTracker __instance)
        {
            try
            {
                // Get pawn through reflection
                var pawnField = typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
                var pawn = pawnField?.GetValue(__instance) as Pawn;
                
                if (pawn != null && !pawn.Dead)
                {
                    // Try to trigger breakfall technique
                    if (YakuzaTechniqueSystem.TryTriggerOnKnockdown(pawn))
                    {
                        // If breakfall triggered, prevent knockdown
                        return false;
                    }
                }
            }
            catch (System.Exception e)
            {
                CompatibilityPatches.HandleModConflict(e, "Knockdown prevention patch");
            }
            
            return true;
        }
        
        /// <summary>
        /// Patch skill learning to support uncapped levels
        /// </summary>
        [HarmonyPatch(typeof(SkillRecord), "Learn")]
        [HarmonyPrefix]
        public static bool SkillRecord_Learn_Prefix(SkillRecord __instance, float xp, bool direct = false)
        {
            try
            {
                if (!YakuzaCombatMod.settings.enableSkillUncap) return true;
                
                // Don't interfere with disabled skills
                if (__instance.TotallyDisabled) return false;
                
                // Allow learning regardless of current level
                __instance.xpSinceLastLevel += xp * __instance.LearnRateFactor(direct);
                
                // Check if we should level up
                while (__instance.xpSinceLastLevel >= __instance.XpRequiredForLevelUp)
                {
                    __instance.xpSinceLastLevel -= __instance.XpRequiredForLevelUp;
                    __instance.Level++;
                    
                    // Cap at max uncapped level
                    if (__instance.Level >= SkillUncap.MaxSkillLevel)
                    {
                        __instance.Level = SkillUncap.MaxSkillLevel;
                        __instance.xpSinceLastLevel = 0f;
                        break;
                    }
                }
                
                return false; // Skip original method
            }
            catch (System.Exception e)
            {
                CompatibilityPatches.HandleModConflict(e, "Skill learning patch");
                return true;
            }
        }
        
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
        /// Patch skill cap to allow unlimited growth
        /// </summary>
        [HarmonyPatch(typeof(SkillRecord), "get_MaxLevel")]
        [HarmonyPostfix]
        public static void SkillRecord_MaxLevel_Postfix(SkillRecord __instance, ref int __result)
        {
            try
            {
                if (YakuzaCombatMod.settings.enableSkillUncap)
                {
                    __result = SkillUncap.MaxSkillLevel;
                }
            }
            catch (System.Exception e)
            {
                CompatibilityPatches.HandleModConflict(e, "Skill cap patch");
            }
        }
        
        /// <summary>
        /// Safety initialization
        /// </summary>
        [HarmonyPatch(typeof(Game), "FinalizeInit")]
        [HarmonyPostfix]
        public static void Game_FinalizeInit_Postfix()
        {
            Log.Message("[Yakuza Combat Mastery] Techniques initialized and ready for combat!");
        }
    }
    
    /// <summary>
    /// Additional patches for mod compatibility
    /// </summary>
    [HarmonyPatch]
    public static class CompatibilityPatches
    {
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