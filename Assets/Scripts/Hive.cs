using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Hive : Player
{
    [SerializeField] private GameObject riftPref;

    private readonly float slowAmountPerRift = .06f;

    public float dashDuration;

    //private readonly Dictionary<GameObject, Vector2> activeRiftsAndDirections = new();
    private readonly List<GameObject> activeRifts = new();

    private float riftLength;
    private float ability2Range;

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

    protected override void Awake()
    {
        base.Awake();

        riftLength = riftPref.transform.localScale.y;
        ability2Range = (transform.localScale.x / 2) + (riftPref.transform.GetChild(0).lossyScale.x / 2);
    }

    protected override IEnumerator UseAbility1()
    {
        Vector2 spawnDirection = (mousePos - (Vector2)transform.position).normalized;
        Vector2 spawnPosition = (Vector2)transform.position + (riftLength / 2 * spawnDirection);
        GameObject rift = Instantiate(riftPref, spawnPosition, Quaternion.identity, sceneReference.petParent);
        rift.transform.up = spawnDirection;

        activeRifts.Add(rift);

        ChangeMoveSpeed(-slowAmountPerRift, false, true);

        StartCoroutine(StartAbilityCooldown(0));

        yield break;
    }

    protected override IEnumerator UseAbility2()
    {
        // Save end points in range and the directions of their rifts
        Dictionary<Transform, Vector2> endPointsInRange = new();

        foreach (GameObject rift in activeRifts)
            for (int i = 0; i < 2; i++)
            {
                Transform endPointTransform = rift.transform.GetChild(i).transform;
                if (Vector2.Distance(endPointTransform.position, transform.position) <= ability2Range)
                {
                    Vector2 dashDirection = (rift.transform.position - endPointTransform.position).normalized;
                    endPointsInRange.Add(endPointTransform, dashDirection);
                }
            }

        // If no end points in range, return
        if (endPointsInRange.Count == 0)
        {
            Debug.Log("no rift endpoints in range");
            yield break;
        }

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

        StartCoroutine(StartAbilityCooldown(1));

        yield break;
    }

    protected override IEnumerator UseAbility3()
    {
        if (activeRifts.Count == 0)
            yield break;

        ApplyStun(.4f, true);
        yield return new WaitForSeconds(.4f);

        foreach (GameObject rift in activeRifts)
            Destroy(rift);
        activeRifts.Clear();

        ChangeMoveSpeed(0, true, true);

        StartCoroutine(StartAbilityCooldown(3));
    }

    protected override IEnumerator UseUltimate()
    {
        yield break;
    }

    //public float force;
    //public float duration;
    //ApplyKnockBack(duration, rb.velocity.normalized * force);
}