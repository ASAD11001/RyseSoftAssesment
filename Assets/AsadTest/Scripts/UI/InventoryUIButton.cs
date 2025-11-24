using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class InventoryUIButton : MonoBehaviour, IPointerClickHandler
{
    
    private Image myImage;

    // Internal data
    public ShapeBlueprint itemData { get; private set; }
    private UnityAction myClickAction;

    void Awake()
    {
        myImage = GetComponent<Image>();
    }

    public void Setup(ShapeBlueprint item, Sprite icon, UnityAction onClick)
    {
        this.itemData = item;
        this.myClickAction = onClick;

        if (myImage != null)
        {
            // 1. Assign the Runtime Sprite (From Database)
            myImage.sprite = icon;

            // 2. Reset visibility to 100% (Normal state)
            SetSelected(false);
        }
    }

    // Runs when clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (myClickAction != null) myClickAction.Invoke();
    }

    // Runs when MainMenuUI tells it to fade
    public void SetSelected(bool isSelected)
    {
        if (myImage != null)
        {
            Color c = myImage.color;
            // 0.5 = Fade out. 1.0 = Full visibility.
            c.a = isSelected ? 0.5f : 1.0f;
            myImage.color = c;
        }
    }
}