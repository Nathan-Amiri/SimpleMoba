using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hive : Player
{
    // PREFAB REFERENCE:
    [SerializeField] private HiveRift riftPref;

    // CONSTANT:
    private readonly float ability1Cooldown = 2.5f;
    private readonly int ability1MaxCharges = 5;
    private readonly float ability2Cooldown = 3;
    private readonly int ability2MaxCharges = 5;
    private readonly float ability3Cooldown = .3f;
    private readonly float ultimateCooldown = 30;

    private readonly float slowAmountPerRift = .06f;
    private readonly float dashDuration = .4f;
    private readonly float explosionDelay = .3f;
    private readonly float explosionDuration = .4f;
    private readonly float knockbackForce = 6;
    private readonly float knockbackDuration = .4f;
    private readonly int riftDamage = 10;
    private readonly float ultimateDuration = 4;
    private readonly float ultimateRiftSpawnDelay = .4f;
    private readonly float ultimateRiftSpawnRange = 3;

    private readonly List<HiveRift> activeRifts = new();

    // DYNAMIC:
    private float riftLength;
    private float ability2Range;

    private bool inUltimateMode;

    protected override void Awake()
    {
        base.Awake();

        riftLength = riftPref.transform.localScale.y;
        ability2Range = (transform.localScale.x / 2) + (riftPref.transform.GetChild(0).lossyScale.x / 2);
    }

    protected override int SetMaxHealth()
    {
        return 100;
    }

    protected override List<int> SetAbilityMaxCharges()
    {
        return new() { ability1MaxCharges, ability2MaxCharges, 1, 1 };
    }

    protected override IEnumerator UseAbility1()
    {
        if (activeRifts.Count > 9)
        {
            Debug.Log("Too many active rifts!");
            yield break;
        }

        EndUltimate();

        Vector2 spawnDirection = (mousePos - (Vector2)transform.position).normalized;
        Vector2 spawnPosition = (Vector2)transform.position + (riftLength / 2 * spawnDirection);

        SpawnRift(spawnPosition, spawnDirection);

        StartAbilityCooldown(0, ability1Cooldown);

        yield break;
    }
    private void SpawnRift(Vector2 spawnPosition, Vector2 spawnDirection)
    {
        HiveRift rift = Instantiate(riftPref, spawnPosition, Quaternion.identity, sceneReference.petParent);
        rift.transform.up = spawnDirection;

        rift.hive = this;

        activeRifts.Add(rift);

        ChangeMoveSpeed(-slowAmountPerRift, false, true);
    }

    protected override IEnumerator UseAbility2()
    {
        // Save end points in range and the directions of their rifts
        Dictionary<Transform, Vector2> endPointsInRange = new();

        foreach (HiveRift rift in activeRifts)
            for (int i = 0; i < 2; i++)
            {
                Transform endPointTransform = rift.endPoints[i];
                if (Vector2.Distance(endPointTransform.position, transform.position) <= ability2Range)
                {
                    Vector2 dashDirection = (rift.transform.position - endPointTransform.position).normalized;
                    endPointsInRange.Add(endPointTransform, dashDirection);
                }
            }

        // If no end points in range, return
        if (endPointsInRange.Count == 0)
        {
            Debug.Log("No rift endpoints in range");
            yield break;
        }

        EndUltimate();

        // Select the target in range which most closely matches the aim direction
        Transform targetEndPoint = null;
        Vector2 targetDashDirection = default;

        float targetDirectionDifference = 0;

        Vector2 aimDirection = (mousePos - (Vector2)transform.position).normalized;
        foreach (KeyValuePair<Transform, Vector2> endPointInRange in endPointsInRange)
        {
            // If targetEndPoint doesn't exist yet, or if its direction is closer to the aim direction than the saved endPoint's direction
            float newDirectionDifference = Vector2.Angle(aimDirection, endPointInRange.Value);

            if (targetEndPoint == null || newDirectionDifference < targetDirectionDifference)
            {
                targetEndPoint = endPointInRange.Key;
                targetDashDirection = endPointInRange.Value;

                targetDirectionDifference = newDirectionDifference;
            }
        }

        // Dash
        transform.position = targetEndPoint.position;

        ApplyStun(dashDuration, true);
        BecomeImmune(dashDuration);
        rb.velocity = riftLength / dashDuration * targetDashDirection;

        StartAbilityCooldown(1, ability2Cooldown);

        yield break;
    }

    protected override IEnumerator UseAbility3()
    {
        if (activeRifts.Count == 0)
        {
            Debug.Log("No rifts active");
            yield break;
        }

        EndUltimate();

        ApplyStun(explosionDelay, true);
        yield return new WaitForSeconds(explosionDelay);

        foreach (HiveRift rift in activeRifts)
            rift.Explode();

        // Cache currently active rifts
        List<HiveRift> rifts = new(activeRifts);
        activeRifts.Clear();

        yield return new WaitForSeconds(explosionDuration);

        // Destroy only cached rifts (in case other rifts were spawned during the explosion)
        foreach (HiveRift rift in rifts)
            Destroy(rift.gameObject);

        ChangeMoveSpeed(0, true, true);

        StartAbilityCooldown(2, ability3Cooldown);
    }
    public void ExplosionEnter(GameObject explosion, Collider2D col)
    {
        if (col.gameObject == gameObject)
            return;

        if (!col.TryGetComponent(out Player enemy))
            return;

        enemy.HealthChange(-riftDamage);
        Vector2 knockbackDirection = (col.transform.position - explosion.transform.position).normalized;
        enemy.ApplyKnockBack(knockbackDuration, knockbackDirection * knockbackForce);
    }

    protected override IEnumerator UseUltimate()
    {
        StartAbilityCooldown(3, ultimateCooldown);

        inUltimateMode = true;

        BecomeImmune(ultimateDuration);

        StartCoroutine(UltimateMode());

        yield return new WaitForSeconds(ultimateDuration);

        EndUltimate();
    }
    private IEnumerator UltimateMode()
    {
        while (inUltimateMode)
        {
            Vector2 randomPosition = Random.insideUnitCircle * ultimateRiftSpawnRange;
            Vector2 spawnPosition = randomPosition + (Vector2)transform.position;

            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            SpawnRift(spawnPosition, randomDirection);

            yield return new WaitForSeconds(ultimateRiftSpawnDelay);
        }
    }
    private void EndUltimate()
    {
        if (!inUltimateMode)
            return;

        CancelImmunity();

        inUltimateMode = false;
    }
}