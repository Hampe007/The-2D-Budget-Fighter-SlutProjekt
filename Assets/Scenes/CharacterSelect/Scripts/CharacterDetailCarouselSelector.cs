using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;

public class CharacterDetailCarouselSelector : MonoBehaviour
{
	#region Serialized References
    [Header("UI Slots (Image + CanvasGroup on each)")]
    [SerializeField] private Image portraitSlotImage;
    [SerializeField] private Image basicSlotImage;
    [SerializeField] private Image abilitySlotImage;

    [Header("Centering")]
    [SerializeField] private RectTransform centerSlotOverride;
    
    [Header("Scroll Settings")]
    [Tooltip("Seconds between allowed scrolls")]
    [SerializeField] private float scrollCooldown = 0.3f;
    #endregion
    
    private Sprite[] sprites;
    private int selectedIndex;
    private bool canScroll = true;
    
    public event Action<int> OnDetailIndexChanged;
    
    #region Unity Lifecycle
    private void Awake()
    {
	    AutoWireReferences();
    }

    private void OnEnable()
    {
	    ForceCenterSelectedSlot();
    }
    #endregion

    public void Initialize(Sprite idleSprite, Sprite basicSprite, Sprite abilitySprite)
    {
        sprites = new[] { idleSprite, basicSprite, abilitySprite };
        selectedIndex = 0;
        UpdateSlots(instant: true);
        OnDetailIndexChanged?.Invoke(0);
    }
    
    void Update()
    {
        if (!canScroll) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            StartCoroutine(ScrollCooldown(() => ChangeIndex(-1)));
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            StartCoroutine(ScrollCooldown(() => ChangeIndex(+1)));
        else if (Input.GetKeyDown(KeyCode.Space))
            OnDetailIndexChanged?.Invoke(selectedIndex);
    }
    
    public void MoveLeft()  => StartCoroutine(ScrollCooldown(() => ChangeIndex(+1)));
    public void MoveRight() => StartCoroutine(ScrollCooldown(() => ChangeIndex(-1)));
    public void Confirm() => OnDetailIndexChanged?.Invoke(selectedIndex);
	
    public void ForceCenterSelectedSlot()
    {
        var centerTarget = centerSlotOverride != null
            ? centerSlotOverride
            : basicSlotImage != null ? basicSlotImage.rectTransform : null;
        if (centerTarget == null)
        {
            return;
        }

        EnsureCentered(centerTarget);
    }
    private IEnumerator ScrollCooldown(Action act)
    {
        canScroll = false;
        act();
        yield return new WaitForSeconds(scrollCooldown);
        canScroll = true;
    }

    private void ChangeIndex(int delta)
    {
        selectedIndex = (selectedIndex + delta + sprites.Length) % sprites.Length;
        UpdateSlots(instant: false);
        OnDetailIndexChanged?.Invoke(selectedIndex);
    }

    private void UpdateSlots(bool instant)
    {
        
        int count = sprites.Length;                 
        int left  = (selectedIndex - 1 + count) % count;
        int right = (selectedIndex + 1) % count;
        
        ApplySlot(portraitSlotImage, sprites[left],   false, instant);
        ApplySlot(basicSlotImage,    sprites[selectedIndex], true, instant);
        ApplySlot(abilitySlotImage,  sprites[right],  false, instant);
		ForceCenterSelectedSlot();
    }

    private void ApplySlot(Image img, Sprite sprite, bool isCenter, bool instant)
    {
        img.sprite = sprite;

        float targetAlpha = isCenter ? 1f : 0.5f;
        float targetScale = isCenter ? 1.2f : 0.8f;
        var group = img.GetComponent<CanvasGroup>();

        if (instant)
        {
            group.alpha = targetAlpha;
            img.rectTransform.localScale = Vector3.one * targetScale;
        }
        else
        {
            group.DOFade(targetAlpha, scrollCooldown);
            img.rectTransform.DOScale(Vector3.one * targetScale, scrollCooldown);
        }
    }
    
    #region Auto-Wiring
    private void AutoWireReferences()
    {
	    if (portraitSlotImage == null)
	    {
		    portraitSlotImage = FindImageByName("PortraitSlot");
	    }

	    if (basicSlotImage == null)
	    {
		    basicSlotImage = FindImageByName("BasicSlot");
	    }

	    if (abilitySlotImage == null)
	    {
		    abilitySlotImage = FindImageByName("AbilitySlot");
	    }

	    if (centerSlotOverride == null && basicSlotImage != null)
	    {
		    centerSlotOverride = basicSlotImage.rectTransform;
	    }
    }

    private Image FindImageByName(string targetName)
    {
	    var direct = transform.Find(targetName);
	    if (direct != null && direct.TryGetComponent<Image>(out var image))
	    {
		    return image;
	    }

	    foreach (var img in GetComponentsInChildren<Image>(true))
	    {
		    if (img.name == targetName)
		    {
			    return img;
		    }
	    }

	    return null;
    }
    #endregion

    #region Centering Helpers
    private void EnsureCentered(RectTransform rectTransform)
    {
	    WarnIfLayoutControlled(rectTransform);
	    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
	    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
	    rectTransform.pivot = new Vector2(0.5f, 0.5f);
	    rectTransform.anchoredPosition = Vector2.zero;
	    rectTransform.localRotation = Quaternion.identity;
	    rectTransform.localScale = Vector3.one;
    }

    private void WarnIfLayoutControlled(Component target)
    {
	    if (target == null)
	    {
		    return;
	    }

	    if (target.GetComponent<LayoutGroup>() != null || target.GetComponent<ContentSizeFitter>() != null)
	    {
		    Debug.LogWarning($"{nameof(CharacterDetailCarouselSelector)}: Layout components on {target.name} may override manual centering.");
	    }
    }
    #endregion
}
