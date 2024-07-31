using System;
using UnityEngine;

public enum UpgradeType
{
    Damage,
    ReloadSpeed,
    Health,
    MoveSpeed,
    VisionRange,
    FireRate,
    Piercing,
    ExtraProjectile,
    HealthRegenPerMinute
};

[Serializable]
public struct StatsModifierComponent
{
    public float healthModifier;
    public float moveSpeedModifier;
    public float damageModifier;
    public float reloadSpeedModifier;
    public float visionRangeModifier;
    public float fireRateModifier;
    public int piercingNumber;
    public int extraProjectiles;
    public int healthRegenPerMinute;
}

[Serializable]
public struct UpgradeSystem
{
    // Should probably be a limit on the actual reload speed not the modifier
    public float reloadSpeedModifierLimit;

    public void ApplyUpgrade(MainScript mainScript, ref UnitEntity unit, UpgradeType type, float valueChange)
    {
        switch (type)
        {
            case UpgradeType.Health:
                unit.statModifiers.healthModifier += valueChange;
                int newMaxHealth = Mathf.FloorToInt(unit.health.baseHealth * unit.statModifiers.healthModifier);
                int healthAdded = newMaxHealth - unit.health.max;
                unit.health.max = newMaxHealth;
                unit.health.current += healthAdded;
                mainScript.healthBar.UpdateHealthBar(mainScript.player.health);
                break;
            case UpgradeType.MoveSpeed:
                unit.statModifiers.moveSpeedModifier += valueChange;
                unit.moveSpeed = unit.baseMoveSpeed * unit.statModifiers.moveSpeedModifier;
                break;
            case UpgradeType.Damage:
                unit.statModifiers.damageModifier += valueChange;
                break;
            case UpgradeType.ReloadSpeed:
                unit.statModifiers.reloadSpeedModifier += valueChange;
                if(unit.statModifiers.reloadSpeedModifier < reloadSpeedModifierLimit)
                {
                    unit.statModifiers.reloadSpeedModifier = reloadSpeedModifierLimit;
                }
                break;
            case UpgradeType.VisionRange:
                unit.statModifiers.visionRangeModifier += valueChange;
                mainScript.playerLight.light.pointLightOuterRadius = mainScript.playerLight.baseLightRange * unit.statModifiers.visionRangeModifier;
                break;
            case UpgradeType.FireRate:
                unit.statModifiers.fireRateModifier += valueChange;
                break;
            case UpgradeType.Piercing:
                unit.statModifiers.piercingNumber += Mathf.FloorToInt(valueChange);
                break;
            case UpgradeType.ExtraProjectile:
                unit.statModifiers.extraProjectiles += Mathf.FloorToInt(valueChange);
                break;
            case UpgradeType.HealthRegenPerMinute:
                unit.statModifiers.healthRegenPerMinute += Mathf.FloorToInt(valueChange);
                if(unit.statModifiers.healthRegenPerMinute == 1)
                {
                    mainScript.gameTimer.healthRegenTime = mainScript.gameTimer.currentTime + 60;
                    mainScript.playerHealthCanRegen = true;
                }
                break;
        }

        mainScript.shakeSystem.Shake(1.0f);
    }
}