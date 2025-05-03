using System;
using UnityEngine;

public enum StatType
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
public struct ModifiableStat
{
    public float baseValue;
    public float add;
    public float multiply;

    public float Calculate()
    {
        return (baseValue + add) * multiply;
    }

    public void Modify(in ModifyStat modifyStat)
    {
        add += modifyStat.add;
        multiply += modifyStat.multiply;
    }

    public void Revert(in ModifyStat modifyStat)
    {
        add -= modifyStat.add;
        multiply -= modifyStat.multiply;
    }
}

[Serializable]
public struct ModifyStat
{
    public StatType type;
    public float add;
    public float multiply;
}

[Serializable]
public struct UpgradeSystem
{
    // Should probably be a limit on the actual reload speed not the modifier
    public float reloadSpeedModifierLimit;

    public void ApplyUpgrade(MainScript mainScript, ref UnitEntity unit, in ModifyStat modifyStat)
    {
        switch (modifyStat.type)
        {
            case StatType.Health:
                unit.statModifiers.healthModifier += valueChange;
                int newMaxHealth = Mathf.FloorToInt(unit.health.baseHealth * unit.statModifiers.healthModifier);
                int healthAdded = newMaxHealth - unit.health.max;
                unit.health.max = newMaxHealth;
                unit.health.current += healthAdded;
                mainScript.healthBar.UpdateHealthBar(mainScript.player.health);
                break;
            case StatType.MoveSpeed:
                unit.statModifiers.moveSpeedModifier += valueChange;
                unit.moveSpeed = unit.baseMoveSpeed * unit.statModifiers.moveSpeedModifier;
                break;
            case StatType.Damage:
                unit.statModifiers.damageModifier += valueChange;
                break;
            case StatType.ReloadSpeed:
                unit.statModifiers.reloadSpeedModifier += valueChange;
                if(unit.statModifiers.reloadSpeedModifier < reloadSpeedModifierLimit)
                {
                    unit.statModifiers.reloadSpeedModifier = reloadSpeedModifierLimit;
                }
                break;
            case StatType.VisionRange:
                unit.statModifiers.visionRangeModifier += valueChange;
                mainScript.playerLight.light.pointLightOuterRadius = mainScript.playerLight.baseLightRange * unit.statModifiers.visionRangeModifier;
                break;
            case StatType.FireRate:
                unit.statModifiers.fireRateModifier += valueChange;
                break;
            case StatType.Piercing:
                unit.statModifiers.piercingNumber += Mathf.FloorToInt(valueChange);
                break;
            case StatType.ExtraProjectile:
                unit.statModifiers.extraProjectiles += Mathf.FloorToInt(valueChange);
                break;
            case StatType.HealthRegenPerMinute:
                unit.statModifiers.healthRegenPerMinute += Mathf.FloorToInt(valueChange);
                break;
        }

        mainScript.shakeSystem.Shake(1.0f);
    }
}