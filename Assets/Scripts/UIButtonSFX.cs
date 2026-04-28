using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    public AudioSource hoverSource;
    public AudioSource clickSource;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSource != null && hoverSource.clip != null)
            hoverSource.PlayOneShot(hoverSource.clip);
    }

    public void PlayClick()
    {
        if (clickSource != null && clickSource.clip != null)
            clickSource.PlayOneShot(clickSource.clip);
    }
}