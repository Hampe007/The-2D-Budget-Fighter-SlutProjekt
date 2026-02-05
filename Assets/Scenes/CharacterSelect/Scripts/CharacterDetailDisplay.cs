using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterDetailDisplay : MonoBehaviour
{
	#region Serialized References
    [Header("References")]
    [SerializeField] private CharacterDetailCarouselSelector detailCarousel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button confirmButton;
	#endregion
	
    private CharacterData currentData;
    private int currentAspectIndex = 0;
    
    public event Action OnBack;
    public event Action<int> OnConfirm;
    
    #region Unity Lifecycle
    private void Awake()
    {
	    AutoWireReferences();
    }

    private void OnEnable()
    {
	    detailCarousel?.ForceCenterSelectedSlot();
    }
    #endregion
    
    public void SetCharacter(CharacterData data)
    {
        currentData = data;

        nameText.text  = data.displayName;
        titleText.text = data.title;

        detailCarousel.Initialize(
            data.idleSprite,
            data.basicAttackSprite,
            data.abilitySprite
        );
        
        detailCarousel.ForceCenterSelectedSlot();

        detailCarousel.OnDetailIndexChanged += OnAspectChanged;

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() => OnBack?.Invoke());

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => OnConfirm?.Invoke(currentAspectIndex));
    }

    void OnDisable()
    {
        detailCarousel.OnDetailIndexChanged -= OnAspectChanged;
    }

    private void OnAspectChanged(int idx)
    {
        currentAspectIndex = idx;
        switch (idx)
        {
            case 0:
                descriptionText.text = currentData.lore;
                break;
            case 1:
                descriptionText.text = currentData.basicAttackDescription;
                break;
            case 2:
                descriptionText.text = currentData.abilityDescription;
                break;
        }
    }
    #region Auto-Wiring
    private void AutoWireReferences()
    {
	    if (detailCarousel == null)
	    {
		    detailCarousel = GetComponentInChildren<CharacterDetailCarouselSelector>(true);
	    }

	    if (nameText == null || titleText == null || descriptionText == null)
	    {
		    foreach (var text in GetComponentsInChildren<TMP_Text>(true))
		    {
			    if (nameText == null && text.name.Contains("Name"))
			    {
				    nameText = text;
			    }
			    else if (titleText == null && text.name.Contains("Title"))
			    {
				    titleText = text;
			    }
			    else if (descriptionText == null && text.name.Contains("Description"))
			    {
				    descriptionText = text;
			    }
		    }
	    }

	    if (backButton == null || confirmButton == null)
	    {
		    foreach (var button in GetComponentsInChildren<Button>(true))
		    {
			    if (backButton == null && button.name.Contains("Back"))
			    {
				    backButton = button;
			    }
			    else if (confirmButton == null && button.name.Contains("Select"))
			    {
				    confirmButton = button;
			    }
		    }
	    }
    }
    #endregion
}