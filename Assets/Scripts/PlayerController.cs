using NUnit.Framework;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float rotationSpeed = 10f;

    [Header("SFX")]
    public AudioSource footstepSource;
    public AudioSource clothesSource;
    public AudioSource pistolFire;
    public AudioSource pistolDry;
    public AudioSource pistolDraw;
    public AudioSource playerHurt;
    public AudioSource playerDeath;

    [Header("Velocidade")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Combate")]
    public bool isArmed = false;
    private bool isAiming = false; // NOVA: Controle de trava

    [Header("Configuração de Tiro")]
    public TextMeshProUGUI tmpBullet;
    private float quantBullet = 12;

    [Header("Efeito Visual")]
    public LineRenderer tiroLinha;
    public float tempoExibicaoLinha = 0.05f;

    [Header("Referências")]
    public Camera cameraTransform;

    private Animator animator;
    private Rigidbody rb;
    private Vector3 moveDirection;
    public GameObject pistola;
    private bool isDead = false;
    public bool isAttacking = false;
    private bool isHit = false;


    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        pistola.SetActive(isArmed);
        if (cameraTransform == null)
            cameraTransform = Camera.main;
    }

    void Update()
    {
        if (isDead) return;
        if (isHit) return;
        
        isAiming = Input.GetKey(KeyCode.Mouse1);
        animator.SetBool("IsAiming", isAiming);
        HandleRunning();
        HandleInteraction();
        HandleAttack();
        HandleGun();
        SetUI();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // AJUSTE: Se travar, as animações de pernas param (ficam em Idle)
        animator.SetFloat("Horizontal", Mathf.Abs(h));
        animator.SetFloat("RawHorizontal", h);

        if (!isAiming && (h != 0 || v != 0))
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }

        Vector3 camForward = cameraTransform.transform.forward;
        Vector3 camRight = cameraTransform.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = (camForward * v + camRight * h).normalized;
        if (!isAttacking && !isHit)
            Move(h);
    }

    void Move(float h)
    {
        // ROTAÇÃO: Fica fora de qualquer trava para o personagem sempre girar

        // MOVIMENTO: Só aplica velocidade se NÃO estiver travado com Q
        if (!isAiming)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            // Se travar, paramos o movimento horizontal mas mantemos a gravidade (y)
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            Quaternion rot = Quaternion.Euler(0f, h * rotationSpeed * 10 * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * rot);
        }
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[Player] Interação ativada!");
        }
    }

    void HandleRunning()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            animator.SetBool("IsRunning", true);
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift) || isAiming)
        {
            animator.SetBool("IsRunning", false);
        }
        HandleMoveSFX();
    }

    void HandleGun()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isArmed = !isArmed;
            pistola.SetActive(isArmed);

            if (isArmed)
            {
                GunDrawSFX();
            }

            Debug.Log("[Player] Armado: " + isArmed);
        }
          if(isAiming && isArmed)
        {
            animator.SetBool("WithGun", true);
        }
        else
        {
            animator.SetBool("WithGun", false);
        }
    }

    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isArmed)
            {
                if (quantBullet > 0)
                {
                    GunFireSFX();
                    ReduceBullet();
                    AttackArmed();
                    
                }
                else
                {
                    GunDrySFX();
                }
            }
            else
            {
                AttackUnarmed();
            }
        }
    }

    void AttackUnarmed()
    {   if(isAiming)
        {
        Debug.Log($"[Player] AttackUnarmed | isAttacking: {isAttacking} | isHit: {isHit}");
        if (isAttacking || isHit) return;
            isAttacking = true;
            moveDirection = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
            animator.SetTrigger("AttackUnarmed");
            Invoke(nameof(ResetAttack), 1.5f); 
        }
    }

    public void OnUnarmedHit()
    {
        RaycastHit hit;
        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out hit, 2f))
        {
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(5f);
                Debug.Log("[Player] Acertou o inimigo!");
            }
        }
    }
    void ReduceBullet()
    {
        quantBullet = Mathf.Clamp(quantBullet - 1, 0, 99);
    }

    public void AddBullet(float value)
    {
        quantBullet = Mathf.Clamp(quantBullet + value, 0, 99);
    }

    public void ReduceLife(float value)
    {
        quantBullet = Mathf.Clamp(quantBullet -= value, 0, 100);
    }

    public void AddLife(float value)
    {
        quantBullet = Mathf.Clamp(quantBullet + value, 0, 100);
    }

    void SetUI()
    {
        tmpBullet.text = quantBullet.ToString();
    }

    void AttackArmed()
    {
        if(isAiming)
        {
        animator.SetTrigger("AttackArmed");
        ShootBullet();    
        }
    }

    public void ShootBullet()
    {
        RaycastHit hit;
        Vector3 pontoFinal;
        if (Physics.SphereCast(transform.position + Vector3.up, 2f, transform.forward, out hit, 50f))
        {
            pontoFinal = hit.point;
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(15f);
        }
        else
        {
            pontoFinal = transform.position + Vector3.up + (transform.forward * 50f);
        }

        StartCoroutine(MostrarFeixe(pontoFinal));
    }

    void HandleMoveSFX()
    {
        bool isMoving = moveDirection.magnitude > 0.1f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        if (isMoving)
        {
            if (!footstepSource.isPlaying)
            {
                footstepSource.Play();
            }

            footstepSource.pitch = isRunning ? 1.3f : 1f;


            if (!clothesSource.isPlaying)
            {
                clothesSource.Play();
            }
            clothesSource.pitch = isRunning ? 1.6f : 1f;
        }
        else
        {
            if (footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }

            if (clothesSource.isPlaying)
            {
                clothesSource.Stop();
            }
        }
    }

    void GunFireSFX()
    {
        pistolFire.PlayOneShot(pistolFire.clip);
    }


    void GunDrySFX()
    {
        pistolDry.PlayOneShot(pistolDry.clip);
    }

    void GunDrawSFX()
    {
        pistolDraw.Play();
    }


    System.Collections.IEnumerator MostrarFeixe(Vector3 destino)
    {
        tiroLinha.SetPosition(0, transform.position + Vector3.up);
        tiroLinha.SetPosition(1, destino);
        tiroLinha.enabled = true;
        yield return new WaitForSeconds(tempoExibicaoLinha);
        tiroLinha.enabled = false;
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    public void OnHit()
    {   
        isHit = true;
        isAttacking = false;
        CancelInvoke(nameof(ResetAttack));
        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        playerHurt.PlayOneShot(playerHurt.clip);
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Hit");
        Invoke(nameof(ResetHit), 0.5f);
        // Debug.Log($"[Player] OnHit chamado | isHit: {isHit} | estado animator: {animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")}");
    }

    void ResetHit()
    {
        animator.ResetTrigger("Hit");
        isHit = false;
    }

    public void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        isHit = false;
        CancelInvoke();
        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        playerDeath.PlayOneShot(playerDeath.clip);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);
        animator.SetTrigger("Death");
        enabled = false;
    }
}
