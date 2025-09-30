using RimWorld;
using Verse;
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
            
            // Counter with 2x melee damage
            float counterDamage = user.GetStatValue(StatDefOf.MeleeDamageFactor) * 20f; // Base unarmed damage
            counterDamage = PreventInstantKill(target, counterDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Blunt,
                counterDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
            );
            
            target.TakeDamage(damageInfo);
            
            // Apply stun effect
            ApplyStunEffect(target, 90); // 1.5 seconds
            
            // Visual effects
            FleckMaker.ThrowDustPuff(target.DrawPos, target.Map, 2f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            return true; // Negates original attack
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
        public override float BaseChance => 0.10f; // 10%
        public override float SkillScaling => 0.003f; // +0.3% per level
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableKomakiParry;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} parries and counters!");
            
            // Counter slash
            float slashDamage = Rand.Range(20f, 30f);
            slashDamage = PreventInstantKill(target, slashDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Cut,
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
            
            return true; // Negates original damage
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
        public override float BaseChance => 0.08f; // 8%
        public override float SkillScaling => 0.002f; // +0.2% per level
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableMadDogDodgeSlash;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} dodges and slashes!");
            
            // Counter with bleeding
            float slashDamage = Rand.Range(15f, 25f);
            slashDamage = PreventInstantKill(target, slashDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Cut,
                slashDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Cut)
            );
            
            target.TakeDamage(damageInfo);
            
            // Apply bleeding (20% chance)
            if (Rand.Chance(0.2f))
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
        public override float BaseChance => 0.07f; // 7%
        public override float SkillScaling => 0.003f; // +0.3% per level
        
        public override bool CanUseTechnique(Pawn pawn)
        {
            return base.CanUseTechnique(pawn) && YakuzaCombatMod.settings.enableKomakiKnockback;
        }
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} delivers a crushing counter!");
            
            // Blunt counter damage
            float bluntDamage = Rand.Range(18f, 28f);
            bluntDamage = PreventInstantKill(target, bluntDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Blunt,
                bluntDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
            );
            
            target.TakeDamage(damageInfo);
            
            // Apply stagger effect
            ApplyStaggerEffect(target);
            
            // Visual effects
            FleckMaker.ThrowDustPuff(target.DrawPos, target.Map, 2.5f);
            
            return true;
        }
        
        private void ApplyStaggerEffect(Pawn target)
        {
            try
            {
                var staggerHediff = DefDatabase<HediffDef>.GetNamedSilentFail("YakuzaDisoriented") ?? HediffDefOf.PsychicShock;
                var hediff = HediffMaker.MakeHediff(staggerHediff, target);
                hediff.Severity = 0.6f;
                target.health.AddHediff(hediff);
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
        public override float BaseChance => 0.10f; // 10%
        public override float SkillScaling => 0.002f; // +0.2% per level
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} unleashes a heat spin attack!");
            
            // Hit all adjacent enemies
            foreach (var cell in GenAdj.CellsAdjacent8Way(user))
            {
                if (!cell.InBounds(user.Map)) continue;
                
                var things = cell.GetThingList(user.Map);
                foreach (var thing in things)
                {
                    if (thing is Pawn enemy && enemy.HostileTo(user) && !enemy.Dead)
                    {
                        float spinDamage = Rand.Range(10f, 15f);
                        spinDamage = PreventInstantKill(enemy, spinDamage);
                        
                        var damageInfo = new DamageInfo(
                            DamageDefOf.Blunt,
                            spinDamage,
                            0f,
                            -1f,
                            user,
                            enemy.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
                        );
                        
                        enemy.TakeDamage(damageInfo);
                        FleckMaker.ThrowMicroSparks(enemy.DrawPos, enemy.Map);
                    }
                }
            }
            
            // Visual effect
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 3f);
            
            return false; // Doesn't negate original damage
        }
    }
    
    /// <summary>
    /// Komaki Breakfall - Recover from knockdowns
    /// </summary>
    public class KomakiBreakfallTechnique : YakuzaTechnique
    {
        public override string TechniqueName => "Komaki Breakfall";
        public override string Description => "Instantly recover from knockdown attempts";
        public override YakuzaWeaponType RequiredWeapon => YakuzaWeaponType.Any;
        public override MoveTrigger TriggerCondition => MoveTrigger.OnKnockdownAttempt;
        public override float BaseChance => 0.20f; // 20%
        public override float SkillScaling => 0.001f; // +0.1% per level
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} recovers instantly!");
            
            // Force pawn to stand up if downed
            if (user.Downed)
            {
                user.health.Reset();
            }
            
            // Visual effect
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 1.5f);
            
            return true; // Prevents knockdown
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
        public override float BaseChance => 0.03f; // 3%
        public override float SkillScaling => 0.002f; // +0.2% per level
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} dodges with cat-like reflexes!");
            
            // Visual effect
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 1f);
            
            return true; // Negates ranged damage
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
        public override float BaseChance => 0.12f; // 12%
        public override float SkillScaling => 0.003f; // +0.3% per level
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} crushes {target.LabelShort} against the wall!");
            
            // Enhanced damage near wall
            float wallDamage = originalDamage.Amount * 1.5f;
            wallDamage = PreventInstantKill(target, wallDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Blunt,
                wallDamage,
                originalDamage.ArmorPenetrationInt,
                originalDamage.Angle,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt)
            );
            
            target.TakeDamage(damageInfo);
            
            // Extended stun (2 extra seconds)
            ApplyStunEffect(target, 120);
            
            // Visual effects
            FleckMaker.ThrowDustPuff(target.DrawPos, target.Map, 3f);
            
            return false; // Replaces original damage
        }
        
        private void ApplyStunEffect(Pawn target, int ticks)
        {
            try
            {
                target.stances.stunner.StunFor(ticks, null);
            }
            catch (Exception e)
            {
                Log.Error($"[Yakuza Combat] Error applying wall crush stun: {e.Message}");
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
        public override float BaseChance => 0.06f; // 6%
        public override float SkillScaling => 0.002f; // +0.2% per level
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} lunges forward!");
            
            // Dash damage
            float lungeDamage = Rand.Range(12f, 20f);
            lungeDamage = PreventInstantKill(target, lungeDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Cut,
                lungeDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Cut)
            );
            
            target.TakeDamage(damageInfo);
            
            // 15% bleed chance
            if (Rand.Chance(0.15f))
            {
                ApplyBleedingEffect(target);
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(user.DrawPos, user.Map, 2f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            return false;
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
        public override float BaseChance => 0.08f; // 8%
        public override float SkillScaling => 0.002f; // +0.2% per level
        
        public override bool ExecuteTechnique(Pawn user, Pawn target, DamageInfo originalDamage)
        {
            ShowTechniqueEffect(user, $"{user.LabelShort} fires point-blank!");
            
            // Small guaranteed hit
            float shotDamage = Rand.Range(8f, 15f);
            shotDamage = PreventInstantKill(target, shotDamage);
            
            var damageInfo = new DamageInfo(
                DamageDefOf.Bullet,
                shotDamage,
                0f,
                -1f,
                user,
                target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Bullet)
            );
            
            target.TakeDamage(damageInfo);
            
            // Visual effects
            FleckMaker.ThrowMicroSparks(user.DrawPos, user.Map);
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
            
            return false; // Doesn't negate original attack
        }
    }
}