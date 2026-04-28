using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    private Animator animator;
    private EnemyAI ai;
    public AudioSource damageSFX;
    public AudioSource deathSFX;    
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        ai = GetComponent<EnemyAI>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"[Inimigo] Levou {amount} de dano. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            deathSFX.PlayOneShot(deathSFX.clip);
            Die();
        }
        else
        {
            damageSFX.PlayOneShot(damageSFX.clip);
            animator.SetTrigger("Hit");
        }
    }

    void Die()
    {
        isDead = true;
        animator.SetBool("IsDead", true);
        animator.SetTrigger("Die");
        ai.enabled = false;
        GetComponent<Collider>().enabled = false;
        Debug.Log("[Inimigo] Morreu.");
        Destroy(gameObject, 3f);
    }

    public bool IsDead() => isDead;
}