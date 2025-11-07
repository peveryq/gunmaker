using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying a single shop item tile
/// </summary>
public class ShopItemTile : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image partIconImage;
    [SerializeField] private Image manufacturerLogoImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Transform starsContainer;
    
    [Header("Star Icons")]
    [SerializeField] private Image[] starImages; // 5 star slots
    
    [Header("Button")]
    [SerializeField] private Button tileButton;
    
    private ShopOffering offering;
    private PartType partType;
    private int offeringIndex;
    private System.Action<PartType, int> onTileClicked;
    
    private void Awake()
    {
        // Setup button listener
        if (tileButton != null)
        {
            tileButton.onClick.AddListener(OnTileClicked);
        }
    }
    
    /// <summary>
    /// Populate tile with offering data
    /// </summary>
    public void Setup(ShopOffering offering, PartType partType, int index, 
                      Sprite filledStar, Sprite emptyStar, 
                      System.Action<PartType, int> onClickCallback)
    {
        this.offering = offering;
        this.partType = partType;
        this.offeringIndex = index;
        this.onTileClicked = onClickCallback;
        
        if (offering == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // Set part icon
        if (partIconImage != null)
        {
            if (offering.partIcon != null)
            {
                partIconImage.sprite = offering.partIcon;
                partIconImage.enabled = true;
            }
            else
            {
                partIconImage.enabled = false;
            }
        }
        
        // Set manufacturer logo
        if (manufacturerLogoImage != null)
        {
            if (offering.manufacturerLogo != null)
            {
                manufacturerLogoImage.sprite = offering.manufacturerLogo;
                manufacturerLogoImage.enabled = true;
            }
            else
            {
                manufacturerLogoImage.enabled = false;
            }
        }
        
        // Set price
        if (priceText != null)
        {
            priceText.text = $"{offering.price} $";
        }
        
        // Set stars (rarity)
        SetStars(offering.rarity, filledStar, emptyStar);
    }
    
    /// <summary>
    /// Set star icons based on rarity
    /// </summary>
    private void SetStars(int rarity, Sprite filledStar, Sprite emptyStar)
    {
        if (starImages == null || starImages.Length == 0)
            return;
        
        // Ensure we have exactly 5 stars
        for (int i = 0; i < 5 && i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                // Filled star if i < rarity, empty otherwise
                if (i < rarity)
                {
                    starImages[i].sprite = filledStar;
                    starImages[i].enabled = filledStar != null;
                }
                else
                {
                    starImages[i].sprite = emptyStar;
                    starImages[i].enabled = emptyStar != null;
                }
            }
        }
    }
    
    /// <summary>
    /// Called when tile button is clicked
    /// </summary>
    private void OnTileClicked()
    {
        onTileClicked?.Invoke(partType, offeringIndex);
    }
    
    /// <summary>
    /// Get the offering this tile represents
    /// </summary>
    public ShopOffering Offering => offering;
}

