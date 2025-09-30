using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace YakuzaCombatMoves
{
    /// <summary>
    /// Skill uncap system that allows skills to exceed level 100
    /// </summary>
    public static class SkillUncap
    {
        public const int MaxSkillLevel = 999; // Maximum skill level
        public const int VanillaMaxLevel = 20; // RimWorld's default max level
        
        /// <summary>
        /// Calculate skill level including uncapped levels
        /// </summary>
        public static int GetTrueSkillLevel(SkillRecord skill)
        {
            if (skill == null) return 0;
            
            // Use the total experience to calculate true level
            float totalXP = skill.xpSinceLastLevel + (skill.Level * skill.XpRequiredForLevelUp);
            return CalculateLevelFromXP(totalXP);
        }
        
        /// <summary>
        /// Calculate level from total experience
        /// </summary>
        public static int CalculateLevelFromXP(float totalXP)
        {
            int level = 0;
            float xpRequired = 1000f; // Base XP required for level 1
            
            while (totalXP >= xpRequired && level < MaxSkillLevel)
            {
                totalXP -= xpRequired;
                level++;
                
                // XP requirement increases with level
                xpRequired = 1000f + (level * 50f);
            }
            
            return level;
        }
        
        /// <summary>
        /// Get skill level percentage for display (0-1)
        /// </summary>
        public static float GetSkillLevelPercentage(SkillRecord skill)
        {
            return UnityEngine.Mathf.Clamp01((float)GetTrueSkillLevel(skill) / MaxSkillLevel);
        }
    }
    
    /// <summary>
    /// Harmony patches for skill uncap functionality
    /// </summary>
    [HarmonyPatch]
    public static class SkillUncapPatches
    {
        /// <summary>
        /// Patch to allow skills to level beyond 20 (only if mod setting enabled)
        /// </summary>
        [HarmonyPatch(typeof(SkillRecord), "Learn")]
        [HarmonyPrefix]
        public static bool SkillRecord_Learn_Prefix(SkillRecord __instance, float xp, bool direct = false)
        {
            // Only apply uncap if our mod setting is enabled to avoid conflicts
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
                
                // Don't cap at 20, allow unlimited leveling
                if (__instance.Level >= SkillUncap.MaxSkillLevel)
                {
                    __instance.Level = SkillUncap.MaxSkillLevel;
                    __instance.xpSinceLastLevel = 0f;
                    break;
                }
            }
            
            return false; // Skip original method
        }
        
        /// <summary>
        /// Patch skill display to show true level
        /// </summary>
        [HarmonyPatch(typeof(SkillRecord), "get_LevelDescriptor")]
        [HarmonyPostfix]
        public static void SkillRecord_LevelDescriptor_Postfix(SkillRecord __instance, ref string __result)
        {
            int trueLevel = SkillUncap.GetTrueSkillLevel(__instance);
            if (trueLevel > 20) // Vanilla max level
            {
                __result = trueLevel.ToString();
            }
        }
    }
}