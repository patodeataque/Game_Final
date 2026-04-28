using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Configurações de UI")]
    public GameObject painelGameOver;
    public CanvasGroup grupoCanvas; 

    [Header("Configurações de Tempo")]
    public float tempoAnimacao = 2f; // Tempo da animação antes da fita "cortar"

    public void AtivarTelaGameOver()
    {
        StartCoroutine(SequenciaFimDeFita());
    }

    private IEnumerator SequenciaFimDeFita()
    {
        // 1. Espera o desastre acontecer na tela
        yield return new WaitForSeconds(tempoAnimacao);

        // 2. "CORTA" a fita: liga a tela de Game Over no máximo instantaneamente
        painelGameOver.SetActive(true);
        grupoCanvas.alpha = 1f;

        // 3. Trava a física e o tempo na mesma hora (como um pause no VCR)
        Time.timeScale = 0f; 

        // 4. O Truque de Arte: Muta todo o som do jogo para dar o "silêncio da fita"
        AudioListener.pause = true; 

        // 5. Libera o mouse para clicar nos botões
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Recomecar()
    {
        Time.timeScale = 1f; 
        AudioListener.pause = false; // Muito importante: devolve o som ao jogo!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VoltarMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false; // Devolve o som ao jogo!
        SceneManager.LoadScene("SampleScene"); 
    }
}