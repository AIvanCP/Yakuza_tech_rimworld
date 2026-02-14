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
        /// Calculate trigger chance based on melee skill using unified mechanics
        /// </summary>
        public virtual float CalculateTriggerChance(Pawn pawn)
        {
            if (!YakuzaCombatMod.settings.enableMod) 
            {
                // Log.Message($"[Yakuza Combat] Mod disabled for {pawn.LabelShort}");
                return 0f;
            }
            
            var meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee);
            if (meleeSkill == null) 
            {
                // Log.Message($"[Yakuza Combat] No melee skill found for {pawn.LabelShort}");
                return 0f;
            }
            
            int skillLevel = YakuzaCombatMod.settings.enableSkillUncap 
                ? SkillUncap.GetTrueSkillLevel(meleeSkill) 
                : meleeSkill.Level;
            
            // Log.Message($"[Yakuza Combat] {pawn.LabelShort} melee skill level: {skillLevel}");
            
            // Balanced scaling: baseMoveChance (configurable) + 1% per level to 20 + scaled beyond 20
            float chance = YakuzaCombatMod.settings.baseMoveChance; // configurable base chance
            
            if (skillLevel <= 20)
            {
                // Standard scaling: +1% per level up to 20, modifiable by skillLevelInfluence
                chance += skillLevel * 0.01f * YakuzaCombatMod.settings.skillLevelInfluence;
            }
            else if (YakuzaCombatMod.settings.enableUncappedScaling)
            {
                // Cap at level 20 base, then slower scaling beyond
                chance += 20 * 0.01f * YakuzaCombatMod.settings.skillLevelInfluence; // 20% from first 20 levels
                chance += (skillLevel - 20) * 0.002f * YakuzaCombatMod.settings.skillLevelInfluence; // +0.2% per level beyond 20
            }
            else
            {
                // If uncapped scaling disabled, cap at level 20 values
                chance += 20 * 0.01f * YakuzaCombatMod.settings.skillLevelInfluence;
            }
            
            // Apply global multiplier
            chance *= YakuzaCombatMod.settings.moveChanceMultiplier;
            
            // Hard cap at 50% for balance
            float finalChance = Mathf.Clamp01(Mathf.Min(chance, 0.5f));
            
            // Debug logging
            // Log.Message($"[Yakuza Combat] {pawn.LabelShort} technique chance: {finalChance:P1} (skill level {skillLevel})");
            
            return finalChance;
        }
        
        /// <summary>
        /// Calculate debuff chance based on skill level (increases beyond level 20)
        /// </summary>
        protected virtual float CalculateDebuffChance(Pawn pawn, float baseChance = 0.3f)
        {
            if (!YakuzaCombatMod.settings.enableMod) return baseChance;
            
            var meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee);
            if (meleeSkill == null) return baseChance;
            
            int skillLevel = YakuzaCombatMod.settings.enableSkillUncap 
                ? SkillUncap.GetTrueSkillLevel(meleeSkill) 
                : meleeSkill.Level;
            
            float debuffChance = baseChance;
            
            // Increase debuff chance for high skill levels
            if (skillLevel > 20 && YakuzaCombatMod.settings.enableUncappedScaling)
            {
                // +1% debuff chance per level beyond 20, capped at 80%
                debuffChance += (skillLevel - 20) * 0.01f;
                debuffChance = Mathf.Min(debuffChance, 0.8f);
            }
            
            return debuffChance;
        }
        
        /// <summary>
        /// Calculate damage with level 20 cap for damage scaling
        /// </summary>
        protected virtual float CalculateScaledDamage(Pawn pawn, float baseDamage, float maxDamage = 50f)
        {
            var meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee);
            if (meleeSkill == null) return baseDamage;
            
            // Always cap damage calculations at level 20 for balance
            int effectiveLevel = Mathf.Min(meleeSkill.Level, 20);
            float damageFactor = pawn.GetStatValue(StatDefOf.MeleeDamageFactor);
            
            float scaledDamage = baseDamage + (effectiveLevel * 1.5f) * damageFactor;
            return Mathf.Min(scaledDamage, maxDamage);
        }
        
        /// <summary>
        /// Check if pawn can use this technique with comprehensive safety checks
        /// </summary>
        public virtual bool CanUseTechnique(Pawn pawn)
        {
            try
            {
                // Basic safety checks
                if (pawn == null || pawn.Dead || pawn.Downed)
                {
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: pawn invalid (null={pawn == null}, dead={pawn?.Dead}, downed={pawn?.Downed})");
                    return false;
                }
                if (!YakuzaCombatMod.settings.enableMod)
                {
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: mod disabled");
                    return false;
                }
                
                // Player-only check (if setting enabled)
                if (YakuzaCombatMod.settings.playerOnly && !pawn.IsColonist && pawn.Faction != Faction.OfPlayer)
                {
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: player only, pawn is not player faction (colonist={pawn.IsColonist}, faction={pawn.Faction?.Name})");
                    return false;
                }
                
                // Log.Message($"[Yakuza Combat] {TechniqueName}: pawn faction check - colonist={pawn.IsColonist}, faction={pawn.Faction?.Name}, playerOnly={YakuzaCombatMod.settings.playerOnly}");
                
                // Prevent techniques during mental states that would interfere
                if (pawn.InMentalState)
                {
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: pawn in mental state");
                    return false;
                }
                
                // Check if already using a technique (prevent loops)
                if (pawn.stances?.curStance?.GetType()?.Name?.Contains("Yakuza") == true)
                {
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: already using technique");
                    return false;
                }
                
                // Check for incapacitating conditions (stun, anesthesia) that should always block techniques
                if (pawn.health?.hediffSet != null)
                {
                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        // Block if pawn has serious debuffs that prevent action
                        if (hediff.def.defName.Contains("Stun") || 
                            hediff.def.defName.Contains("Anesthetic") ||
                            hediff.def.defName.Contains("Unconscious"))
                        {
                            // Log.Message($"[Yakuza Combat] {TechniqueName}: pawn has incapacitating hediff {hediff.def.defName}");
                            return false;
                        }
                    }
                }
                
                // Defensive/reactive techniques (parry, counter) should work even during attack cooldown
                bool isDefensiveTechnique = TriggerCondition == MoveTrigger.OnMeleeAttackReceived || 
                                           TriggerCondition == MoveTrigger.OnRangedAttackReceived ||
                                           TriggerCondition == MoveTrigger.OnKnockdownAttempt;
                
                // For non-defensive techniques, check if pawn is busy/cooling down
                if (!isDefensiveTechnique)
                {
                    var curStance = pawn.stances?.curStance;
                    if (curStance is Stance_Busy || curStance is Stance_Cooldown)
                    {
                        // Log.Message($"[Yakuza Combat] {TechniqueName}: pawn busy or cooling down (non-defensive technique)");
                        return false;
                    }
                }
                else
                {
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: defensive technique, bypassing busy/cooldown check");
                }
                
                bool weaponOk = IsWeaponCompatible(pawn);
                bool conditionOk = IsConditionMet(pawn);
                // Log.Message($"[Yakuza Combat] {TechniqueName}: weapon={weaponOk}, condition={conditionOk}");
                
                return weaponOk && conditionOk;
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
            string weaponName = weapon?.def?.defName ?? "none";
            // Log.Message($"[Yakuza Combat] {TechniqueName}: checking weapon '{weaponName}' for requirement {RequiredWeapon}");
            
            switch (RequiredWeapon)
            {
                case YakuzaWeaponType.Unarmed:
                    // Consider "unarmed" if no equipped weapon OR only has natural body parts (claws, fangs, etc.)
                    bool isUnarmed = weapon == null || IsNaturalWeapon(weapon);
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: unarmed check = {isUnarmed} (weapon null: {weapon == null}, natural: {IsNaturalWeapon(weapon)})");
                    return isUnarmed;
                
                case YakuzaWeaponType.Katana:
                    bool isKatana = weapon != null && !IsNaturalWeapon(weapon) && 
                                   (weapon.def.defName.ToLower().Contains("katana") ||
                                    weapon.def.defName.ToLower().Contains("longsword") ||
                                    weapon.def.defName.ToLower().Contains("sword") ||
                                    weapon.def.defName.ToLower().Contains("blade") ||
                                    weapon.def.defName.ToLower().Contains("scimitar") ||
                                    weapon.def.defName.ToLower().Contains("saber") ||
                                    weapon.def.defName.ToLower().Contains("rapier") ||
                                    // Check weapon stats - katanas typically have good cut damage
                                    (weapon.def.IsMeleeWeapon && 
                                     weapon.def.tools?.Any(tool => tool.capacities?.Any(cap => cap.defName == "Cut") == true) == true &&
                                     weapon.def.BaseMass >= 1.0f && weapon.def.BaseMass <= 3.0f)); // Sword-like weight range
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: katana check = {isKatana}");
                    return isKatana;
                
                case YakuzaWeaponType.Knife:
                    bool isKnife = weapon != null && !IsNaturalWeapon(weapon) &&
                                  (weapon.def.defName.ToLower().Contains("knife") ||
                                   weapon.def.defName.ToLower().Contains("dagger") ||
                                   weapon.def.defName.ToLower().Contains("shiv") ||
                                   weapon.def.defName.ToLower().Contains("stiletto") ||
                                   weapon.def.defName.ToLower().Contains("kunai") ||
                                   weapon.def.defName.ToLower().Contains("tanto") ||
                                   weapon.def.defName.ToLower().Contains("dirk") ||
                                   // Small cutting weapons with specific characteristics
                                   (weapon.def.IsMeleeWeapon && weapon.def.BaseMass < 1.5f &&
                                    weapon.def.tools?.Any(tool => tool.capacities?.Any(cap => cap.defName == "Cut" || cap.defName == "Stab") == true) == true));
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: knife check = {isKnife}");
                    return isKnife;
                
                case YakuzaWeaponType.Club:
                    bool isClub = weapon != null && !IsNaturalWeapon(weapon) &&
                                 weapon.def.IsMeleeWeapon && 
                                 (weapon.def.defName.ToLower().Contains("mace") || 
                                  weapon.def.defName.ToLower().Contains("club") ||
                                  weapon.def.defName.ToLower().Contains("hammer") ||
                                  weapon.def.defName.ToLower().Contains("bat") ||
                                  weapon.def.defName.ToLower().Contains("staff") ||
                                  weapon.def.defName.ToLower().Contains("pole") ||
                                  weapon.def.defName.ToLower().Contains("rod") ||
                                  weapon.def.defName.ToLower().Contains("baton") ||
                                  weapon.def.defName.ToLower().Contains("cudgel") ||
                                  // Blunt weapons by damage type and characteristics
                                  (weapon.def.tools?.Any(tool => tool.capacities?.Any(cap => cap.defName == "Blunt") == true) == true &&
                                   weapon.def.BaseMass >= 0.5f)); // Exclude very light items
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: club check = {isClub}");
                    return isClub;
                
                case YakuzaWeaponType.Gun:
                    bool isGun = weapon != null && !IsNaturalWeapon(weapon) && weapon.def.IsRangedWeapon;
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: gun check = {isGun}");
                    return isGun;
                
                case YakuzaWeaponType.Any:
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: any weapon = true");
                    return true;
                
                default:
                    // Log.Message($"[Yakuza Combat] {TechniqueName}: unknown weapon type");
                    return false;
            }
        }
        
        /// <summary>
        /// Check if a weapon is a natural body part (claws, fangs, etc.) from modded races
        /// </summary>
        private bool IsNaturalWeapon(ThingWithComps weapon)
        {
            if (weapon == null) return false;
            
            string defName = weapon.def.defName.ToLower();
            
            // Common natural weapon indicators
            return defName.Contains("claw") ||
                   defName.Contains("fang") ||
                   defName.Contains("tooth") ||
                   defName.Contains("teeth") ||
                   defName.Contains("hoof") ||
                   defName.Contains("horn") ||
                   defName.Contains("talon") ||
                   defName.Contains("beak") ||
                   defName.Contains("tail") ||
                   defName.Contains("natural") ||
                   defName.Contains("body") ||
                   defName.Contains("limb") ||
                   // Check if it's categorized as a body part
                   weapon.def.weaponTags?.Any(tag => tag.Contains("Body") || tag.Contains("Natural")) == true;
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
        /// Validate that pawns are valid for technique execution
        /// </summary>
        protected bool ValidatePawns(Pawn user, Pawn target)
        {
            if (target == null || target.Dead || target.Downed || target.health == null)
                return false;
            if (user == null || user.Dead || user.Map == null)
                return false;
            return true;
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
            if (!YakuzaCombatMod.settings.enableMod) 
            {
                // Log.Message("[Yakuza Combat] Mod disabled, no techniques");
                return false;
            }
            if (defender?.Dead != false || attacker?.Dead != false) return false;
            
            var availableTechniques = GetAvailableTechniques(defender, MoveTrigger.OnMeleeAttackReceived);
            // Log.Message($"[Yakuza Combat] Found {availableTechniques.Count} available techniques for {defender.LabelShort}");
            
            foreach (var technique in availableTechniques)
            {
                // Check for mod compatibility
                if (CompatibilityPatches.ShouldSkipTechnique(technique.TechniqueName))
                {
                    // Log.Message($"[Yakuza Combat] Skipping {technique.TechniqueName} due to mod compatibility");
                    continue;
                }
                
                float chance = technique.CalculateTriggerChance(defender);
                // Log.Message($"[Yakuza Combat] Checking {technique.TechniqueName}: {chance:P1} chance");
                if (Rand.Chance(chance))
                {
                    // Log.Message($"[Yakuza Combat] {technique.TechniqueName} triggered!");
                    bool success = technique.ExecuteTechnique(defender, attacker, damageInfo);
                    
                    // Show floating text when technique succeeds
                    if (success && YakuzaCombatMod.settings.enableMoveText && defender?.Map != null)
                    {
                        string translationKey = $"Yakuza_{technique.TechniqueName.Replace(" ", "")}";
                        string text = translationKey.Translate();
                        // Use technique-specific color or default yellow
                        Color textColor = GetTechniqueColor(technique.TechniqueName);
                        MoteMaker.ThrowText(defender.DrawPos + new Vector3(0f, 0f, 0.5f), defender.Map, text, textColor, 3.5f);
                    }
                    
                    return success;
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
                    bool success = technique.ExecuteTechnique(defender, attacker, damageInfo);
                    
                    // Show floating text when technique succeeds
                    if (success && YakuzaCombatMod.settings.enableMoveText && defender?.Map != null)
                    {
                        string translationKey = $"Yakuza_{technique.TechniqueName.Replace(" ", "")}";
                        string text = translationKey.Translate();
                        Color textColor = GetTechniqueColor(technique.TechniqueName);
                        MoteMaker.ThrowText(defender.DrawPos + new Vector3(0f, 0f, 0.5f), defender.Map, text, textColor, 3.5f);
                    }
                    
                    return success;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get technique-specific color for floating text
        /// </summary>
        private static Color GetTechniqueColor(string techniqueName)
        {
            // Defensive/counter techniques - red/orange
            if (techniqueName.Contains("Tiger Drop") || techniqueName.Contains("Parry"))
                return new Color(1f, 0.3f, 0.1f); // Bright orange-red
            
            // Dodge/reflex techniques - cyan/blue
            if (techniqueName.Contains("Dodge") || techniqueName.Contains("Reflex") || techniqueName.Contains("Breakfall"))
                return new Color(0.2f, 0.8f, 1f); // Bright cyan
            
            // Aggressive/damage techniques - yellow
            if (techniqueName.Contains("Lunge") || techniqueName.Contains("Crush") || techniqueName.Contains("Knockback"))
                return new Color(1f, 0.9f, 0.2f); // Bright yellow
            
            // AoE/spin techniques - purple
            if (techniqueName.Contains("Spin") || techniqueName.Contains("Heat"))
                return new Color(0.9f, 0.3f, 1f); // Bright purple
            
            // Firearm counter - white
            if (techniqueName.Contains("Firearm") || techniqueName.Contains("Counter"))
                return new Color(0.95f, 0.95f, 0.95f); // Bright white
            
            // Default - bright yellow-orange
            return new Color(1f, 0.85f, 0.3f);
        }
        
        /// <summary>
        /// Get available techniques for a pawn based on trigger condition
        /// </summary>
        public static List<YakuzaTechnique> GetAvailableTechniques(Pawn pawn, MoveTrigger trigger)
        {
            var available = new List<YakuzaTechnique>();
            foreach (var technique in allTechniques)
            {
                // Log.Message($"[Yakuza Combat] Checking {technique.TechniqueName} for {pawn.LabelShort}: trigger={technique.TriggerCondition}, can use={technique.CanUseTechnique(pawn)}");
                if (technique.TriggerCondition == trigger && technique.CanUseTechnique(pawn))
                {
                    available.Add(technique);
                }
            }
            return available;
        }
    }
}