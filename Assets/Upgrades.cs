using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Upgrades : MonoBehaviour
{
    [Serializable]
    public class SoapTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("Multiplier to apply to existing profit values (e.g. 1.5 = +50%)")]
        public float multiplier = 1f;
        public Sprite icon; // assign per-tier icon in inspector (bar soap, bottle, etc.)
    }

    [Serializable]
    public class GloveTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("How many additional dishes are added to the dish completion count when this tier is active (relative to base).")]
        public int dishesAdded = 0;
        public Sprite icon;
        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
    }
    [Serializable]
    public class SpongeTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("How many dish stages are completed per click (e.g. 1 = normal, 2 = double, etc.)")]
        public int stagesPerClick = 1;
        public Sprite icon;
        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
    }

    [Serializable]
    public class Mp3Tier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        public Sprite icon;
    }

    [Header("Soap Tiers (index 0 is the starting unlocked bar soap)")]
    public List<SoapTier> soapTiers = new List<SoapTier>();

    [Header("Soap UI")]
    public GameObject soapMenuPanel;
    public TMP_Text soapNameText;
    public TMP_Text soapDescText;
    public TMP_Text soapCostText;
    public Button soapUpgradeButton;
    public Button soapCloseButton;

    [Header("HUD Button Image (assign the Image component from the SoapButton)")]
    public Image soapButtonImage;

    [Header("Glove Tiers (index 0 is starting plastic gloves)")]
    public List<GloveTier> gloveTiers = new List<GloveTier>();

    [Header("Glove UI")]
    public GameObject gloveMenuPanel;
    public TMP_Text gloveNameText;
    public TMP_Text gloveDescText;
    public TMP_Text gloveCostText;
    public Button gloveUpgradeButton;
    public Button gloveCloseButton;

    [Header("HUD Button Image (assign the Image component from the GloveButton)")]
    public Image gloveButtonImage;

    [Header("Sponge Tiers (index 0 is starting basic sponge)")]
    public List<SpongeTier> spongeTiers = new List<SpongeTier>();

    [Header("Sponge UI")]
    public GameObject spongeMenuPanel;
    public TMP_Text spongeNameText;
    public TMP_Text spongeDescText;
    public TMP_Text spongeCostText;
    public Button spongeUpgradeButton;
    public Button spongeCloseButton;

    [Header("HUD Button Image (assign the Image component from the SpongeButton)")]
    public Image spongeButtonImage; 
    // Add UI fields for sponge menu if needed (similar to soap/glove) 

    [Header("MP3 Tiers (index 0 is the starting/unlocked mp3 upgrade)")]
    public List<Mp3Tier> mp3Tiers = new List<Mp3Tier>();

    [Header("MP3 UI")]
    public GameObject mp3MenuPanel;
    public TMP_Text mp3NameText;
    public TMP_Text mp3DescText;
    public TMP_Text mp3CostText;
    public Button mp3UpgradeButton;
    public Button mp3CloseButton;

    [Header("HUD Button Image (assign the Image component from the MP3Button)")]
    public Image mp3ButtonImage;

    [Header("MP3 HUD")]
    [Tooltip("Optional: assign the Button used in the HUD to open the MP3 menu (calls OpenMp3Menu).")]
    public Button mp3OpenButton;

    [Header("MP3 Player UI (shown after purchase)")]
    [Tooltip("Assign the MP3 player panel GameObject or Mp3PlayerUI component. When the MP3 upgrade is purchased, clicking the HUD MP3 button will open this instead.")]
    public GameObject mp3PlayerPanel;       // optional: panel GameObject for the advanced player
    public Mp3PlayerUI mp3PlayerUI;         // optional: script on the player panel that handles song list & controls

    [Header("Optional: full-screen transparent Button behind the panel")]
    [Tooltip("If set, clicking this Button will close the soap/glove menu. If not set, the script will try to detect clicks outside the panel via UI raycast.")]
    public Button backgroundOverlayButton;

    private int currentSoapIndex = 0;
    private int currentGloveIndex = 0;
    private int currentSpongeIndex = 0;
    private int currentMp3Index = 0;
    private EmployeeManager employeeManager;
    private ScoreManager scoreManager;

    // used for UI raycast fallback when no overlay button is provided
    private GraphicRaycaster graphicRaycaster;
    [Tooltip("Optional Canvas used for UI raycasts when backgroundOverlayButton is not provided. If null the script will try to find one at runtime.")]
    public Canvas raycastCanvas;

    private void Reset()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
        employeeManager = FindFirstObjectByType<EmployeeManager>();
    }

    private void Awake()
    {
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (employeeManager == null) employeeManager = FindFirstObjectByType<EmployeeManager>();

        // find canvas / graphic raycaster for raycast fallback
        if (raycastCanvas == null)
            raycastCanvas = FindFirstObjectByType<Canvas>();

        if (raycastCanvas != null)
            graphicRaycaster = raycastCanvas.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null && raycastCanvas != null)
            graphicRaycaster = raycastCanvas.gameObject.AddComponent<GraphicRaycaster>();

        // seed default soap tiers if none set in inspector
        if (soapTiers.Count == 0)
        {
            soapTiers.Add(new SoapTier
            {
                tierName = "Bar Soap",
                description = "Basic bar soap. No bonus. Click to upgrade.",
                cost = 0f,
                multiplier = 1f,
                icon = null
            });
            soapTiers.Add(new SoapTier
            {
                tierName = "Dish Soap",
                description = "Increases dish profit and employee income by 50%.",
                cost = 100f,
                multiplier = 1.5f,
                icon = null
            });
            soapTiers.Add(new SoapTier
            {
                tierName = "Premium Dish Soap",
                description = "Further increases dish profit and employee income (x2).",
                cost = 500f,
                multiplier = 2f,
                icon = null
            });
            soapTiers.Add(new SoapTier
            {
                tierName = "Industrial Degreaser",
                description = "Massive boost to all profit generation (x3).",
                cost = 2000f,
                multiplier = 3f,
                icon = null
            });
        }

        // seed default glove tiers if none set in inspector
        if (gloveTiers.Count == 0)
        {
            gloveTiers.Add(new GloveTier
            {
                tierName = "Plastic Gloves",
                description = "Cheap plastic gloves. No bonus. Click to upgrade.",
                cost = 0f,
                dishesAdded = 0,
                icon = null,
                requiredDishes = 0
            });
            gloveTiers.Add(new GloveTier
            {
                tierName = "Nitrile Gloves",
                description = "Nitrile gloves: +1 dish per completed cycle.",
                cost = 75f,
                dishesAdded = 1,
                icon = null,
                requiredDishes = 10 // must complete 10 dishes to unlock
            });
            gloveTiers.Add(new GloveTier
            {
                tierName = "Kevlar Gloves",
                description = "Kevlar gloves: +2 dishes per completed cycle.",
                cost = 250f,
                dishesAdded = 2,
                icon = null,
                requiredDishes = 100 // must complete 100 dishes to unlock
            });
        }

        // seed default sponge tiers if none set in inspector
        if (spongeTiers.Count == 0)
        {
            spongeTiers.Add(new SpongeTier
            {
                tierName = "Basic Sponge",
                description = "No bonus. Click to upgrade.",
                cost = 0f,
                stagesPerClick = 1,
                icon = null,
                requiredDishes = 0
            });
            spongeTiers.Add(new SpongeTier
            {
                tierName = "Scrubber Sponge",
                description = "Completes 2 stages per click.",
                cost = 150f,
                stagesPerClick = 2,
                icon = null,
                requiredDishes = 10 // must complete 10 dishes to unlock
            });
            spongeTiers.Add(new SpongeTier
            {
                tierName = "Steel Wool",
                description = "Completes 3 stages per click.",
                cost = 600f,
                stagesPerClick = 3,
                icon = null,
                requiredDishes = 100 // must complete 100 dishes to unlock
            });
        }

        // seed default mp3 tiers if none set in inspector
        // include a base (index 0) and the purchasable MP3 tier (index 1)
        if (mp3Tiers.Count == 0)
        {
            mp3Tiers.Add(new Mp3Tier
            {
                tierName = "No MP3",
                description = "Default radio. No custom songs.",
                loreDescription = string.Empty,
                cost = 0f,
                icon = null
            });
            mp3Tiers.Add(new Mp3Tier
            {
                tierName = "MP3",
                description = "Change the songs",
                loreDescription = string.Empty,
                cost = 100f,
                icon = null
            });
        }

        // Try to auto-assign mp3 UI references if user didn't assign them in inspector.
        AutoAssignMp3UI();

        // wire soap buttons
        if (soapUpgradeButton != null)
        {
            soapUpgradeButton.onClick.RemoveAllListeners();
            soapUpgradeButton.onClick.AddListener(OnSoapUpgradeButton);
        }
        if (soapCloseButton != null)
        {
            soapCloseButton.onClick.RemoveAllListeners();
            soapCloseButton.onClick.AddListener(CloseSoapMenu);
        }

        // wire glove buttons
        if (gloveUpgradeButton != null)
        {
            gloveUpgradeButton.onClick.RemoveAllListeners();
            gloveUpgradeButton.onClick.AddListener(OnGloveUpgradeButton);
        }
        if (gloveCloseButton != null)
        {
            gloveCloseButton.onClick.RemoveAllListeners();
            gloveCloseButton.onClick.AddListener(CloseGloveMenu);
        }

        // wire sponge buttons
        if (spongeUpgradeButton != null)
        {
            spongeUpgradeButton.onClick.RemoveAllListeners();
            spongeUpgradeButton.onClick.AddListener(OnSpongeUpgradeButton);
        }
        if (spongeCloseButton != null)
        {
            spongeCloseButton.onClick.RemoveAllListeners();
            spongeCloseButton.onClick.AddListener(CloseSpongeMenu);
        }

        // wire mp3 buttons
        if (mp3UpgradeButton != null)
        {
            mp3UpgradeButton.onClick.RemoveAllListeners();
            mp3UpgradeButton.onClick.AddListener(OnMp3UpgradeButton);
        }
        if (mp3CloseButton != null)
        {
            mp3CloseButton.onClick.RemoveAllListeners();
            mp3CloseButton.onClick.AddListener(CloseMp3Menu);
        }

        // wire mp3 HUD open button (optional)
        if (mp3OpenButton != null)
        {
            mp3OpenButton.onClick.RemoveAllListeners();
            mp3OpenButton.onClick.AddListener(OpenMp3Menu);
        }

        // overlay button is optional; if provided we hook it so clicking outside the panel closes the menu
        if (backgroundOverlayButton != null)
        {
            backgroundOverlayButton.onClick.RemoveAllListeners();
            // overlay should close whichever panel is open; keep single handler
            backgroundOverlayButton.onClick.AddListener(() =>
            {
                CloseSoapMenu();
                CloseGloveMenu();
                CloseSpongeMenu();
                CloseMp3Menu();

                // also close mp3 player panel if present
                if (mp3PlayerUI != null)
                    mp3PlayerUI.Close();
                else if (mp3PlayerPanel != null)
                    mp3PlayerPanel.SetActive(false);
            });
            // ensure overlay is hidden initially
            if (backgroundOverlayButton.gameObject.activeSelf)
                backgroundOverlayButton.gameObject.SetActive(false);
        }

        CloseSoapMenu();
        CloseGloveMenu();
        CloseSpongeMenu();
        CloseMp3Menu();
    }

    private void Start()
    {
        // ensure starting tiers are visible/known
        UpdateSoapMenuUI(); // ensure HUD icon matches initial soap tier
        UpdateGloveMenuUI(); // ensure HUD icon matches initial glove tier
        UpdateSpongeMenuUI(); // ensure HUD icon matches initial sponge tier
        UpdateMp3MenuUI(); // ensure HUD icon / menu for mp3 is initialized

        // Ensure the HUD MP3 button is bound to the appropriate action depending on whether MP3 is owned.
        RebindMp3OpenButtonForPlayer();
    }

    // Helper: rebind the HUD MP3 open button so after purchase it opens the player panel/UI
    private void RebindMp3OpenButtonForPlayer()
    {
        if (mp3OpenButton == null) return;

        // clear existing listeners and bind appropriately
        mp3OpenButton.onClick.RemoveAllListeners();

        if (currentMp3Index > 0)
        {
            // MP3 purchased -> open player UI if available
            if (mp3PlayerUI != null)
            {
                mp3OpenButton.onClick.AddListener(() =>
                {
                    if (backgroundOverlayButton != null)
                        backgroundOverlayButton.gameObject.SetActive(true);
                    mp3PlayerUI.Open();
                });
                return;
            }

            if (mp3PlayerPanel != null)
            {
                mp3OpenButton.onClick.AddListener(() =>
                {
                    if (backgroundOverlayButton != null)
                        backgroundOverlayButton.gameObject.SetActive(true);
                    mp3PlayerPanel.SetActive(true);
                });
                return;
            }
        }

        // Default: open the purchasable MP3 upgrade menu (existing behavior)
        mp3OpenButton.onClick.AddListener(OpenMp3Menu);
    }

    // --------- Soap UI API ----------
    public void OpenSoapMenu()
    {
        if (soapMenuPanel == null) return;
        UpdateSoapMenuUI();

        // show overlay if available
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        soapMenuPanel.SetActive(true);
    }

    public void CloseSoapMenu()
    {
        if (soapMenuPanel == null) return;
        soapMenuPanel.SetActive(false);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void UpdateSoapMenuUI()
    {
        if (soapTiers == null || soapTiers.Count == 0) return;

        var current = soapTiers[Mathf.Clamp(currentSoapIndex, 0, soapTiers.Count - 1)];

        if (soapNameText) soapNameText.text = current.tierName;
        if (soapDescText) soapDescText.text = current.description; // in-game effect

        // Where the old cost text was: now show IRL / lore description
        if (soapCostText)
            soapCostText.text = string.IsNullOrEmpty(current.loreDescription)
                ? string.Empty
                : current.loreDescription;

        // HUD icon stays the same
        if (soapButtonImage != null && current != null && current.icon != null)
            soapButtonImage.sprite = current.icon;

        bool hasNext = currentSoapIndex < soapTiers.Count - 1;

        if (soapUpgradeButton)
        {
            var btnText = soapUpgradeButton.GetComponentInChildren<TMP_Text>();
            float wallet = (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;

            if (hasNext)
            {
                var next = soapTiers[currentSoapIndex + 1];

                soapUpgradeButton.interactable = scoreManager != null && wallet >= next.cost;

                if (btnText != null)
                    btnText.SetText($"Upgrade for ${next.cost:0.00}");
            }
            else
            {
                soapUpgradeButton.interactable = false;
                if (btnText != null)
                    btnText.SetText("Max");
            }
        }
    }

    private void OnSoapUpgradeButton()
    {
        // attempt to upgrade to next tier
        if (currentSoapIndex >= soapTiers.Count - 1) return;
        var next = soapTiers[currentSoapIndex + 1];
        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();
        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName} (need ${next.cost:0.00})");
            return;
        }

        // pay and apply multiplier
        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        // Use ScoreManager API to apply global dish profit multiplier (avoids editing assets)
        scoreManager.MultiplyDishProfit(next.multiplier);

        // apply to employees via EmployeeManager if present
        if (employeeManager != null)
        {
            employeeManager.MultiplyEmployeeProfit(next.multiplier);
        }
        else
        {
            employeeManager = FindFirstObjectByType<EmployeeManager>();
            if (employeeManager != null) employeeManager.MultiplyEmployeeProfit(next.multiplier);
        }

        currentSoapIndex++;
        UpdateSoapMenuUI();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName} (x{next.multiplier:0.00})");
    }

    // --------- Glove UI API ----------
    public void OpenGloveMenu()
    {
        if (gloveMenuPanel == null) return;
        UpdateGloveMenuUI();

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        gloveMenuPanel.SetActive(true);
    }

    public void CloseGloveMenu()
    {
        if (gloveMenuPanel == null) return;
        gloveMenuPanel.SetActive(false);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void UpdateGloveMenuUI()
    {
        if (gloveTiers == null || gloveTiers.Count == 0) return;

        var current = gloveTiers[Mathf.Clamp(currentGloveIndex, 0, gloveTiers.Count - 1)];

        if (gloveNameText) gloveNameText.text = current.tierName;
        if (gloveDescText) gloveDescText.text = current.description; // in-game effect

        // Former cost text now shows IRL / lore description
        if (gloveCostText)
            gloveCostText.text = string.IsNullOrEmpty(current.loreDescription)
                ? string.Empty
                : current.loreDescription;

        if (gloveButtonImage != null && current != null && current.icon != null)
            gloveButtonImage.sprite = current.icon;

        bool hasNext = currentGloveIndex < gloveTiers.Count - 1;
        float wallet = (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;

        if (gloveUpgradeButton)
        {
            var btnText = gloveUpgradeButton.GetComponentInChildren<TMP_Text>();

            if (hasNext)
            {
                var next = gloveTiers[currentGloveIndex + 1];
                bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

                gloveUpgradeButton.interactable = unlocked && wallet >= next.cost;

                if (btnText != null)
                {
                    btnText.SetText(unlocked
                        ? $"Upgrade for ${next.cost:0.00}"
                        : $"Locked: {next.requiredDishes} dishes");
                }
            }
            else
            {
                gloveUpgradeButton.interactable = false;
                if (btnText != null)
                    btnText.SetText("Max");
            }
        }
    }

    private void OnGloveUpgradeButton()
    {
        // attempt to upgrade to next tier
        if (currentGloveIndex >= gloveTiers.Count - 1) return;
        var next = gloveTiers[currentGloveIndex + 1];
        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        // enforce milestone
        if (scoreManager.GetTotalDishes() < next.requiredDishes)
        {
            Debug.Log($"[Upgrades] {next.tierName} locked: requires {next.requiredDishes} dishes.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();
        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName} (need ${next.cost:0.00})");
            return;
        }

        // pay
        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        // compute how many dishes to add relative to current tier
        int currentAdded = gloveTiers[Mathf.Clamp(currentGloveIndex, 0, gloveTiers.Count - 1)].dishesAdded;
        int delta = next.dishesAdded - currentAdded;
        if (delta > 0)
        {
            // use ScoreManager API to increase dishes-per-complete
            scoreManager.IncreaseDishCountIncrement(delta);
        }

        currentGloveIndex++;
        UpdateGloveMenuUI();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName} (+{delta} dish per completion)");
    }
    
    // --------- Sponge UI API ----------
    public void OpenSpongeMenu()
    {
        if (spongeMenuPanel == null) return;
        UpdateSpongeMenuUI();

        // show overlay if available
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        spongeMenuPanel.SetActive(true);
    }
    public void CloseSpongeMenu()
    {
        if (spongeMenuPanel == null) return;
        spongeMenuPanel.SetActive(false);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void UpdateSpongeMenuUI()
    {
        if (spongeTiers == null || spongeTiers.Count == 0) return;

        var current = spongeTiers[Mathf.Clamp(currentSpongeIndex, 0, spongeTiers.Count - 1)];

        if (spongeNameText) spongeNameText.text = current.tierName;
        if (spongeDescText) spongeDescText.text = current.description; // in-game effect

        // Former cost text now shows IRL / lore description
        if (spongeCostText)
            spongeCostText.text = string.IsNullOrEmpty(current.loreDescription)
                ? string.Empty
                : current.loreDescription;

        if (spongeButtonImage != null && current != null && current.icon != null)
            spongeButtonImage.sprite = current.icon;

        bool hasNext = currentSpongeIndex < spongeTiers.Count - 1;
        float wallet = (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;

        if (spongeUpgradeButton)
        {
            var btnText = spongeUpgradeButton.GetComponentInChildren<TMP_Text>();

            if (hasNext)
            {
                var next = spongeTiers[currentSpongeIndex + 1];
                bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

                spongeUpgradeButton.interactable = unlocked && wallet >= next.cost;

                if (btnText != null)
                {
                    btnText.SetText(unlocked
                        ? $"Upgrade for ${next.cost:0.00}"
                        : $"Locked: {next.requiredDishes} dishes");
                }
            }
            else
            {
                spongeUpgradeButton.interactable = false;
                if (btnText != null)
                    btnText.SetText("Max");
            }
        }
    }

    private void OnSpongeUpgradeButton()
    {
        // attempt to upgrade to next tier
        if (currentSpongeIndex >= spongeTiers.Count - 1) return;
        var next = spongeTiers[currentSpongeIndex + 1];
        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        // enforce milestone like gloves
        if (scoreManager.GetTotalDishes() < next.requiredDishes)
        {
            Debug.Log($"[Upgrades] {next.tierName} locked: requires {next.requiredDishes} dishes.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();
        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName} (need ${next.cost:0.00})");
            return;
        }

        // pay
        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        // advance sponge tier
        currentSpongeIndex++;
        UpdateSpongeMenuUI();

        // Ensure DishClicker instances will pick up the new sponge value immediately.
        // DishClicker reads stagesPerClick from Upgrades.GetCurrentStagesPerClick() on click,
        // but some DishClicker instances may not have an Upgrades reference assigned.
        // Set this Upgrades instance on any DishClicker found so they will use the new value immediately.
        try
        {
            // prefer ScoreManager.activeDish if available
            if (scoreManager != null && scoreManager.activeDish != null)
            {
                scoreManager.activeDish.upgrades = this;
            }

            // assign to all DishClicker instances in scene so UI/auto-clickers behave consistently
            var allClickers = FindObjectsByType<DishClicker>(FindObjectsSortMode.None);
            for (int i = 0; i < allClickers.Length; i++)
            {
                if (allClickers[i] != null)
                    allClickers[i].upgrades = this;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Upgrades] Failed to apply sponge upgrade to DishClicker instances: {ex.Message}");
        }

        Debug.Log($"[Upgrades] Upgraded to {next.tierName} (stages per click: {next.stagesPerClick})");
    }


    public int GetCurrentStagesPerClick()
    {
        return spongeTiers[Mathf.Clamp(currentSpongeIndex, 0, spongeTiers.Count - 1)].stagesPerClick;
    }

    // --------- MP3 UI API ----------
    public void OpenMp3Menu()
    {
        // If MP3 upgrade has been purchased (any index > 0) and a player UI exists, open the player panel instead.
        if (currentMp3Index > 0)
        {
            // Prefer Mp3PlayerUI if assigned
            if (mp3PlayerUI != null)
            {
                if (backgroundOverlayButton != null)
                    backgroundOverlayButton.gameObject.SetActive(true);

                mp3PlayerUI.Open();
                return;
            }

            // fallback: open mp3PlayerPanel GameObject if assigned
            if (mp3PlayerPanel != null)
            {
                if (backgroundOverlayButton != null)
                    backgroundOverlayButton.gameObject.SetActive(true);

                mp3PlayerPanel.SetActive(true);
                return;
            }
        }

        // Otherwise open the purchasable MP3 upgrade menu (existing behavior)
        Debug.Log($"[Upgrades] OpenMp3Menu called. mp3MenuPanel is {(mp3MenuPanel == null ? "null" : "assigned")}");
        if (mp3MenuPanel == null) return;
        UpdateMp3MenuUI();

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        mp3MenuPanel.SetActive(true);
    }

    public void CloseMp3Menu()
    {
        if (mp3MenuPanel == null) return;
        mp3MenuPanel.SetActive(false);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void UpdateMp3MenuUI()
    {
        if (mp3Tiers == null || mp3Tiers.Count == 0) return;

        var current = mp3Tiers[Mathf.Clamp(currentMp3Index, 0, mp3Tiers.Count - 1)];

        if (mp3NameText) mp3NameText.text = current.tierName;
        if (mp3DescText) mp3DescText.text = current.description;

        if (mp3CostText)
            mp3CostText.text = string.IsNullOrEmpty(current.loreDescription)
                ? string.Empty
                : current.loreDescription;

        if (mp3ButtonImage != null && current != null && current.icon != null)
            mp3ButtonImage.sprite = current.icon;

        bool hasNext = currentMp3Index < mp3Tiers.Count - 1;

        if (mp3UpgradeButton)
        {
            var btnText = mp3UpgradeButton.GetComponentInChildren<TMP_Text>();
            float wallet = (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;

            if (hasNext)
            {
                var next = mp3Tiers[currentMp3Index + 1];
                mp3UpgradeButton.interactable = scoreManager != null && wallet >= next.cost;

                if (btnText != null)
                    btnText.SetText($"Upgrade for ${next.cost:0.00}");
            }
            else
            {
                mp3UpgradeButton.interactable = false;
                if (btnText != null)
                    btnText.SetText("Max");
            }
        }
    }

    private void OnMp3UpgradeButton()
    {
        // attempt to upgrade to next tier
        if (currentMp3Index >= mp3Tiers.Count - 1) return;
        var next = mp3Tiers[currentMp3Index + 1];
        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();
        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName} (need ${next.cost:0.00})");
            return;
        }

        // pay
        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        // advance mp3 tier
        currentMp3Index++;
        UpdateMp3MenuUI();

        // Rebind HUD MP3 button to open player UI/panel so next press opens the player
        RebindMp3OpenButtonForPlayer();

        // Optional: you can choose to immediately open the MP3 player UI after purchase.
        // If you'd like that UX, uncomment the following lines:
        
        if (mp3PlayerUI != null) mp3PlayerUI.Open();
        else if (mp3PlayerPanel != null) mp3PlayerPanel.SetActive(true);
        

        Debug.Log($"[Upgrades] Upgraded to {next.tierName} (mp3 purchased)");
    }

    private void Update()
    {
        // keep the upgrade interactable state up to date while menus are active
        if (soapMenuPanel != null && soapMenuPanel.activeSelf)
            UpdateSoapMenuUI();
        if (gloveMenuPanel != null && gloveMenuPanel.activeSelf)
            UpdateGloveMenuUI();
        if (spongeMenuPanel != null && spongeMenuPanel.activeSelf)
            UpdateSpongeMenuUI();
        if (mp3MenuPanel != null && mp3MenuPanel.activeSelf)
            UpdateMp3MenuUI();

        // if no explicit overlay Button configured, detect clicks outside the active panel via UI raycast
        if ((soapMenuPanel != null && soapMenuPanel.activeSelf || gloveMenuPanel != null && gloveMenuPanel.activeSelf || spongeMenuPanel != null && spongeMenuPanel.activeSelf || mp3MenuPanel != null && mp3MenuPanel.activeSelf) && backgroundOverlayButton == null)
        {
            // only respond to primary mouse button down / primary touch
            if (Input.GetMouseButtonDown(0))
            {
                // require EventSystem present for raycasts
                if (EventSystem.current == null || graphicRaycaster == null)
                    return;

                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                var results = new List<RaycastResult>();
                graphicRaycaster.Raycast(pointerData, results);

                bool clickedInsideAnyPanel = false;

                // check soap panel
                if (soapMenuPanel != null && soapMenuPanel.activeSelf)
                {
                    var rtSoap = soapMenuPanel.transform as RectTransform;
                    foreach (var r in results)
                    {
                        if (r.gameObject == null) continue;
                        if (rtSoap != null && (r.gameObject.transform as RectTransform) != null && (r.gameObject.transform as RectTransform).IsChildOf(rtSoap))
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                        if (r.gameObject == soapMenuPanel)
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                    }
                }

                // check glove panel if not already clicked inside soap
                if (!clickedInsideAnyPanel && gloveMenuPanel != null && gloveMenuPanel.activeSelf)
                {
                    var rtGlove = gloveMenuPanel.transform as RectTransform;
                    foreach (var r in results)
                    {
                        if (r.gameObject == null) continue;
                        if (rtGlove != null && (r.gameObject.transform as RectTransform) != null && (r.gameObject.transform as RectTransform).IsChildOf(rtGlove))
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                        if (r.gameObject == gloveMenuPanel)
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                    }
                }
                // check sponge panel if not already clicked inside soap or glove
                if (!clickedInsideAnyPanel && spongeMenuPanel != null && spongeMenuPanel.activeSelf)
                {
                    var rtSponge = spongeMenuPanel.transform as RectTransform;
                    foreach (var r in results)
                    {
                        if (r.gameObject == null) continue;
                        if (rtSponge != null && (r.gameObject.transform as RectTransform) != null && (r.gameObject.transform as RectTransform).IsChildOf(rtSponge))
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                        if (r.gameObject == spongeMenuPanel)
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                    }
                }

                // check mp3 panel if not already clicked inside others
                if (!clickedInsideAnyPanel && mp3MenuPanel != null && mp3MenuPanel.activeSelf)
                {
                    var rtMp3 = mp3MenuPanel.transform as RectTransform;
                    foreach (var r in results)
                    {
                        if (r.gameObject == null) continue;
                        if (rtMp3 != null && (r.gameObject.transform as RectTransform) != null && (r.gameObject.transform as RectTransform).IsChildOf(rtMp3))
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                        if (r.gameObject == mp3MenuPanel)
                        {
                            clickedInsideAnyPanel = true;
                            break;
                        }
                    }
                }

                if (!clickedInsideAnyPanel)
                {
                    CloseSoapMenu();
                    CloseGloveMenu();
                    CloseSpongeMenu();
                    CloseMp3Menu();
                }
            }
        }
    }
    // --- TEST/DEV ONLY: set SOAP tier to an index (free or spend) ---
    public void SetSoapTierIndex(int targetIndex, bool spend = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, soapTiers.Count - 1);
        if (targetIndex == currentSoapIndex) { UpdateSoapMenuUI(); return; }

        // move upward only; ignoring downgrades to keep math simple
        int from = currentSoapIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;
        float cumulativeMultiplier = 1f;

        for (int i = from + 1; i <= to; i++)
        {
            totalCost += soapTiers[i].cost;
            cumulativeMultiplier *= soapTiers[i].multiplier;
        }

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost) return;
        if (spend && scoreManager != null) scoreManager.SubtractProfit(totalCost, isPurchase: true);

        // apply the *net* multiplier once
        if (scoreManager != null) scoreManager.MultiplyDishProfit(cumulativeMultiplier);
        if (employeeManager != null) employeeManager.MultiplyEmployeeProfit(cumulativeMultiplier);

        currentSoapIndex = to;
        UpdateSoapMenuUI();
    }

    // --- TEST/DEV ONLY: set GLOVE tier to an index (free or spend) ---
    public void SetGloveTierIndex(int targetIndex, bool spend = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, gloveTiers.Count - 1);
        if (targetIndex == currentGloveIndex) { UpdateGloveMenuUI(); return; }

        int from = currentGloveIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;
        int addDishesDelta = gloveTiers[to].dishesAdded - gloveTiers[from].dishesAdded;

        // enforce required dishes only if spending (mirrors normal flow)
        if (spend && scoreManager != null && scoreManager.GetTotalDishes() < gloveTiers[to].requiredDishes) return;

        for (int i = from + 1; i <= to; i++) totalCost += gloveTiers[i].cost;

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost) return;
        if (spend && scoreManager != null) scoreManager.SubtractProfit(totalCost, isPurchase: true);

        if (addDishesDelta > 0 && scoreManager != null)
            scoreManager.IncreaseDishCountIncrement(addDishesDelta);

        currentGloveIndex = to;
        UpdateGloveMenuUI();
    }

    // --- TEST/DEV ONLY: set SPONGE tier to an index (free or spend) ---
    public void SetSpongeTierIndex(int targetIndex, bool spend = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, spongeTiers.Count - 1);
        if (targetIndex == currentSpongeIndex) { UpdateSpongeMenuUI(); return; }

        int from = currentSpongeIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;

        // enforce required dishes only if spending
        if (spend && scoreManager != null && scoreManager.GetTotalDishes() < spongeTiers[to].requiredDishes) return;

        for (int i = from + 1; i <= to; i++) totalCost += spongeTiers[i].cost;

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost) return;
        if (spend && scoreManager != null) scoreManager.SubtractProfit(totalCost, isPurchase: true);

        currentSpongeIndex = to;
        UpdateSpongeMenuUI();

        // mimic OnSpongeUpgradeButton side-effects so clickers pick up the new value immediately
        try
        {
            if (scoreManager != null && scoreManager.activeDish != null)
                scoreManager.activeDish.upgrades = this;

            var allClickers = FindObjectsByType<DishClicker>(FindObjectsSortMode.None);
            for (int i = 0; i < allClickers.Length; i++)
                if (allClickers[i] != null) allClickers[i].upgrades = this;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Upgrades.SetSpongeTierIndex] Apply failed: {ex.Message}");
        }
    }

    // Modified to include MP3 index so SaveManager can persist purchase
    public void GetSaveState(out int soap, out int glove, out int sponge, out int mp3)
    {
        soap = currentSoapIndex;
        glove = currentGloveIndex;
        sponge = currentSpongeIndex;
        mp3 = currentMp3Index;
    }

    // Apply save now restores MP3 purchase state as well.
    public void ApplySaveState(int soap, int glove, int sponge, int mp3)
    {
        // Make sure refs exist (bootstrapping can change init order)
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (employeeManager == null) employeeManager = FindFirstObjectByType<EmployeeManager>();

        // Only restore indices. Do NOT apply multipliers or dishCountIncrement here,
        // because ScoreManager already loaded those values from disk.
        currentSoapIndex = Mathf.Clamp(soap, 0, soapTiers.Count - 1);
        currentGloveIndex = Mathf.Clamp(glove, 0, gloveTiers.Count - 1);
        currentSpongeIndex = Mathf.Clamp(sponge, 0, spongeTiers.Count - 1);

        // Restore mp3 index (purchase state). Clamp to valid range.
        currentMp3Index = Mathf.Clamp(mp3, 0, mp3Tiers.Count - 1);

        UpdateSoapMenuUI();
        UpdateGloveMenuUI();
        UpdateSpongeMenuUI();
        UpdateMp3MenuUI();

        // Sponge needs the clickers to reference this Upgrades instance.
        try
        {   
            if (scoreManager != null && scoreManager.activeDish != null)
                scoreManager.activeDish.upgrades = this;

            var allClickers = FindObjectsByType<DishClicker>(FindObjectsSortMode.None);
            for (int i = 0; i < allClickers.Length; i++)
                if (allClickers[i] != null) allClickers[i].upgrades = this;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Upgrades] Failed to apply sponge refs on load: {ex.Message}");
        }

        // If MP3 was already owned on load, ensure the HUD button opens the player panel/UI.
        RebindMp3OpenButtonForPlayer();
    }

    // Auto-assign helper for MP3 UI references (called at Awake)
    private void AutoAssignMp3UI()
    {
        // Try a few common GameObject names for the panel
        if (mp3MenuPanel == null)
        {
            string[] panelNames = { "MP3MenuPanel" };
            foreach (var n in panelNames)
            {
                var go = GameObject.Find(n);
                if (go != null)
                {
                    mp3MenuPanel = go;
                    Debug.Log($"[Upgrades] Auto-assigned MP3MenuPanel from GameObject.Find(\"{n}\")");
                    break;
                }
            }
        }

        // Try to auto-find the HUD open button if not assigned
        if (mp3OpenButton == null)
        {
            string[] buttonNames = { "Mp3Button", "MP3Button", "mp3Button", "mp3OpenButton", "btnMp3" };
            foreach (var n in buttonNames)
            {
                var go = GameObject.Find(n);
                if (go != null)
                {
                    var btn = go.GetComponent<Button>();
                    if (btn != null)
                    {
                        mp3OpenButton = btn;
                        Debug.Log($"[Upgrades] Auto-assigned mp3OpenButton from GameObject.Find(\"{n}\")");
                        break;
                    }
                }
            }
        }

        // If we found the panel but not the close button, attempt to find a close button inside the panel
        if (mp3MenuPanel != null && mp3CloseButton == null)
        {
            // Direct child lookups
            var close = mp3MenuPanel.transform.Find("CloseButton")?.GetComponent<Button>()
                        ?? mp3MenuPanel.transform.Find("Close")?.GetComponent<Button>()
                        ?? mp3MenuPanel.transform.Find("X")?.GetComponent<Button>();

            if (close == null)
            {
                // fallback: search children for a Button with "close" or "x" in its name
                var buttons = mp3MenuPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    var lower = b.name.ToLower();
                    if (lower.Contains("close") || lower == "x")
                    {
                        close = b;
                        break;
                    }
                }
            }

            if (close != null)
            {
                mp3CloseButton = close;
                Debug.Log("[Upgrades] Auto-assigned mp3CloseButton from panel children.");
            }
        }

        // Try to auto-assign a player panel and Mp3PlayerUI script if available
        if (mp3PlayerPanel == null)
        {
            var p = GameObject.Find("MP3PlayerPanel") ?? GameObject.Find("Mp3PlayerPanel") ?? GameObject.Find("mp3PlayerPanel");
            if (p != null)
            {
                mp3PlayerPanel = p;
                Debug.Log("[Upgrades] Auto-assigned mp3PlayerPanel from GameObject.Find.");
            }
        }

        if (mp3PlayerUI == null && mp3PlayerPanel != null)
        {
            mp3PlayerUI = mp3PlayerPanel.GetComponent<Mp3PlayerUI>();
            if (mp3PlayerUI != null)
                Debug.Log("[Upgrades] Auto-assigned Mp3PlayerUI from mp3PlayerPanel.");
        }

        // Ensure mp3 panel is hidden initially
        if (mp3MenuPanel != null && mp3MenuPanel.activeSelf)
            mp3MenuPanel.SetActive(false);

        if (mp3PlayerPanel != null && mp3PlayerPanel.activeSelf)
            mp3PlayerPanel.SetActive(false);
    }
}