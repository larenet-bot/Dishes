using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Upgrades : MonoBehaviour
{
    [Serializable]
    public class RadioTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("How many additional dishes are added to the dish completion count when this tier is active (relative to base).")]
        public int dishesAdded = 0;
        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
        [Tooltip("Control the music")]
        public Sprite icon; // assign per-tier icon in inspector (bar soap, bottle, etc.)
    }

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

    [Header("Radio Tiers")]
    public List<RadioTier> radioTiers = new List<RadioTier>();

    [Header("Radio UI")]
    public GameObject radioMenuPanel;
    public TMP_Text radioNameText;
    public TMP_Text radioDescText;
    public TMP_Text radioCostText;
    public Button radioUpgradeButton;
    public Button radioCloseButton;

    [Header("HUD Button Image (assign the Image component from the radioButton)")]
    public Image RadioButtonImage;

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

    [Header("Optional: full-screen transparent Button behind the panel")]
    [Tooltip("If set, clicking this Button will close the soap/glove menu. If not set, the script will try to detect clicks outside the panel via UI raycast.")]
    public Button backgroundOverlayButton;

    private int currentSoapIndex = 0;
    private int currentGloveIndex = 0;
    private int currentSpongeIndex = 0;
    private int currentRadioIndex = 0;
    private EmployeeManager employeeManager;
    private ScoreManager scoreManager;

    // used for UI raycast fallback when no overlay button is provided
    private GraphicRaycaster graphicRaycaster;
    [Tooltip("Optional Canvas used for UI raycasts when backgroundOverlayButton is not provided. If null the script will try to find one at runtime.")]
    public Canvas raycastCanvas;
    private bool radioPurchased = false;

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

        // seed default radio tiers if none set in inspector (single tier only)
        if (radioTiers.Count == 0)
        {
            radioTiers.Add(new RadioTier
            {
                tierName = "Radio",
                description = "Old radio. No upgrades available.",
                cost = 0f,
                dishesAdded = 0,
                requiredDishes = 0,
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
        // wire radio buttons (single-tier: keep handler but UI will show Max)
        if (radioUpgradeButton != null)
        {
            radioUpgradeButton.onClick.RemoveAllListeners();
            radioUpgradeButton.onClick.AddListener(OnRadioUpgradeButton);
        }
        if (radioCloseButton != null)
        {
            radioCloseButton.onClick.RemoveAllListeners();
            radioCloseButton.onClick.AddListener(CloseRadioMenu);
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
                CloseRadioMenu();
            });
            // ensure overlay is hidden initially
            if (backgroundOverlayButton.gameObject.activeSelf)
                backgroundOverlayButton.gameObject.SetActive(false);
        }

        CloseSoapMenu();
        CloseGloveMenu();
        CloseSpongeMenu();
        CloseRadioMenu();
    }

    public void CloseRadioMenu()
    {
        if (radioMenuPanel == null) return;
        radioMenuPanel.SetActive(false);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        // ensure starting tiers are visible/known
        UpdateSoapMenuUI(); // ensure HUD icon matches initial soap tier
        UpdateGloveMenuUI(); // ensure HUD icon matches initial glove tier
        UpdateSpongeMenuUI(); // ensure HUD icon matches initial sponge tier
        UpdateRadioMenuUI();
    }

    public void UpdateRadioMenuUI()
    {
        if (radioTiers == null || radioTiers.Count == 0) return;

        var current = radioTiers[Mathf.Clamp(currentRadioIndex, 0, radioTiers.Count - 1)];

        if (radioNameText) radioNameText.text = current.tierName;
        if (radioDescText) radioDescText.text = current.description; // in-game effect

        // Former cost text now shows IRL / lore description
        if (radioCostText)
            radioCostText.text = string.IsNullOrEmpty(current.loreDescription)
                ? string.Empty
                : current.loreDescription;

        // HUD icon stays the same
        if (RadioButtonImage != null && current != null && current.icon != null)
            RadioButtonImage.sprite = current.icon;

        bool hasNext = currentRadioIndex < radioTiers.Count - 1;
        float wallet = score_manager_safe();

        if (radioUpgradeButton)
        {
            var btnText = radioUpgradeButton.GetComponentInChildren<TMP_Text>();

            if (hasNext)
            {
                var next = radioTiers[currentRadioIndex + 1];
                bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

                radioUpgradeButton.interactable = unlocked && wallet >= next.cost;

                if (btnText != null)
                {
                    btnText.SetText(unlocked
                        ? $"Upgrade for ${next.cost:0.00}"
                        : $"Locked: {next.requiredDishes} dishes");
                }
            }
            else
            {
                radioUpgradeButton.interactable = !radioPurchased;
                if (btnText != null)
                    btnText.SetText(radioPurchased ? "Owned" : "Buy");
            }

        }
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
    public void OpenRadioMenu()
    {
        if (radioMenuPanel == null) return;

        // If radio not purchased yet, show the purchase UI
        if (!radioPurchased)
        {
            UpdateRadioMenuUI();

            if (backgroundOverlayButton != null)
                backgroundOverlayButton.gameObject.SetActive(true);

            radioMenuPanel.SetActive(true);
            return;
        }

        // Radio already purchased → open radio control panel instead
        OpenRadioControlPanel();
    }
    [SerializeField] private GameObject radioControlPanel;

    private void OpenRadioControlPanel()
    {
        if (radioControlPanel == null)
        {
            Debug.LogWarning("[Upgrades] Radio control panel not assigned.");
            return;
        }

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        radioControlPanel.SetActive(true);
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
            float wallet = (scoreManager != null) ? score_manager_safe() : 0f;

            if (hasNext)
            {
                var next = soapTiers[currentSoapIndex + 1];

                soapUpgradeButton.interactable = score_manager_safe() >= next.cost;

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

    // small helper to avoid repetitive null checks for scoreManager
    private float score_manager_safe()
    {
        return (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;
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
            if (employeeManager != null) employee_manager_safe_multiply(next.multiplier);
        }

        currentSoapIndex++;
        UpdateSoapMenuUI();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName} (x{next.multiplier:0.00})");
    }

    private void employee_manager_safe_multiply(float multiplier)
    {
        if (employeeManager != null) employeeManager.MultiplyEmployeeProfit(multiplier);
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
        float wallet = score_manager_safe();

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

    // --------- Radio  ----------
    private void OnRadioUpgradeButton()
    {
        if (radioPurchased) return;

        var radio = radioTiers[0];

        if (scoreManager == null) return;

        float wallet = scoreManager.GetTotalProfit();
        if (wallet < radio.cost)
        {
            Debug.Log("[Upgrades] Not enough money to buy Radio.");
            return;
        }

        // Pay once
        scoreManager.SubtractProfit(radio.cost, isPurchase: true);

        radioPurchased = true;

        UpdateRadioMenuUI();
        CloseRadioMenu();

        // Immediately open the real radio panel
        OpenRadioControlPanel();

        // Stop ambient loop and start radio playback (if RadioCOntroller exists)
        try
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.DisableAmbientLooping();
            }

            var radioController = FindFirstObjectByType<RadioCOntroller>();
            if (radioController != null)
            {
                radioController.StartRadio();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Upgrades] Failed to start radio after purchase: {ex.Message}");
        }

        Debug.Log("[Upgrades] Radio purchased.");
    }


    // --- TEST/DEV ONLY: set RADIO tier to an index (free or spend) ---
    public void SetRadioTierIndex(int targetIndex, bool spend = false)
    {
        // Clamp to available single tier safely.
        targetIndex = Mathf.Clamp(targetIndex, 0, radioTiers.Count - 1);
        if (targetIndex == currentRadioIndex) { UpdateRadioMenuUI(); return; }

        // Only allow moving forward if there are multiple tiers (defensive).
        int from = currentRadioIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;
        int addDishesDelta = radioTiers[to].dishesAdded - radioTiers[from].dishesAdded;

        if (spend && score_manager_has_enough_dishes(radioTiers[to].requiredDishes) == false) return;

        for (int i = from + 1; i <= to; i++) totalCost += radioTiers[i].cost;

        if (spend && score_manager_safe() < totalCost) return;
        if (spend && scoreManager != null) scoreManager.SubtractProfit(totalCost, isPurchase: true);

        if (addDishesDelta > 0 && scoreManager != null)
            scoreManager.IncreaseDishCountIncrement(addDishesDelta);

        currentRadioIndex = to;
        UpdateRadioMenuUI();
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
        float wallet = score_manager_safe();

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
        if (score_manager_has_enough_dishes(next.requiredDishes) == false)
        {
            Debug.Log($"[Upgrades] {next.tierName} locked: requires {next.requiredDishes} dishes.");
            return;
        }

        float wallet = score_manager_safe();
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
                score_manager_assign_active_dish();
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

    private void score_manager_assign_active_dish()
    {
        if (scoreManager != null && scoreManager.activeDish != null)
            scoreManager.activeDish.upgrades = this;
    }

    private bool score_manager_has_enough_dishes(int required)
    {
        return scoreManager != null && scoreManager.GetTotalDishes() >= required;
    }

    public int GetCurrentStagesPerClick()
    {
        return spongeTiers[Mathf.Clamp(currentSpongeIndex, 0, spongeTiers.Count - 1)].stagesPerClick;
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

        if (spend && score_manager_safe() < totalCost) return;
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
        if (spend && score_manager_has_enough_dishes(gloveTiers[to].requiredDishes) == false) return;

        for (int i = from + 1; i <= to; i++) totalCost += gloveTiers[i].cost;

        if (spend && score_manager_safe() < totalCost) return;
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
        if (spend && score_manager_has_enough_dishes(spongeTiers[to].requiredDishes) == false) return;

        for (int i = from + 1; i <= to; i++) totalCost += spongeTiers[i].cost;

        if (spend && score_manager_safe() < totalCost) return;
        if (spend && scoreManager != null) scoreManager.SubtractProfit(totalCost, isPurchase: true);

        currentSpongeIndex = to;
        UpdateSpongeMenuUI();

        // mimic OnSpongeUpgradeButton side-effects so clickers pick up the new value immediately
        try
        {
            score_manager_assign_active_dish();

            var allClickers = FindObjectsByType<DishClicker>(FindObjectsSortMode.None);
            for (int i = 0; i < allClickers.Length; i++)
                if (allClickers[i] != null) allClickers[i].upgrades = this;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Upgrades.SetSpongeTierIndex] Apply failed: {ex.Message}");
        }
    }

    // Modified save helpers
    public void GetSaveState(out int soap, out int glove, out int sponge, out bool radioOwned)
    {
        soap = currentSoapIndex;
        glove = currentGloveIndex;
        sponge = currentSpongeIndex;
        radioOwned = radioPurchased;
    }


    public void ApplySaveState(int soap, int glove, int sponge, bool radioOwned)
    {
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (employeeManager == null) employeeManager = FindFirstObjectByType<EmployeeManager>();

        currentSoapIndex = Mathf.Clamp(soap, 0, soapTiers.Count - 1);
        currentGloveIndex = Mathf.Clamp(glove, 0, gloveTiers.Count - 1);
        currentSpongeIndex = Mathf.Clamp(sponge, 0, spongeTiers.Count - 1);

        radioPurchased = radioOwned;

        UpdateSoapMenuUI();
        UpdateGloveMenuUI();
        UpdateSpongeMenuUI();
        UpdateRadioMenuUI();

        if (radioPurchased)
        {
            // Make sure purchase UI never shows again
            CloseRadioMenu();

            // Ensure radio playback and ambient shutdown are restored after load
            try
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.DisableAmbientLooping();
                }

                var radioController = FindFirstObjectByType<RadioCOntroller>();
                if (radioController != null)
                {
                    radioController.StartRadio();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Upgrades] Failed to restore radio state on load: {ex.Message}");
            }
        }

        score_manager_assign_active_dish();

        var allClickers = FindObjectsByType<DishClicker>(FindObjectsSortMode.None);
        for (int i = 0; i < allClickers.Length; i++)
            if (allClickers[i] != null)
                allClickers[i].upgrades = this;
    }

}