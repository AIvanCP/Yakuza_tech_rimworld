using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using System.Linq;
using System;

namespace YakuzaCombatMoves
{
    /// <summary>
    /// Tiger Drop - Unarmed counter with 2x damage and stun
    /// </summary>
    public class TigerDropTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Tiger Drop";
        public override string Description => "Perfect unarmed counter with devastating damage";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Unarmed;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnMeleeAttackReceived;
        public override float BaseChance => 0.05f; // 5%
        public override float SkillScaling => 0.005f; // +0.5% per level
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableTigerDrop;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user);
            
            // Calculate scaled damage with max 50 cap
            float counterDamage = CalculateScaledDamage(user, 20f, 50f);
            counterDamage = PreventInstantKill(target, counterDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaTigerDrop") ?? DamageDefOf.Blunt,
                counterDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
            );
            
            target.TakeDamage(damageInfo);
            
            // Always apply stun effect
            ApplyStunEffect(target, 90);
            
            // 30% chance for additional random debuff
            float debuffChance = CalculateDebuffChance(user, 0.3f);
            if (Rand.Chance(debuffChance))
            {
                ApplyRandomDebuff(target);
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(target.DrawPos, target.Map, 2f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            // Play custom sound if available
            PlayTechniqueSound("TigerDrop", user);
            
            return true; // Negates original attack
        }
        
        private void ApplyRandomDebuff(Pawn target)
        {
            try
            {
                var debuffOptions = new string[] { "YakuzaSlow", "YakuzaWeakened", "YakuzaArmorShred" };
                string selectedDebuff = debuffOptions[Rand.Range(0, debuffOptions.Length)];
                
                var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(selectedDebuff);
                if (hediffDef != null)
                {
                    var hediff = HediffMaker.MakeHediff(hediffDef, target);
                    hediff.Severity = 1.0f;
                    target.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying random debuff: {e.Message}");
            }
        }
        
        private void PlayTechniqueSound(string techniqueName, Pawn user)
        {
            try
            {
                // Try to play default technique sound
                var defaultSound = DefDatabase<SoundDef>.GetNamedSilentFail($"Yakuza{techniqueName}");
                if (defaultSound != null && user.Map != null)
                {
                    SoundStarter.PlayOneShot(defaultSound, new TargetInfo(user.Position, user.Map));
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Yakuza Combat] Could not play sound for {techniqueName}: {e.Message}");
            }
        }
        
        private void ApplyStunEffect(Pawn target, int ticks)
        {
            try
            {
                var stunHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaStunned") ?? HediffDefOf.Anesthetic;
                var hediff = HediffMaker.MakeHediff(stunHediff, target);
                hediff.Severity = 0.8f;
                target.health.AddHediff(hediff);
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying stun: {e.Message}");
                target.stances.stunner.StunFor(ticks, null);
            }
        }
    }
    
    /// <summary>
    /// Komaki Parry - Katana technique that negates damage and slashes back
    /// </summary>
    public class KomakiParryTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Komaki Parry";
        public override string Description => "Katana technique that deflects attacks and counters";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Katana;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnMeleeAttackReceived;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableKomakiParry;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} parries and counters!");
            
            // Calculate scaled damage with max 50 cap
            float slashDamage = CalculateScaledDamage(user, 15f, 50f);
            slashDamage = PreventInstantKill(target, slashDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaKomakiParry") ?? DamageDefOf.Cut,
                slashDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Cut)
            );
            
            target.TakeDamage(damageInfo);
            
            // Visual effects
            FleckMaker.ThrowMetaIcon(user.Position, user.Map, FleckDefOf.IncapIcon);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            // Play sound
            PlayTechniqueSound("KomakiParry", user);
            
            return true; // Negates original damage
        }
        
        private void PlayTechniqueSound(string techniqueName, Pawn user)
        {
            try
            {
                var defaultSound = DefDatabase<SoundDef>.GetNamedSilentFail($"Yakuza{techniqueName}");
                if (defaultSound != null && user.Map != null)
                {
                    SoundStarter.PlayOneShot(defaultSound, new TargetInfo(user.Position, user.Map));
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Yakuza Combat] Could not play sound for {techniqueName}: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Mad Dog Dodge Slash - Knife technique with dodge and bleed counter
    /// </summary>
    public class MadDogDodgeSlashTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Mad Dog Dodge Slash";
        public override string Description => "Dodge and counter with bleeding wound";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Knife;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnMeleeAttackReceived;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableMadDogDodgeSlash;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} dodges and slashes!");
            
            // Calculate scaled damage with max 45 cap
            float slashDamage = CalculateScaledDamage(user, 12f, 45f);
            slashDamage = PreventInstantKill(target, slashDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaMadDogSlash") ?? DamageDefOf.Cut,
                slashDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Cut)
            );
            
            target.TakeDamage(damageInfo);
            
            // Enhanced bleeding chance based on skill
            float bleedChance = CalculateDebuffChance(user, 0.2f);
            if (Rand.Chance(bleedChance))
            {
                ApplyBleedingEffect(target);
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 1.5f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            return true; // Negates original damage
        }
        
        private void ApplyBleedingEffect(Pawn target)
        {
            try
            {
                var bleedingHediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, target);
                bleedingHediff.Severity = 0.2f;
                target.health.AddHediff(bleedingHediff);
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying bleeding: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Komaki Knockback - Club technique with stagger
    /// </summary>
    public class KomakiKnockbackTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Komaki Knockback";
        public override string Description => "Blunt weapon counter with devastating knockback";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Club;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnMeleeAttackReceived;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableKomakiKnockback;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} delivers a crushing counter!");
            
            // Calculate scaled blunt damage with max 50 cap
            float bluntDamage = CalculateScaledDamage(user, 18f, 50f);
            bluntDamage = PreventInstantKill(target, bluntDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaKomakiKnockback") ?? DamageDefOf.Blunt,
                bluntDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
            );
            
            target.TakeDamage(damageInfo);
            
            // Apply stagger effect with enhanced chance
            float staggerChance = CalculateDebuffChance(user, 0.4f);
            if (Rand.Chance(staggerChance))
            {
                ApplyStaggerEffect(target);
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(target.DrawPos, target.Map, 2.5f);
            
            return true;
        }
        
        private void ApplyStaggerEffect(Pawn target)
        {
            try
            {
                var staggerHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaDisoriented");
                if (staggerHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(staggerHediff, target);
                    hediff.Severity = 0.6f;
                    target.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying stagger: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Majima Heat Spin - AoE attack when surrounded
    /// </summary>
    public class MajimaHeatSpinTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Majima Heat Spin";
        public override string Description => "Devastating spin attack when outnumbered";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Any;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnSurrounded;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableMajimaHeatSpin;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} unleashes a heat spin attack!");
            
            int enemiesHit = 0;
            
            // Hit all adjacent enemies with scaled damage
            foreach (var cell in GenAdj.CellsAdjacent8Way(user))
            {
                if (!cell.InBounds(user.Map)) continue;
                
                var things = cell.GetThingList(user.Map);
                foreach (var thing in things)
                {
                    if (thing is Pawn enemy && enemy.HostileTo(user) && !enemy.Dead)
                    {
                        // Scaled damage based on user skill, max 40 per enemy
                        float spinDamage = CalculateScaledDamage(user, 8f, 40f);
                        spinDamage = PreventInstantKill(enemy, spinDamage);
                        
                        var damageInfo = new DamageInfo(
                            DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaHeatSpin") ?? DamageDefOf.Blunt,
                            spinDamage,
                            0f,
                            -1f,
                            user,
                            enemy.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
                        );
                        
                        enemy.TakeDamage(damageInfo);
                        FleckMaker.ThrowMicroSparks(enemy.DrawPos, enemy.Map);
                        enemiesHit++;
                        
                        // Chance to apply slow debuff to each enemy
                        float debuffChance = CalculateDebuffChance(user, 0.25f);
                        if (Rand.Chance(debuffChance))
                        {
                            ApplySlowDebuff(enemy);
                        }
                    }
                }
            }
            
            // Visual effect scales with enemies hit
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 2f + enemiesHit * 0.5f);
            
            // Play enhanced sound
            PlayTechniqueSound("HeatSpin", user);
            
            return false; // Doesn't negate original damage - it's an additional AoE
        }
        
        private void ApplySlowDebuff(Pawn target)
        {
            try
            {
                var slowHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaSlow");
                if (slowHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(slowHediff, target);
                    hediff.Severity = 1.0f;
                    target.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying slow debuff: {e.Message}");
            }
        }
        
        private void PlayTechniqueSound(string techniqueName, Pawn user)
        {
            try
            {
                var defaultSound = DefDatabase<SoundDef>.GetNamedSilentFail($"Yakuza{techniqueName}");
                if (defaultSound != null && user.Map != null)
                {
                    SoundStarter.PlayOneShot(defaultSound, new TargetInfo(user.Position, user.Map));
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Yakuza Combat] Could not play sound for {techniqueName}: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Komaki Breakfall - Recover from knockdowns
    /// </summary>
    public class KomakiBreakfallTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Komaki Breakfall";
        public override string Description => "Instantly recover from knockdown attempts with enhanced reflexes";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Any;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnKnockdownAttempt;
        public override float BaseChance => 0.20f; // Will be overridden by unified scaling
        public override float SkillScaling => 0.001f; // Will be overridden by unified scaling
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} recovers with enhanced reflexes!");
            
            // Force pawn to stand up if downed
            if (user.Downed)
            {
                user.health.Reset();
            }
            
            // Apply enhanced reflexes buff (30% dodge for 2 seconds)
            ApplyEnhancedReflexes(user);
            
            // Visual effect
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 1.5f);
            
            return true; // Prevents knockdown
        }
        
        private void ApplyEnhancedReflexes(Pawn user)
        {
            try
            {
                var reflexHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaEnhancedReflexes");
                if (reflexHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(reflexHediff, user);
                    hediff.Severity = 1.0f;
                    user.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying enhanced reflexes: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Cat-Like Reflexes - Dodge ranged attacks
    /// </summary>
    public class CatLikeReflexesTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Cat-Like Reflexes";
        public override string Description => "Supernatural dodge ability against ranged attacks";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Unarmed;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnRangedAttackReceived;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableCatLikeReflexes;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} dodges with cat-like reflexes!");
            
            // Apply brief enhanced reflexes after successful dodge
            ApplyEnhancedReflexes(user);
            
            // Visual effect
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 1f);
            
            return true; // Negates ranged damage
        }
        
        private void ApplyEnhancedReflexes(Pawn user)
        {
            try
            {
                var reflexHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaEnhancedReflexes");
                if (reflexHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(reflexHediff, user);
                    hediff.Severity = 0.5f; // Lower than Breakfall
                    user.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying enhanced reflexes: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Wall Crush - Enhanced stun when near walls
    /// </summary>
    public class WallCrushTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Wall Crush";
        public override string Description => "Devastating slam against walls";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Club;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnNearWall;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableWallCrush;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} crushes {target.LabelShort} against the wall!");
            
            // Enhanced damage calculation: base damage + wall bonus, capped at 60
            float baseDamage = CalculateScaledDamage(user, 20f, 50f);
            float wallDamage = baseDamage * 1.5f; // 50% bonus near wall
            wallDamage = Mathf.Min(wallDamage, 60f); // Higher cap due to environmental bonus
            wallDamage = PreventInstantKill(target, wallDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaWallCrush") ?? DamageDefOf.Blunt,
                wallDamage,
                originalDamage.ArmorPenetrationInt,
                originalDamage.Angle,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
            );
            
            target.TakeDamage(damageInfo);
            
            // Extended stun and chance for armor shred
            ApplyStunEffect(target, 150); // 2.5 seconds
            
            float armorShredChance = CalculateDebuffChance(user, 0.35f);
            if (Rand.Chance(armorShredChance))
            {
                ApplyArmorShred(target);
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(target.DrawPos, target.Map, 3f);
            
            return true; // Replaces original damage with enhanced wall crush
        }
        
        private void ApplyStunEffect(Pawn target, int ticks)
        {
            try
            {
                var stunHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaStunned");
                if (stunHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(stunHediff, target);
                    hediff.Severity = 0.9f;
                    target.health.AddHediff(hediff);
                }
                else
                {
                    target.stances.stunner.StunFor(ticks, null);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying wall crush stun: {e.Message}");
            }
        }
        
        private void ApplyArmorShred(Pawn target)
        {
            try
            {
                var armorShredHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaArmorShred");
                if (armorShredHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(armorShredHediff, target);
                    hediff.Severity = 1.0f;
                    target.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying armor shred: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Mad Dog Lunge - Dash attack with knives
    /// </summary>
    public class MadDogLungeTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Mad Dog Lunge";
        public override string Description => "Lightning-fast dash attack";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Knife;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnMeleeAttackReceived;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableMadDogLunge;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} lunges forward!");
            
            // Calculate scaled dash damage with max 40 cap
            float lungeDamage = CalculateScaledDamage(user, 10f, 40f);
            lungeDamage = PreventInstantKill(target, lungeDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaMadDogLunge") ?? DamageDefOf.Cut,
                lungeDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Cut)
            );
            
            target.TakeDamage(damageInfo);
            
            // Enhanced bleed chance based on skill
            float bleedChance = CalculateDebuffChance(user, 0.15f);
            if (Rand.Chance(bleedChance))
            {
                ApplyBleedingEffect(target);
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 2f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            return false; // Additional damage, doesn't negate original
        }
        
        private void ApplyBleedingEffect(Pawn target)
        {
            try
            {
                var bleedingHediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, target);
                bleedingHediff.Severity = 0.15f;
                target.health.AddHediff(bleedingHediff);
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying lunge bleeding: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Firearm Counter - Point-blank shot when attacked while holding gun
    /// </summary>
    public class FirearmCounterTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Firearm Counter";
        public override string Description => "Point-blank counter shot";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Gun;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnMeleeAttackReceived;
        public override float BaseChance => 0.05f; // Uses unified scaling
        public override float SkillScaling => 0.01f; // Uses unified scaling
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableFirearmCounter;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} fires point-blank!");
            
            // Calculate scaled shot damage with max 35 cap
            float shotDamage = CalculateScaledDamage(user, 6f, 35f);
            shotDamage = PreventInstantKill(target, shotDamage);
            
            var damageInfo = new DamageInfo(
                DefDatabase<DamageDef>.GetNamedSilentFail("YakuzaFirearmCounter") ?? DamageDefOf.Bullet,
                shotDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Bullet)
            );
            
            target.TakeDamage(damageInfo);
            
            // Chance to apply weakened debuff from close-range shot
            float weakenChance = CalculateDebuffChance(user, 0.2f);
            if (Rand.Chance(weakenChance))
            {
                ApplyWeakenedDebuff(target);
            }
            
            // Visual effects
            FleckMaker.ThrowMicroSparks(user.DrawPos, user.Map);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            // Play sound
            PlayTechniqueSound("FirearmCounter", user);
            
            return false; // Doesn't negate original attack - additional shot
        }
        
        private void ApplyWeakenedDebuff(Pawn target)
        {
            try
            {
                var weakenedHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaWeakened");
                if (weakenedHediff != null)
                {
                    var hediff = HediffMaker.MakeHediff(weakenedHediff, target);
                    hediff.Severity = 1.0f;
                    target.health.AddHediff(hediff);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying weakened debuff: {e.Message}");
            }
        }
        
        private void PlayTechniqueSound(string techniqueName, Pawn user)
        {
            try
            {
                var defaultSound = DefDatabase<SoundDef>.GetNamedSilentFail($"Yakuza{techniqueName}");
                if (defaultSound != null && user.Map != null)
                {
                    SoundStarter.PlayOneShot(defaultSound, new TargetInfo(user.Position, user.Map));
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Yakuza Combat] Could not play sound for {techniqueName}: {e.Message}");
            }
        }
    }
}