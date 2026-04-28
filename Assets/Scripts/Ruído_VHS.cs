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
        imagemChuvisco = GetComponent<RawImage>();
    }

    void Update()
    {
        Rect texturaRect = imagemChuvisco.uvRect;
        
        // unscaledDeltaTime faz o ruído mexer mesmo com o jogo pausado!
        texturaRect.x += velocidadeX * Time.unscaledDeltaTime;
        texturaRect.y += velocidadeY * Time.unscaledDeltaTime;
        
        imagemChuvisco.uvRect = texturaRect;
    }
}