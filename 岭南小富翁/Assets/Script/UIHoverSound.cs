using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIHover);
    }
}
