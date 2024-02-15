using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // PREFAB REFERENCE:
    [SerializeField] private SpriteRenderer healthBar;
    [SerializeField] protected Rigidbody2D rb;

    // SCENE REFERENCE:
    [SerializeField] protected SceneReference sceneReference;

    // CONSTANT:
    private readonly float moveSpeed = 2.5f;
    // At this distance from the move click position, the player will stop moving
    private readonly float moveSnapDistance = .03f;

    private readonly float knockBackDrag = 10;

    // DYNAMIC:
    private Camera mainCamera;

    protected Vector2 mousePos;

    private int currentHealth;
        // Set by each character
    protected int maxHealth;
    protected List<int> abilityCooldowns;

    private readonly List<bool> abilitiesOnCooldown = new() { false, false, false, false };

        // Movement
    private Vector2 moveClickPosition;
    protected Vector2 moveDirection;

    private float moveSpeedMultiplier = 1;

    private bool isStunned;
    private bool isImmune;

    private Coroutine stunRoutine;
    private Coroutine knockBackRoutine;
    private Coroutine immunityRoutine;

    protected virtual void Awake()
    {
        mainCamera = Camera.main;

        moveClickPosition = transform.position;

        maxHealth = SetMaxHealth();
        currentHealth = maxHealth;

        abilityCooldowns = SetAbilityCooldowns();
    }

    protected virtual int SetMaxHealth()
    {
        Debug.LogError("Character did not set max health");
        return 0;
    }

    protected virtual List<int> SetAbilityCooldowns()
    {
        Debug.LogError("Character did not set ability cooldowns");
        return default;
    }

    private void Update()
    {
        mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(1))
            moveClickPosition = mousePos;

        if (Input.GetKeyDown(KeyCode.S))
            moveClickPosition = transform.position;

        if (Input.GetKeyDown(KeyCode.Q) && !abilitiesOnCooldown[0]) StartCoroutine(UseAbility1());
        if (Input.GetKeyDown(KeyCode.W) && !abilitiesOnCooldown[1]) StartCoroutine(UseAbility2());
        if (Input.GetKeyDown(KeyCode.E) && !abilitiesOnCooldown[2]) StartCoroutine(UseAbility3());
        if (Input.GetKeyDown(KeyCode.R) && !abilitiesOnCooldown[3]) StartCoroutine(UseUltimate());
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void LateUpdate()
    {
        mainCamera.transform.position = new(transform.position.x, transform.position.y, -10);
    }

    // Run in FixedUpdate
    private void Movement()
    {
        if (isStunned)
            return;


        if (Vector2.Distance(transform.position, moveClickPosition) < moveSnapDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        moveDirection = (moveClickPosition - (Vector2)transform.position).normalized;
        rb.velocity = moveSpeed * moveSpeedMultiplier * moveDirection;
    }

    protected virtual IEnumerator UseAbility1() { yield break; }
    protected virtual IEnumerator UseAbility2() { yield break; }
    protected virtual IEnumerator UseAbility3() { yield break; }
    protected virtual IEnumerator UseUltimate() { yield break; }

    protected IEnumerator StartAbilityCooldown(int ability)
    {
        abilitiesOnCooldown[ability] = true;

        float cooldown = abilityCooldowns[ability];

        while (cooldown > 0)
        {
            cooldown -= Time.deltaTime;

            float percentage = cooldown / abilityCooldowns[ability];
            sceneReference.hudAbilities[ability].fillAmount = percentage;

            yield return null;
        }

        sceneReference.hudAbilities[ability].fillAmount = 0;

        abilitiesOnCooldown[ability] = false;
    }

    private void UpdateHealthBar()
    {
        float healthPercentage = currentHealth / (float)maxHealth;

        Vector2 healthBarSize = healthBar.size;
        // Ensure that healthBar never shrinks enough to become fully black
        healthBarSize.x = 4.5f * healthPercentage + .5f;
        healthBar.size = healthBarSize;
    }

    public void HealthChange(int amount)
    {
        if (amount < 0 && isImmune)
            return;

        currentHealth += amount;
        UpdateHealthBar();
    }

    public void ChangeMoveSpeed(float changeAmount, bool resetMoveSpeed = false, bool bypassImmunity = false)
    {
        if (isImmune && !bypassImmunity)
            return;

        if (resetMoveSpeed)
            moveSpeedMultiplier = 1;
        else
            moveSpeedMultiplier += changeAmount;
    }

    public void ApplyStun(float duration, bool bypassImmunity = false)
    {
        if (isImmune && !bypassImmunity)
            return;

        isStunned = true;
        rb.velocity = Vector2.zero;

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(Stun(duration));
    }
    private IEnumerator Stun(float duration)
    {
        yield return new WaitForSeconds(duration);

        rb.velocity = Vector2.zero;
        isStunned = false;
        moveClickPosition = transform.position;
    }

    public void ApplyKnockBack(float duration, Vector2 force)
    {
        if (isImmune)
            return;

        isStunned = true;
        rb.velocity = Vector2.zero;
        rb.drag = knockBackDrag;
        rb.AddForce(force, ForceMode2D.Impulse);

        if (knockBackRoutine != null)
            StopCoroutine(knockBackRoutine);
        knockBackRoutine = StartCoroutine(KnockBack(duration));
    }
    private IEnumerator KnockBack(float duration)
    {
        yield return new WaitForSeconds(duration);

        rb.velocity = Vector2.zero;
        rb.drag = 0;
        isStunned = false;
        moveClickPosition = transform.position;
    }

    public void BecomeImmune(float duration)
    {
        isImmune = true;

        if (immunityRoutine != null)
            StopCoroutine(immunityRoutine);
        immunityRoutine = StartCoroutine(Immunity(duration));
    }
    private IEnumerator Immunity(float duration)
    {
        yield return new WaitForSeconds(duration);

        isImmune = false;
    }
}