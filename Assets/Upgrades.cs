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
        public float cost;
        [Tooltip("How many dish stages are completed per click (e.g. 1 = normal, 2 = double, etc.)")]
        public int stagesPerClick = 1;
        public Sprite icon;
        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
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

    [Header("Optional: full-screen transparent Button behind the panel")]
    [Tooltip("If set, clicking this Button will close the soap/glove menu. If not set, the script will try to detect clicks outside the panel via UI raycast.")]
    public Button backgroundOverlayButton;

    private int currentSoapIndex = 0;
    private int currentGloveIndex = 0;
    private int currentSpongeIndex = 0;
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
            });
            // ensure overlay is hidden initially
            if (backgroundOverlayButton.gameObject.activeSelf)
                backgroundOverlayButton.gameObject.SetActive(false);
        }

        CloseSoapMenu();
        CloseGloveMenu();
        CloseSpongeMenu();
    }

    private void Start()
    {
        // ensure starting tiers are visible/known
        currentSoapIndex = 0;
        currentGloveIndex = 0;
        currentSpongeIndex = 0;
        UpdateSoapMenuUI(); // ensure HUD icon matches initial soap tier
        UpdateGloveMenuUI(); // ensure HUD icon matches initial glove tier
        UpdateSpongeMenuUI(); // ensure HUD icon matches initial sponge tier
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
        if (soapDescText) soapDescText.text = current.description;

        // update HUD button image to reflect current tier (if assigned)
        if (soapButtonImage != null && current != null && current.icon != null)
        {
            soapButtonImage.sprite = current.icon;
            // soapButtonImage.SetNativeSize(); // optional
        }

        bool hasNext = currentSoapIndex < soapTiers.Count - 1;
        if (hasNext)
        {
            var next = soapTiers[currentSoapIndex + 1];
            if (soapCostText) soapCostText.text = $"Upgrade for ${next.cost:0.00}";
            if (soapUpgradeButton) soapUpgradeButton.interactable = scoreManager != null && scoreManager.GetTotalProfit() >= next.cost;
            if (soapUpgradeButton) soapUpgradeButton.GetComponentInChildren<TMP_Text>()?.SetText($"Upgrade for ${next.cost:0.00}");
        }
        else
        {
            if (soapCostText) soapCostText.text = "MAX";
            if (soapUpgradeButton) soapUpgradeButton.interactable = false;
            if (soapUpgradeButton) soapUpgradeButton.GetComponentInChildren<TMP_Text>()?.SetText("Max");
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
        if (gloveDescText) gloveDescText.text = current.description;

        // update HUD button image to reflect current glove tier (if assigned)
        if (gloveButtonImage != null && current != null && current.icon != null)
        {
            gloveButtonImage.sprite = current.icon;
            // gloveButtonImage.SetNativeSize(); // optional
        }

        bool hasNext = currentGloveIndex < gloveTiers.Count - 1;
        if (hasNext)
        {
            var next = gloveTiers[currentGloveIndex + 1];

            // check milestone unlock using ScoreManager total dishes
            bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

            if (gloveCostText)
                gloveCostText.text = unlocked ? $"Upgrade for ${next.cost:0.00}" : $"Locked: Complete {next.requiredDishes} dishes";

            if (gloveUpgradeButton)
            {
                gloveUpgradeButton.interactable = unlocked && scoreManager != null && scoreManager.GetTotalProfit() >= next.cost;
                var btnText = gloveUpgradeButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                    btnText.SetText(unlocked ? $"Upgrade for ${next.cost:0.00}" : $"Locked: {next.requiredDishes} dishes");
            }
        }
        else
        {
            if (gloveCostText) gloveCostText.text = "MAX";
            if (gloveUpgradeButton) gloveUpgradeButton.interactable = false;
            if (gloveUpgradeButton) gloveUpgradeButton.GetComponentInChildren<TMP_Text>()?.SetText("Max");
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
        if (spongeDescText) spongeDescText.text = current.description;
        // update HUD button image to reflect current tier (if assigned)
        if (spongeButtonImage != null && current != null && current.icon != null)
        {
            spongeButtonImage.sprite = current.icon;
            // spongeButtonImage.SetNativeSize(); // optional
        }
        bool hasNext = currentSpongeIndex < spongeTiers.Count - 1;
        if (hasNext)
        {
            var next = spongeTiers[currentSpongeIndex + 1];

            // check milestone unlock using ScoreManager total dishes
            bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

            if (spongeCostText)
                spongeCostText.text = unlocked ? $"Upgrade for ${next.cost:0.00}" : $"Locked: Complete {next.requiredDishes} dishes";

            if (spongeUpgradeButton)
            {
                spongeUpgradeButton.interactable = unlocked && scoreManager != null && scoreManager.GetTotalProfit() >= next.cost;
                var btnText = spongeUpgradeButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                    btnText.SetText(unlocked ? $"Upgrade for ${next.cost:0.00}" : $"Locked: {next.requiredDishes} dishes");
            }
        }
        else
        {
            if (spongeCostText) spongeCostText.text = "MAX";
            if (spongeUpgradeButton) spongeUpgradeButton.interactable = false;
            if (spongeUpgradeButton) spongeUpgradeButton.GetComponentInChildren<TMP_Text>()?.SetText("Max");
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

    private void Update()
    {
        // keep the upgrade interactable state up to date while menus are open
        if (soapMenuPanel != null && soapMenuPanel.activeSelf)
            UpdateSoapMenuUI();
        if (gloveMenuPanel != null && gloveMenuPanel.activeSelf)
            UpdateGloveMenuUI();
        if (spongeMenuPanel != null && spongeMenuPanel.activeSelf)
            UpdateSpongeMenuUI();

        // if no explicit overlay Button configured, detect clicks outside the active panel via UI raycast
        if ((soapMenuPanel != null && soapMenuPanel.activeSelf || gloveMenuPanel != null && gloveMenuPanel.activeSelf || spongeMenuPanel != null && spongeMenuPanel.activeSelf) && backgroundOverlayButton == null)
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

                if (!clickedInsideAnyPanel)
                {
                    CloseSoapMenu();
                    CloseGloveMenu();
                    CloseSpongeMenu();
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
}