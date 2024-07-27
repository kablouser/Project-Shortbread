using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    Damage,
    ReloadSpeed,
    Health,
    MoveSpeed,
};

[Serializable]
public struct StatsModifierComponent
{
    public float healthModifier;
    public float moveSpeedModifier;
    public float damageModifier;
    public float reloadSpeedModifier;
}

[Serializable]
public struct UpgradeSystem
{
    // Should probably be a limit on the actual reload speed not the modifier
    public float reloadSpeedModifierLimit;

    public void ApplyUpgrade(ref UnitEntity unit, UpgradeType type, float valueChange)
    {
        switch (type)
        {
            case UpgradeType.Health:
                unit.statModifiers.healthModifier += valueChange;
                int newMaxHealth = Mathf.FloorToInt(unit.health.baseHealth * unit.statModifiers.healthModifier);
                int healthAdded = newMaxHealth - unit.health.max;
                unit.health.max = newMaxHealth;
                unit.health.current += healthAdded;
                Debug.Log("Health Modifier: " + unit.statModifiers.healthModifier);
                break;
            case UpgradeType.MoveSpeed:
                unit.statModifiers.moveSpeedModifier += valueChange;
                unit.moveSpeed = unit.baseMoveSpeed * unit.statModifiers.moveSpeedModifier;
                Debug.Log("Move Speed Modifier: " + unit.statModifiers.moveSpeedModifier);
                break;
            case UpgradeType.Damage:
                unit.statModifiers.damageModifier += valueChange;
                Debug.Log("Damae Modifier: " + unit.statModifiers.damageModifier);
                break;
            case UpgradeType.ReloadSpeed:
                unit.statModifiers.reloadSpeedModifier += valueChange;
                if(unit.statModifiers.reloadSpeedModifier < reloadSpeedModifierLimit)
                {
                    unit.statModifiers.reloadSpeedModifier = reloadSpeedModifierLimit;
                }
                Debug.Log("Reload Cooldown Modifier: " + unit.statModifiers.reloadSpeedModifier);
                break;
        }
    }
}