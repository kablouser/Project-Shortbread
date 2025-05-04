using System;
using UnityEngine;

public enum StatType
{
    Damage,
    ReloadSpeed,
    MaxHealth,
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

    public float CalculateWithBaseValue(float overrideBaseValue)
    {
        return (overrideBaseValue + add) * multiply;
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

public struct StatSheet
{
    public ModifiableStat
        // Please ignore base value. Gotten from weapon/projectile
        damage,
        reloadSpeed,
        maxHealth,
        moveSpeed,
        visionRange,
        // Please ignore base value. Gotten from weapon/projectile
        fireRate,
        // Please ignore base value. Gotten from weapon/projectile
        piercing,
        // Please ignore base value. Gotten from weapon/projectile
        extraProjectile,
        healthRegenPerMinute;
}

static class StatSheetExtension
{
    public static ref ModifiableStat Get(this ref StatSheet statSheet, StatType type)
    {
        switch (type)
        {
            case StatType.Damage:
                return ref statSheet.damage;
            case StatType.ReloadSpeed:
                return ref statSheet.reloadSpeed;
            case StatType.MaxHealth:
                return ref statSheet.maxHealth;
            case StatType.MoveSpeed:
                return ref statSheet.moveSpeed;
            case StatType.VisionRange:
                return ref statSheet.visionRange;
            case StatType.FireRate:
                return ref statSheet.fireRate;
            case StatType.Piercing:
                return ref statSheet.piercing;
            case StatType.ExtraProjectile:
                return ref statSheet.extraProjectile;
            case StatType.HealthRegenPerMinute:
                return ref statSheet.healthRegenPerMinute;
            default:
                throw new NotImplementedException();
        }
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
    public void ApplyUpgrade(MainScript mainScript, ref UnitEntity unit, in ModifyStat modifyStat)
    {
        switch (modifyStat.type)
        {
            default:
                unit.statSheet.Get(modifyStat.type).Modify(modifyStat);
                break;

            case StatType.MaxHealth:
                float oldMaxHealth = unit.statSheet.maxHealth.Calculate();
                unit.statSheet.maxHealth.Modify(modifyStat);
                float newMaxHealth = unit.statSheet.maxHealth.Calculate();

                if (oldMaxHealth < newMaxHealth)
                {
                    unit.health.current += newMaxHealth - oldMaxHealth;
                }
                mainScript.healthBar.UpdateHealthBar(mainScript.player);
                break;
        }

        // post processing
        switch (modifyStat.type)
        {
            default: break;
            case StatType.VisionRange:
                mainScript.playerLight.light.pointLightOuterRadius = mainScript.playerLight.baseLightRange * unit.statSheet.visionRange.Calculate();
                break;
        }

        mainScript.shakeSystem.Shake(1.0f);
    }
}