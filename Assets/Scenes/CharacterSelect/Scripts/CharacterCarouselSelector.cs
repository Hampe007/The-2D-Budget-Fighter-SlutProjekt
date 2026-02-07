using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class CharacterCarouselSelector : MonoBehaviour
{
    [Header("Character Data List")]
    [SerializeField] private List<CharacterData> characters;

    [Header("UI References")]
    [SerializeField] private Image slotLeftImage;
    [SerializeField] private Image slotCenterImage;
    [SerializeField] private Image slotRightImage;

    [Header("Scroll & Tween Settings")]
    [Tooltip("Seconds between allowed scrolls")]
    public float scrollCooldown = 0.3f;
    [Tooltip("Alpha of non-selected slots")]
    public float sideAlpha = 0.5f;
    [Tooltip("Scale of non-selected slots")]
    public float sideScale = 0.8f;
    [Tooltip("Alpha of selected (center) slot")]
    public float centerAlpha = 1f;
    [Tooltip("Scale of selected (center) slot")]
    public float centerScale = 1.2f;
    
    [SerializeField] private int selectedIndex = 0;
    private bool canScroll = true;

    public event Action<int> OnCharacterChosen;
    private readonly Dictionary<Image, Sprite> lastSpriteBySlot = new();
    
    public void Initialize(List<CharacterData> allCharacters)
    {
        characters = allCharacters;
        selectedIndex = 0;
        UpdateSlots(instant: true);
        lastSpriteBySlot.Clear();
    }
    
    void Update()
    {
        if (!canScroll) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            StartCoroutine(ScrollCooldown(() => ChangeIndex(-1)));
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            StartCoroutine(ScrollCooldown(() => ChangeIndex(+1)));
        else if (Input.GetKeyDown(KeyCode.Space))
            OnCharacterChosen?.Invoke(selectedIndex);
    }

    public void MoveLeft()  => StartCoroutine(ScrollCooldown(() => ChangeIndex(-1)));
    public void MoveRight() => StartCoroutine(ScrollCooldown(() => ChangeIndex(+1)));
    public void ConfirmSelection() => OnCharacterChosen?.Invoke(selectedIndex);
    
    private void ChangeIndex(int delta)
    {
        selectedIndex = (selectedIndex + delta + characters.Count) % characters.Count;
        UpdateSlots(instant: false);
    }
    
    private IEnumerator ScrollCooldown(Action change)
    {
        canScroll = false;
        change();
        yield return new WaitForSeconds(scrollCooldown);
        canScroll = true;
    }
    
    void UpdateSlots(bool instant)
    {
        int count = characters.Count;
        int leftIndex  = (selectedIndex - 1 + count) % count;
        int rightIndex = (selectedIndex + 1) % count;

        ApplySlot(slotLeftImage,  characters[leftIndex].idleSprite, sideAlpha, sideScale, instant);
        ApplySlot(slotCenterImage, characters[selectedIndex].idleSprite,     centerAlpha, centerScale, instant);
        ApplySlot(slotRightImage, characters[rightIndex].idleSprite, sideAlpha, sideScale, instant);
    }

    void ApplySlot(Image img, Sprite sprite, float targetAlpha, float targetScale, bool instant)
    {
	    if (img == null)
		    return;

	    var group = img.GetComponent<CanvasGroup>();
	    if (group == null)
		    group = img.gameObject.AddComponent<CanvasGroup>();

	    bool hasLast = lastSpriteBySlot.TryGetValue(img, out var lastSprite);
	    bool spriteChanged = !hasLast || lastSprite != sprite;
	    lastSpriteBySlot[img] = sprite;

	    // Stop old tweens so rapid scrolling doesn’t stack
	    group.DOKill(false);
	    img.rectTransform.DOKill(false);

	    if (instant || !spriteChanged)
	    {
		    img.sprite = sprite;
		    group.alpha = targetAlpha;
		    img.rectTransform.localScale = Vector3.one * targetScale;
		    return;
	    }

	    float outTime = Mathf.Max(0.01f, scrollCooldown * 0.35f);
	    float inTime  = Mathf.Max(0.01f, scrollCooldown * 0.65f);

	    float fadeOutTo = Mathf.Clamp(targetAlpha * 0.15f, 0.02f, 0.2f);

	    group.DOFade(fadeOutTo, outTime)
		    .SetEase(Ease.InOutQuad)
		    .OnComplete(() =>
		    {
			    img.sprite = sprite;
			    group.DOFade(targetAlpha, inTime).SetEase(Ease.InOutQuad);
		    });

	    img.rectTransform
		    .DOScale(Vector3.one * targetScale, scrollCooldown)
		    .SetEase(Ease.OutBack);

	    bool isCenter = targetScale >= centerScale - 0.0001f;
	    if (isCenter)
	    {
		    img.rectTransform.DOPunchScale(Vector3.one * 0.08f, scrollCooldown, 6, 0.6f);
	    }
    }

}