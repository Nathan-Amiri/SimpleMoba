using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // PREFAB REFERENCE:
    [SerializeField] private SpriteRenderer healthBar;

    // SCENE REFERENCE:
    [SerializeField] protected Rigidbody2D rb;

    [SerializeField] private List<Image> hudAbilities;

    // CONSTANT:
    private readonly float moveSpeed = 2.5f;
    // At this distance from the move click position, the player will stop moving
    private readonly float moveSnapDistance = .03f;

    private readonly float knockBackDrag = 10;

    // DYNAMIC:
    private Camera mainCamera;

    private Vector2 mousePos;

    private int currentHealth;
        // Set by each character
    protected int maxHealth;
    protected List<int> abilityCooldowns;

    private readonly List<bool> abilitiesOnCooldown = new() { false, false, false, false };

        // Movement
    private Vector2 moveClickPosition;

    private bool isStunned;

    private Coroutine stunRoutine;
    private Coroutine knockBackRoutine;

    private void Awake()
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

        if (Input.GetKeyDown(KeyCode.Q) && !abilitiesOnCooldown[0]) UseAbility1();
        if (Input.GetKeyDown(KeyCode.W) && !abilitiesOnCooldown[1]) UseAbility2();
        if (Input.GetKeyDown(KeyCode.E) && !abilitiesOnCooldown[2]) UseAbility3();
        if (Input.GetKeyDown(KeyCode.R) && !abilitiesOnCooldown[3]) UseUltimate();
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
        
        Vector2 moveDirection = (moveClickPosition - (Vector2)transform.position).normalized;
        rb.velocity = moveSpeed * moveDirection;
    }

    protected virtual void UseAbility1() { }
    protected virtual void UseAbility2() { }
    protected virtual void UseAbility3() { }
    protected virtual void UseUltimate() { }

    protected IEnumerator StartAbilityCooldown(int ability)
    {
        abilitiesOnCooldown[ability] = true;

        float cooldown = abilityCooldowns[ability];

        while (cooldown > 0)
        {
            cooldown -= Time.deltaTime;

            float percentage = cooldown / abilityCooldowns[ability];
            hudAbilities[ability].fillAmount = percentage;

            yield return null;
        }

        hudAbilities[ability].fillAmount = 0;

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
        currentHealth += amount;
        UpdateHealthBar();
    }

    public void ApplyStun(float duration)
    {
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
}