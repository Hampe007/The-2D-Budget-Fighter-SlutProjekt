using UnityEngine;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Data")]
    public List<CharacterData> allCharacters;

    [Header("Panel Roots")]
    public GameObject carouselPanelRoot;
    public GameObject detailPanelRoot;

    [Header("Selectors & Display")]
    public CharacterCarouselSelector carouselSelector;
    public CharacterDetailDisplay    detailDisplay;

    // server‐side record of which clients have confirmed
    private HashSet<ulong> readyClients = new HashSet<ulong>();
    
    private CharacterData selectedCharacter;
    private int selectedAspectIndex;
    
    void Start()
    {
        // 1) Initialize the fighter carousel
        carouselSelector.Initialize(allCharacters);

        // 2) When they confirm a character, show the detail panel
        carouselSelector.OnCharacterChosen += idx =>
        {
            selectedCharacter = allCharacters[idx];
            detailDisplay.SetCharacter(selectedCharacter);

            // Subscribe to the detail‑panel events:
            detailDisplay.OnBack    += ShowCarousel;
            detailDisplay.OnConfirm += OnDetailConfirmed;

            // Fade to detail
            PanelTransition.SwapPanels(carouselPanelRoot, detailPanelRoot, 0.4f);
        };
    }
    /// <summary>
    /// Called when “Back” is pressed in detail. Returns to fighter carousel.
    /// </summary>
    private void ShowCarousel()
    {
        // Unsubscribe detail callbacks to avoid duplicates
        detailDisplay.OnBack    -= ShowCarousel;
        detailDisplay.OnConfirm -= OnDetailConfirmed;

        PanelTransition.SwapPanels(detailPanelRoot, carouselPanelRoot, 0.4f);
    }
    
    private void OnDetailConfirmed(int aspectIdx)
    {
        selectedAspectIndex = aspectIdx;
        
        Debug.Log($"Chosen: {selectedCharacter.displayName} Aspect {aspectIdx}");

        // hand off to the network controller
        SelectionNetworkController.Instance.SubmitSelection(selectedCharacter.id, aspectIdx);
        
    }
}
