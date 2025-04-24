using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : CharacterStats, ISaveManager
{
    private Player player;

    public bool keepPlayerHealthSceneChange;

    protected override void Start()
    {
        base.Start();

        player = GetComponent<Player>();
        if (!keepPlayerHealthSceneChange)
        {
            currentHealth = GetMaxHealthValue();
            keepPlayerHealthSceneChange = false;
        }
    }

    public override void TakeDamage(int _damage)
    {
        base.TakeDamage(_damage);
    }

    protected override void Die()
    {
        base.Die();
        player.Die();

        AudioManager.instance.PlaySFX("PlayerDeath");
    }

    protected override void DecreaseHealthBy(int _damage)
    {
        base.DecreaseHealthBy(_damage);

        if (_damage > GetMaxHealthValue() * .2f)
        {   
            player.SetupKnockbackPower(new Vector2(10,6));
            player.fX.ScreenShake(player.fX.shakeHighDamage);

            string[] sound = { "PlayerHurt1", "PlayerHurt2" };
            int range = Random.Range(0, sound.Length);
            AudioManager.instance.PlaySFX(sound[range]);
        }

        ItemData_Equipment currentArmor = Inventory.instance.GetEquipment(EquipmentType.Armor);

        if (currentArmor != null)
            currentArmor.Effect(player.transform);
    }

    public override void OnEvasion()
    {
        player.skill.dodge.CreateMirageOnDodge();
    }

    public void CloneDoDamage(CharacterStats _targetStats, float _multiplier)
    {
        if (TargetCanAvoidAttack(_targetStats))
            return;

        int totalDamage = damage.GetValue() + strength.GetValue();

        if(_multiplier > 0)
            totalDamage = Mathf.RoundToInt(totalDamage * _multiplier);

        if (CanCrit())
        {
            totalDamage = CalculateCriticalDamage(totalDamage);
        }

        totalDamage = CheckTargetArmor(_targetStats, totalDamage);

        if (fireDamage.GetBaseValue() > 0 || iceDamage.GetBaseValue() > 0 || lightingDamage.GetBaseValue() > 0)
        {
            DoMagicalDamage(_targetStats);
            return;
        }

        _targetStats.TakeDamage(totalDamage);

    }

    public void LoadData(GameData _data)
    {
        maxHealth.SetDefaultValue(_data.maxHealth);
        //if (_data.keepPlayerHealthSceneChange)
        //{
        //    currentHealth = _data.currentHealth;
        //    //keepPlayerHealthSceneChange = false;
        //}else
        //{
        //    currentHealth = _data.maxHealth + _data.vitality * 5;
        //}
        currentHealth = _data.maxHealth + _data.vitality * 5;
        damage.SetDefaultValue(_data.damage);
        strength.SetDefaultValue(_data.strength);
        agility.SetDefaultValue(_data.agility);
        intelligence.SetDefaultValue(_data.intelligence);
        vitality.SetDefaultValue(_data.vitality);
        critChance.SetDefaultValue(_data.critChance);
        critPower.SetDefaultValue(_data.critPower);
        armor.SetDefaultValue(_data.armor);
        evasion.SetDefaultValue(_data.evasion);
        magicResistance.SetDefaultValue(_data.magicResistance);
        fireDamage.SetDefaultValue(_data.fireDamage);
        iceDamage.SetDefaultValue(_data.iceDamage);
        lightingDamage.SetDefaultValue(_data.lightingDamage);
        keepPlayerHealthSceneChange = _data.keepPlayerHealthSceneChange;
    }

    public void SaveData(ref GameData _data)
    {
        _data.maxHealth = maxHealth.GetBaseValue();
        _data.damage = damage.GetBaseValue();
        _data.strength = strength.GetBaseValue();
        _data.agility = agility.GetBaseValue();
        _data.intelligence = intelligence.GetBaseValue();
        _data.vitality = vitality.GetBaseValue();
        _data.critChance = critChance.GetBaseValue();
        _data.critPower = critPower.GetBaseValue();
        _data.armor = armor.GetBaseValue();
        _data.evasion = evasion.GetBaseValue();
        _data.magicResistance = magicResistance.GetBaseValue();
        _data.fireDamage = fireDamage.GetBaseValue();
        _data.iceDamage = iceDamage.GetBaseValue();
        _data.lightingDamage = lightingDamage.GetBaseValue();

        _data.currentHealth = currentHealth;
        _data.keepPlayerHealthSceneChange = keepPlayerHealthSceneChange;
    }
}
