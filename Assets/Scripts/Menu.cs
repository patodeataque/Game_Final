using UnityEngine;
using UnityEngine.Video;

public class Menu : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject menuOpcoes, rawImage;

    public AudioSource confirm;
    public bool has_confirmed = false;
    void Start()
    {
        rawImage.SetActive(false);
    }

    
    void Update()
    {
        if (!videoPlayer.isPlaying && Input.anyKeyDown)
        {
            if (!has_confirmed)
            {
                confirm.PlayOneShot(confirm.clip);
                has_confirmed = true;
            }
            videoPlayer.Play();
            rawImage.SetActive(true);
            menuOpcoes.SetActive(true);
        }
    }
}
