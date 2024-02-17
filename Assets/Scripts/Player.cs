using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // PREFAB REFERENCE:
    [SerializeField] private GameObject healthBar;
    [SerializeField] private SpriteRenderer healthBarSR;
    [SerializeField] protected Rigidbody2D rb;

    // SCENE REFERENCE:
    [SerializeField] protected SceneReference sceneReference;

    [SerializeField] private bool isEnemy;

    // CONSTANT:
    private readonly float moveSpeed = 2.5f;
    // At this distance from the move click position, the player will stop moving
    private readonly float moveSnapDistance = .03f;

    private readonly float abilityBufferDuringStun = .3f;

    private readonly float knockBackDrag = 10;

    // DYNAMIC:
    private Camera mainCamera;

    protected Vector2 mousePos;

    private int currentHealth;
        // Set by each character
    protected int maxHealth;

    private readonly float[] abilityCurrentCooldowns = new float[4];
    private readonly float[] abilityCooldownDurations = new float[4];

    private List<int> abilityMaxCharges;
    private List<int> abilityCurrentCharges;


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

        if (isEnemy)
            return;

        // Copy the lists
        abilityMaxCharges = new(SetAbilityMaxCharges());
        abilityCurrentCharges = new(abilityMaxCharges);

        for (int i = 0; i < 4; i++)
            UpdateHudAbilityCharges(i);
    }

    protected virtual int SetMaxHealth()
    {
        Debug.LogError("Character did not set max health");
        return 0;
    }

    protected virtual List<int> SetAbilityMaxCharges()
    {
        // Characters do not need to override this method unless some abilities have extra charges
        return new() { 1, 1, 1, 1 };
    }

    private void Update()
    {
        if (isEnemy)
            return;

        for (int i = 0; i < 4; i++)
            UpdateAbilityCooldowns(i);

        if (Input.GetKeyDown(KeyCode.Q)) UseAbility(0);
        if (Input.GetKeyDown(KeyCode.W)) UseAbility(1);
        if (Input.GetKeyDown(KeyCode.E)) UseAbility(2);
        if (Input.GetKeyDown(KeyCode.R)) UseAbility(3);

        if (isStunned)
            return;

        mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(1))
            moveClickPosition = mousePos;

        if (Input.GetKeyDown(KeyCode.S))
            moveClickPosition = transform.position;
    }

    private void UseAbility(int ability)
    {
        if (abilityCurrentCharges[ability] == 0)
            return;

        if (isStunned)
        {
            StartCoroutine(BufferAbilityDuringStun(ability));
            return;
        }

        if (ability == 0) StartCoroutine(UseAbility1());
        if (ability == 1) StartCoroutine(UseAbility2());
        if (ability == 2) StartCoroutine(UseAbility3());
        if (ability == 3) StartCoroutine(UseUltimate());
    }
    private IEnumerator BufferAbilityDuringStun(int ability)
    {
        yield return new WaitForSeconds(abilityBufferDuringStun);

        UseAbility(ability);
    }

    private void FixedUpdate()
    {
        if (isEnemy)
            return;

        Movement();
    }

    private void LateUpdate()
    {
        if (isEnemy)
            return;

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


    protected void StartAbilityCooldown(int ability, float cooldown)
    {
        // Allow characters to set the ability cooldown each time in case a character's ability
        // doesn't always have the same cooldown duration
        abilityCurrentCooldowns[ability] = cooldown;
        abilityCooldownDurations[ability] = cooldown;

        if (abilityCurrentCharges[ability] < 1)
            Debug.LogError("Ability " + ability + " was used when it didn't have a charge");

        abilityCurrentCharges[ability] -= 1;
        UpdateHudAbilityCharges(ability);
    }

    // Run in Update for each ability
    private void UpdateAbilityCooldowns(int ability)
    {
        if (abilityCurrentCharges[ability] == abilityMaxCharges[ability])
            return;

        // Run cooldown. Note: this code only works if no character will ever change the max cooldown of
        // an ability while a cooldown is in progress, which should never happen unless an ability has
        // both multiple charges AND an inconsistent cooldown duration.
        if (abilityCurrentCooldowns[ability] > 0)
        {
            abilityCurrentCooldowns[ability] -= Time.deltaTime;

            float percentage = abilityCurrentCooldowns[ability] / abilityCooldownDurations[ability];
            sceneReference.hudAbilities[ability].fillAmount = percentage;

            return;
        }

        // Snap fillAmount after cooldown completes
        sceneReference.hudAbilities[ability].fillAmount = 0;

        abilityCurrentCharges[ability] += 1;
        UpdateHudAbilityCharges(ability);

        // Restart cooldown if not at max charges
        if (abilityCurrentCharges[ability] < abilityMaxCharges[ability])
            abilityCurrentCooldowns[ability] = abilityCooldownDurations[ability];
    }

    private void UpdateHudAbilityCharges(int ability)
    {
        if (abilityMaxCharges[ability] == 1)
            return;

        int currentCharges = abilityCurrentCharges[ability];
        string text = currentCharges > 0 ? currentCharges.ToString() : string.Empty;
        sceneReference.hudAbilityCharges[ability].text = text;
    }

    private void UpdateHealthBar()
    {
        float healthPercentage = currentHealth / (float)maxHealth;

        Vector2 healthBarSize = healthBarSR.size;
        // Ensure that healthBar never shrinks enough to become fully black
        healthBarSize.x = 4.5f * healthPercentage + .5f;
        healthBarSR.size = healthBarSize;
    }

    public void HealthChange(int amount)
    {
        if (amount < 0 && isImmune)
            return;

        currentHealth += amount;

        if (currentHealth < 0)
            currentHealth = 0;

        if (currentHealth == 0)
        {
            Debug.Log("Game over!");
            healthBar.SetActive(false);
        }
        else
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
        healthBar.SetActive(false);

        if (immunityRoutine != null)
            StopCoroutine(immunityRoutine);
        immunityRoutine = StartCoroutine(Immunity(duration));
    }
    public void CancelImmunity()
    {
        if (immunityRoutine != null)
        {
            StopCoroutine(immunityRoutine);
            StartCoroutine(Immunity(0));
        }
    }
    private IEnumerator Immunity(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (currentHealth > 0)
            healthBar.SetActive(true);

        immunityRoutine = null;
        isImmune = false;
    }
}