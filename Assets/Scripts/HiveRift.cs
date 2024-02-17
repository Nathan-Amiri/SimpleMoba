using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiveRift : MonoBehaviour
{
    [SerializeField] private GameObject explosion;
    [SerializeField] private BoxCollider2D explosionCol;

    [NonSerialized] public Hive hive;

    public List<Transform> endPoints = new();

    public void Explode()
    {
        explosion.SetActive(true);
        // If collider is enabled by default, gameObject.SetActive won't trigger OnTriggerEnter until the trigger detects motion
        // One simple workaround is to wait until after the gameObject is active to enable the collider
        explosionCol.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        hive.ExplosionEnter(gameObject, col);
    }
}