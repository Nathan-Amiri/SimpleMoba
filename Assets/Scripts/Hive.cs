using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : Player
{
    public float force;
    public float duration;

    protected override int SetMaxHealth()
    {
        return 100;
    }
    protected override List<int> SetAbilityCooldowns()
    {
        return new List<int>()
        {
            3, // Ability 1
            1, // Ability 2
            1, // Ability 3
            1 // Ultimate Ability
        };
    }

    protected override void UseAbility1()
    {
        ApplyKnockBack(duration, rb.velocity.normalized * force);

        StartCoroutine(StartAbilityCooldown(0));
    }
    protected override void UseAbility2()
    {
        HealthChange(-10);

        StartCoroutine(StartAbilityCooldown(1));
    }
    protected override void UseAbility3()
    {

        StartCoroutine(StartAbilityCooldown(2));
    }
    protected override void UseUltimate()
    {
        StartCoroutine(StartAbilityCooldown(3));
    }
}