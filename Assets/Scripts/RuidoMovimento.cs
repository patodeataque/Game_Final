using UnityEngine;
using UnityEngine.UI;

public class RuidoMovimento : MonoBehaviour {
    RawImage img;
    void Start() => img = GetComponent<RawImage>();

    void Update() {
        
        img.uvRect = new Rect(Random.value, Random.value, 1, 1);
    }
}