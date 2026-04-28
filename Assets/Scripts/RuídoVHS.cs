using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RuidoMovimento : MonoBehaviour
{
    private RawImage imagemChuvisco;
    
    [Header("Velocidade do Ruído")]
    public float velocidadeX = 5f;
    public float velocidadeY = 5f;

    void Awake()
    {
        // Pega o componente RawImage automaticamente
        imagemChuvisco = GetComponent<RawImage>();
    }

    void Update()
    {
        // Pega as coordenadas atuais da imagem
        Rect texturaRect = imagemChuvisco.uvRect;
        
        // Desloca a textura (usando unscaledDeltaTime para o chuvisco não congelar no Game Over)
        texturaRect.x += velocidadeX * Time.unscaledDeltaTime;
        texturaRect.y += velocidadeY * Time.unscaledDeltaTime;
        
        // Aplica o novo posicionamento de volta na imagem
        imagemChuvisco.uvRect = texturaRect;
    }
}