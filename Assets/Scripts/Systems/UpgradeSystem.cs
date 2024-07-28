using System;
using UnityEngine;

public enum UpgradeType
{
    Damage,
    ReloadSpeed,
    Health,
    MoveSpeed,
    VisionRange,
};

[Serializable]
public struct StatsModifierComponent
{
    public float healthModifier;
    public float moveSpeedModifier;
    public float damageModifier;
    public float reloadSpeedModifier;
    public float visionRangeModifier;
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
        }

        mainScript.shakeSystem.Shake(1.0f);
    }
}