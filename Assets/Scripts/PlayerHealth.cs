using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuração de Tiro")]
    public TextMeshProUGUI tmpLife;

    private bool isDead = false;
    public float maxHealth = 100f;
    private float currentHealth = 80;

    public Slider healthBar; // opcional — arraste uma UI Slider

    void Awake()
    {
        if (healthBar) healthBar.maxValue = maxHealth;
    }

    private void Update()
    {
        SetUI();
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"[Player] HP: {currentHealth}/{maxHealth}");

        if (healthBar) healthBar.value = currentHealth;

        if (currentHealth <= 0)
            Die();
        else
            GetComponent<PlayerController>().OnHit();
    }

    public void AddLife(float value)
    {
        currentHealth = Mathf.Clamp(currentHealth + value, 0, maxHealth);
    }

    void SetUI()
    {
        tmpLife.text = currentHealth.ToString() + "%";
    }

    void Die()
    {
        isDead = true;
        Debug.Log("[Player] Game Over.");
        GetComponent<PlayerController>().OnDeath();
        FindObjectOfType<GameOverManager>().AtivarTelaGameOver();
    }
}