using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace YakuzaCombatMoves
{
    /// <summary>
    /// Weapon categories for move restrictions
    /// </summary>
    public enum YakuzaWeaponType
    {
        Unarmed,
        Katana,
        Knife,
        Club,
        Gun,
        Any
    }

    /// <summary>
    /// Trigger conditions for moves
    /// </summary>
    public enum MoveTrigger
    {
        OnMeleeAttackReceived,
        OnRangedAttackReceived,
        OnKnockdownAttempt,
        OnSurrounded,
        OnNearWall
    }

    /// <summary>
    /// Base class for all Yakuza combat techniques
    /// </summary>
    public abstract class YakuzaTechnique
    {
        public abstract string TechniqueName { get; }
        public abstract string Description { get; }
        public abstract YakuzaWeaponType RequiredWeapon { get; }
        public abstract MoveTrigger TriggerCondition { get; }
        public abstract float BaseChance { get; }
        public abstract float SkillScaling { get; }
        
        /// <summary>
        /// Calculate trigger chance based on melee skill
        /// </summary>
        public virtual float CalculateTriggerChance(Pawn pawn)
        {
            if (!YakuzaCombatMod.settings.enableMod) return 0f;
            
            var meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee);
            if (meleeSkill == null) return 0f;
            
            int skillLevel = YakuzaCombatMod.settings.enableSkillUncap 
                ? SkillUncap.GetTrueSkillLevel(meleeSkill) 
                : meleeSkill.Level;
            
            float chance = BaseChance + (skillLevel * SkillScaling);
            chance *= YakuzaCombatMod.settings.moveChanceMultiplier;
            
            return Mathf.Clamp01(chance);
        }
        
        /// <summary>
        /// Check if pawn can use this technique with comprehensive safety checks
        /// </summary>
        public virtual bool CanUseTechnique(Pawn pawn)
        {
            try
            {
                // Basic safety checks
                if (pawn == null || pawn.Dead || pawn.Downed) return false;
                if (!YakuzaCombatMod.settings.enableMod) return false;
                if (YakuzaCombatMod.settings.playerOnly && !pawn.IsColonist) return false;
                
                // Prevent techniques during mental states that would interfere
                if (pawn.InMentalState || (pawn.IsColonist && pawn.Drafted == false)) return false;
                
                // Check if already using a technique (prevent loops)
                if (pawn.stances?.curStance?.GetType()?.Name?.Contains("Yakuza") == true) return false;
                
                return IsWeaponCompatible(pawn) && IsConditionMet(pawn);
            }
            catch (System.Exception e)
            {
                Log.Error($"[Yakuza Combat] Error in CanUseTechnique for {pawn?.LabelShort}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check weapon compatibility
        /// </summary>
        protected virtual bool IsWeaponCompatible(Pawn pawn)
        {
            var weapon = pawn.equipment?.Primary;
            
            switch (RequiredWeapon)
            {
                case YakuzaWeaponType.Unarmed:
                    return weapon == null;
                
                case YakuzaWeaponType.Katana:
                    return weapon?.def.defName.ToLower().Contains("katana") == true ||
                           weapon?.def.defName.ToLower().Contains("longsword") == true;
                
                case YakuzaWeaponType.Knife:
                    return weapon?.def.defName.ToLower().Contains("knife") == true ||
                           weapon?.def.defName.ToLower().Contains("dagger") == true;
                
                case YakuzaWeaponType.Club:
                    return weapon?.def.IsMeleeWeapon == true && 
                           (weapon.def.defName.ToLower().Contains("mace") || 
                            weapon.def.defName.ToLower().Contains("club") ||
                            weapon.def.defName.ToLower().Contains("hammer"));
                
                case YakuzaWeaponType.Gun:
                    return weapon?.def.IsRangedWeapon == true;
                
                case YakuzaWeaponType.Any:
                    return true;
                
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check if special conditions are met
        /// </summary>
        protected virtual bool IsConditionMet(Pawn pawn)
        {
            switch (TriggerCondition)
            {
                case MoveTrigger.OnSurrounded:
                    return CountAdjacentEnemies(pawn) >= 2;
                
                case MoveTrigger.OnNearWall:
                    return IsNearWall(pawn);
                
                default:
                    return true;
            }
        }
        
        protected int CountAdjacentEnemies(Pawn pawn)
        {
            if (pawn.Map == null) return 0;
            
            int count = 0;
            foreach (var cell in GenAdj.CellsAdjacent8Way(pawn))
            {
                if (!cell.InBounds(pawn.Map)) continue;
                
                var things = cell.GetThingList(pawn.Map);
                foreach (var thing in things)
                {
                    if (thing is Pawn otherPawn && otherPawn.HostileTo(pawn))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        
        protected bool IsNearWall(Pawn pawn)
        {
            if (pawn.Map == null) return false;
            
            foreach (var cell in GenAdj.CellsAdjacent8Way(pawn))
            {
                if (!cell.InBounds(pawn.Map)) continue;
                
                var edifice = cell.GetEdifice(pawn.Map);
                if (edifice != null && edifice.def.blockWind)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Execute the technique
        /// </summary>
        public abstract bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage);
        
        /// <summary>
        /// Show technique message and effects
        /// </summary>
        protected virtual void ShowTechniqueEffect(Pawn user, string message = null)
        {
            if (YakuzaCombatMod.settings.enableMoveText)
            {
                string msg = message ?? $"{user.LabelShort} uses {TechniqueName}!";
                Messages.Message(msg, user, MessageTypeDefOf.NeutralEvent, false);
            }
        }
        
        /// <summary>
        /// Prevent instant kills by converting to downed
        /// </summary>
        protected virtual float PreventInstantKill(Pawn target, float damage)
        {
            if (target.health.summaryHealth.SummaryHealthPercent <= 0.1f)
            {
                // If damage would be lethal, reduce it to knock down instead
                float maxSafeDamage = target.health.summaryHealth.SummaryHealthPercent * 0.8f;
                return Mathf.Min(damage, maxSafeDamage);
            }
            return damage;
        }
    }
    
    /// <summary>
    /// Manages all Yakuza techniques and their execution
    /// </summary>
    public static class YakuzaTechniqueSystem
    {
        private static List<YakuzaTechnique> allTechniques;
        
        static YakuzaTechniqueSystem()
        {
            InitializeTechniques();
        }
        
        private static void InitializeTechniques()
        {
            allTechniques = new List<YakuzaTechnique>
            {
                new TigerDropTechnique(),
                new KomakiParryTechnique(),
                new MadDogDodgeSlashTechnique(),
                new KomakiKnockbackTechnique(),
                new MajimaHeatSpinTechnique(),
                new KomakiBreakfallTechnique(),
                new CatLikeReflexesTechnique(),
                new WallCrushTechnique(),
                new MadDogLungeTechnique(),
                new FirearmCounterTechnique()
            };
        }
        
        /// <summary>
        /// Try to trigger a technique on melee attack received
        /// </summary>
        public static bool TryTriggerOnMeleeReceived(Pawn defender, Pawn attacker, DamageInfo damageInfo)
        {
            if (!YakuzaCombatMod.settings.enableMod) return false;
            if (defender?.Dead != false || attacker?.Dead != false) return false;
            
            var availableTechniques = GetAvailableTechniques(defender, MoveTrigger.OnMeleeAttackReceived);
            
            foreach (var technique in availableTechniques)
            {
                float chance = technique.CalculateTriggerChance(defender);
                if (Rand.Chance(chance))
                {
                    return technique.ExecuteTechnique(defender, attacker, damageInfo);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Try to trigger a technique on ranged attack received
        /// </summary>
        public static bool TryTriggerOnRangedReceived(Pawn defender, Pawn attacker, DamageInfo damageInfo)
        {
            if (!YakuzaCombatMod.settings.enableMod) return false;
            if (defender?.Dead != false || attacker?.Dead != false) return false;
            
            var availableTechniques = GetAvailableTechniques(defender, MoveTrigger.OnRangedAttackReceived);
            
            foreach (var technique in availableTechniques)
            {
                float chance = technique.CalculateTriggerChance(defender);
                if (Rand.Chance(chance))
                {
                    return technique.ExecuteTechnique(defender, attacker, damageInfo);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Try to trigger a technique on knockdown attempt
        /// </summary>
        public static bool TryTriggerOnKnockdown(Pawn pawn)
        {
            if (!YakuzaCombatMod.settings.enableMod) return false;
            if (pawn?.Dead != false) return false;
            
            var availableTechniques = GetAvailableTechniques(pawn, MoveTrigger.OnKnockdownAttempt);
            
            foreach (var technique in availableTechniques)
            {
                float chance = technique.CalculateTriggerChance(pawn);
                if (Rand.Chance(chance))
                {
                    return technique.ExecuteTechnique(pawn, null, new DamageInfo());
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get available techniques for a pawn based on trigger condition
        /// </summary>
        public static List<YakuzaTechnique> GetAvailableTechniques(Pawn pawn, MoveTrigger trigger)
        {
            var available = new List<YakuzaTechnique>();
            foreach (var technique in allTechniques)
            {
                if (technique.TriggerCondition == trigger && technique.CanUseTechnique(pawn))
                {
                    available.Add(technique);
                }
            }
            return available;
        }
    }
}