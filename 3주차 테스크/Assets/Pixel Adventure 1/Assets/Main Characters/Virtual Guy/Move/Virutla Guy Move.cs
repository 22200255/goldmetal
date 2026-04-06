using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float doubleJumpForce = 6f;
    public int maxJumps = 2;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Combat (Hit & Knockback)")]
    // 속도를 직접 제어하므로 수치를 살짝 조절해야 할 수 있습니다.
    public float knockbackForceX = 4f;
    public float knockbackForceY = 6f;
    public float invincibleDuration = 1.5f;
    public float minAirTime = 0.2f; // 바닥에서 확실히 떨어지기 위한 최소 보장 시간

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool isGrounded;
    private int jumpCount;

    private bool isHurt = false;
    private bool isInvincible = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckGrounded();

        if (isHurt)
        {
            UpdateAnimations();
            return;
        }

        UpdateAnimations();
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            TryJump();
        }

        if (moveInput > 0) spriteRenderer.flipX = false;
        else if (moveInput < 0) spriteRenderer.flipX = true;
    }

    void FixedUpdate()
    {
        if (isHurt) return;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            jumpCount = 0;
        }
    }

    private void TryJump()
    {
        if (isGrounded || jumpCount < maxJumps)
        {
            if (!isGrounded) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            float currentJumpForce = (jumpCount == 0) ? jumpForce : doubleJumpForce;
            rb.AddForce(Vector2.up * currentJumpForce, ForceMode2D.Impulse);
            jumpCount++;
            anim.SetTrigger("Jump");
        }
    }

    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("YVelocity", rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInvincible && collision.gameObject.CompareTag("Spike"))
        {
            TakeDamage(collision.transform.position);
        }
    }

    private void TakeDamage(Vector2 damageSourcePos)
    {
        if (isHurt) return;

        StartCoroutine(HurtRoutine(damageSourcePos));
        StartCoroutine(InvincibilityRoutine());
    }

    // 💥 핵심 수정: 넉백 로직 개선
    private IEnumerator HurtRoutine(Vector2 damageSourcePos)
    {
        isHurt = true;

        // 1. 방향 계산
        int pushDirection = transform.position.x < damageSourcePos.x ? -1 : 1;

        // 2. AddForce 대신 속도를 직접 할당해버림 (무조건 동일한 포물선을 그리게 됨)
        rb.linearVelocity = new Vector2(pushDirection * knockbackForceX, knockbackForceY);

        // 3. 최소 체공 시간 보장 (이 시간 동안은 바닥에 닿아도 무시하고 날아감)
        // 이 수치(0.2f)가 높을수록 넉백이 더 강하게(오래) 체감됩니다.
        yield return new WaitForSeconds(minAirTime);

        // 4. 이제부터 바닥에 닿을 때까지 대기
        yield return new WaitUntil(() => isGrounded);

        // 5. 바닥에 닿았을 때 미끄러짐 방지 (원한다면 제거 가능)
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        isHurt = false; // 조작 권한 복구
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsedTime = 0f;
        float blinkInterval = 0.1f;

        while (elapsedTime < invincibleDuration)
        {
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(blinkInterval);
            spriteRenderer.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += (blinkInterval * 2);
        }

        spriteRenderer.color = new Color(1, 1, 1, 1f);
        isInvincible = false;
    }
}