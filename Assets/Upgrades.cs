using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        public Sprite icon;
    }

    [Serializable]
    public class SoapTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("Multiplier to apply to existing profit values, e.g. 1.5 = +50%.")]
        public float multiplier = 1f;
        public Sprite icon;
    }

    [Serializable]
    public class GloveTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("How many additional dishes are added to the dish completion count when this tier is active, relative to base.")]
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
        [Tooltip("How many dish stages are completed per click.")]
        public int stagesPerClick = 1;
        public Sprite icon;
        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
    }
    [Serializable]
    public class PiggyBankTier
    {
        public string tierName;

        [TextArea]
        public string description;

        [TextArea]
        public string loreDescription;

        public float cost;

        [Tooltip("Offline earnings seconds cap provided by this tier. 0 means this tier grants NO offline earnings. Values are in seconds.")]
        public float value = 0f;

        public Sprite icon;

        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
    }

    [Header("Piggy Bank Tiers")]
    public List<PiggyBankTier> piggyBankTiers = new List<PiggyBankTier>();

    [Header("Piggy Bank UI")]
    public GameObject piggyBankMenuPanel;
    public TMP_Text piggyBankNameText;
    public TMP_Text piggyBankDescText;
    public TMP_Text piggyBankCostText;
    public Button piggyBankUpgradeButton;
    public Button piggyBankCloseButton;

    [Header("HUD Button Image")]
    public Image piggyBankButtonImage;

    [Header("Radio Tiers")]
    public List<RadioTier> radioTiers = new List<RadioTier>();

    [Header("Radio UI")]
    public GameObject radioMenuPanel;
    public TMP_Text radioNameText;
    public TMP_Text radioDescText;
    public TMP_Text radioCostText;
    public Button radioUpgradeButton;
    public Button radioCloseButton;


    [Header("HUD Button Image")]
    public Image RadioButtonImage;

    [Header("Radio Control Panel")]
    [SerializeField] private GameObject radioControlPanel;

    [Header("Soap Tiers")]
    public List<SoapTier> soapTiers = new List<SoapTier>();

    [Header("Soap UI")]
    public GameObject soapMenuPanel;
    public TMP_Text soapNameText;
    public TMP_Text soapDescText;
    public TMP_Text soapCostText;
    public Button soapUpgradeButton;
    public Button soapCloseButton;

    [Header("HUD Button Image")]
    public Image soapButtonImage;

    [Header("Glove Tiers")]
    public List<GloveTier> gloveTiers = new List<GloveTier>();

    [Header("Glove UI")]
    public GameObject gloveMenuPanel;
    public TMP_Text gloveNameText;
    public TMP_Text gloveDescText;
    public TMP_Text gloveCostText;
    public Button gloveUpgradeButton;
    public Button gloveCloseButton;

    [Header("HUD Button Image")]
    public Image gloveButtonImage;

    [Header("Sponge Tiers")]
    public List<SpongeTier> spongeTiers = new List<SpongeTier>();

    [Header("Sponge UI")]
    public GameObject spongeMenuPanel;
    public TMP_Text spongeNameText;
    public TMP_Text spongeDescText;
    public TMP_Text spongeCostText;
    public Button spongeUpgradeButton;
    public Button spongeCloseButton;

    [Header("HUD Button Image")]
    public Image spongeButtonImage;

    [Header("Optional Full-Screen Transparent Button Behind Panel")]
    public Button backgroundOverlayButton;

    [Header("Optional Canvas For Click Outside Detection")]
    public Canvas raycastCanvas;

    private int currentSoapIndex = 0;
    private int currentGloveIndex = 0;
    private int currentSpongeIndex = 0;
    private int currentRadioIndex = 0;
    private int currentPiggyBankIndex = 0;

    private EmployeeManager employeeManager;
    private ScoreManager scoreManager;
    private GraphicRaycaster graphicRaycaster;
    private bool radioPurchased = false;

    public bool RadioPurchased
    {
        get { return radioPurchased; }
        set { radioPurchased = value; }
    }

    private void Reset()
    {
        scoreManager = FindAnyObjectByType<ScoreManager>();
        employeeManager = FindAnyObjectByType<EmployeeManager>();
    }

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (employeeManager == null)
            employeeManager = FindAnyObjectByType<EmployeeManager>();

        if (raycastCanvas == null)
            raycastCanvas = FindAnyObjectByType<Canvas>();

        if (raycastCanvas != null)
            graphicRaycaster = raycastCanvas.GetComponent<GraphicRaycaster>();

        if (graphicRaycaster == null && raycastCanvas != null)
            graphicRaycaster = raycastCanvas.gameObject.AddComponent<GraphicRaycaster>();

        SeedDefaultTiers();
        WireButtons();

        CloseSoapMenu();
        CloseGloveMenu();
        CloseSpongeMenu();
        CloseRadioMenu();
        CloseRadioControlPanel();
        ClosePiggyBankMenu();
    }

    private void Start()
    {
        UpdateSoapMenuUI();
        UpdateGloveMenuUI();
        UpdateSpongeMenuUI();
        UpdateRadioMenuUI();
        UpdatePiggyBankMenuUI();
    }

    private void Update()
    {
        if (piggyBankMenuPanel != null && piggyBankMenuPanel.activeSelf)
            UpdatePiggyBankMenuUI();
        if (soapMenuPanel != null && soapMenuPanel.activeSelf)
            UpdateSoapMenuUI();

        if (gloveMenuPanel != null && gloveMenuPanel.activeSelf)
            UpdateGloveMenuUI();

        if (spongeMenuPanel != null && spongeMenuPanel.activeSelf)
            UpdateSpongeMenuUI();

        if (radioMenuPanel != null && radioMenuPanel.activeSelf)
            UpdateRadioMenuUI();

        if (!AnyUpgradePanelOpen() || backgroundOverlayButton != null)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current == null || graphicRaycaster == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        if (ClickedInsideOpenPanel(results))
            return;

        CloseSoapMenu();
        CloseGloveMenu();
        CloseSpongeMenu();
        CloseRadioMenu();
        CloseRadioControlPanel();
        ClosePiggyBankMenu();
    }

    private void SeedDefaultTiers()
    {
        if (piggyBankTiers.Count == 0)
        {
            // default tier: no offline earnings
            piggyBankTiers.Add(new PiggyBankTier
            {
                tierName = "Piggy Bank",
                description = "Stores spare change.",
                loreDescription = "A simple piggy bank.",
                cost = 0f,
                value = 0f, // NO offline earnings by default
                icon = null,
                requiredDishes = 0
            });

            // Each upgrade increases the cap by 2 hours (7200 seconds)
            piggyBankTiers.Add(new PiggyBankTier
            {
                tierName = "Ceramic Piggy Bank",
                description = "A sturdier piggy bank.",
                loreDescription = "Allows 2 hours of offline earnings.",
                cost = 100f,
                value = 7200f, // 2 hours
                icon = null,
                requiredDishes = 10
            });

            piggyBankTiers.Add(new PiggyBankTier
            {
                tierName = "Golden Piggy Bank",
                description = "A luxurious piggy bank.",
                loreDescription = "Allows 4 hours of offline earnings.",
                cost = 1000f,
                value = 14400f, // 4 hours
                icon = null,
                requiredDishes = 100
            });
        }
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
                description = "Further increases dish profit and employee income.",
                cost = 500f,
                multiplier = 2f,
                icon = null
            });

            soapTiers.Add(new SoapTier
            {
                tierName = "Industrial Degreaser",
                description = "Massive boost to all profit generation.",
                cost = 2000f,
                multiplier = 3f,
                icon = null
            });
        }

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
                requiredDishes = 10
            });

            gloveTiers.Add(new GloveTier
            {
                tierName = "Kevlar Gloves",
                description = "Kevlar gloves: +2 dishes per completed cycle.",
                cost = 250f,
                dishesAdded = 2,
                icon = null,
                requiredDishes = 100
            });
        }

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
                requiredDishes = 10
            });

            spongeTiers.Add(new SpongeTier
            {
                tierName = "Steel Wool",
                description = "Completes 3 stages per click.",
                cost = 600f,
                stagesPerClick = 3,
                icon = null,
                requiredDishes = 100
            });
        }
    }
    public void OpenPiggyBankMenu()
    {
        if (piggyBankMenuPanel == null) return;

        UpdatePiggyBankMenuUI();

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        piggyBankMenuPanel.SetActive(true);
    }

    public void ClosePiggyBankMenu()
    {
        if (piggyBankMenuPanel == null) return;

        piggyBankMenuPanel.SetActive(false);

        if (backgroundOverlayButton != null && !AnyUpgradePanelOpen())
            backgroundOverlayButton.gameObject.SetActive(false);
    }
    private void WireButtons()
    {
        if (piggyBankUpgradeButton != null)
        {
            piggyBankUpgradeButton.onClick.RemoveAllListeners();
            piggyBankUpgradeButton.onClick.AddListener(OnPiggyBankUpgradeButton);
        }

        if (piggyBankCloseButton != null)
        {
            piggyBankCloseButton.onClick.RemoveAllListeners();
            piggyBankCloseButton.onClick.AddListener(ClosePiggyBankMenu);
        }
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

        if (backgroundOverlayButton != null)
        {
            backgroundOverlayButton.onClick.RemoveAllListeners();
            backgroundOverlayButton.onClick.AddListener(() =>
            {
                CloseSoapMenu();
                CloseGloveMenu();
                CloseSpongeMenu();
                CloseRadioMenu();
                CloseRadioControlPanel();
                ClosePiggyBankMenu();
            });

            if (backgroundOverlayButton.gameObject.activeSelf)
                backgroundOverlayButton.gameObject.SetActive(false);
        }
    }

    public void OpenSoapMenu()
    {
        if (soapMenuPanel == null) return;

        UpdateSoapMenuUI();

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        soapMenuPanel.SetActive(true);
    }

    public void CloseSoapMenu()
    {
        if (soapMenuPanel == null) return;

        soapMenuPanel.SetActive(false);

        if (backgroundOverlayButton != null && !AnyUpgradePanelOpen())
            backgroundOverlayButton.gameObject.SetActive(false);
    }

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

        if (backgroundOverlayButton != null && !AnyUpgradePanelOpen())
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    public void OpenSpongeMenu()
    {
        if (spongeMenuPanel == null) return;

        UpdateSpongeMenuUI();

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        spongeMenuPanel.SetActive(true);
    }

    public void CloseSpongeMenu()
    {
        if (spongeMenuPanel == null) return;

        spongeMenuPanel.SetActive(false);

        if (backgroundOverlayButton != null && !AnyUpgradePanelOpen())
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    public void OpenRadioMenu()
    {
        if (radioMenuPanel == null) return;

        if (!radioPurchased)
        {
            UpdateRadioMenuUI();

            if (backgroundOverlayButton != null)
                backgroundOverlayButton.gameObject.SetActive(true);

            radioMenuPanel.SetActive(true);
            return;
        }

        OpenRadioControlPanel();
    }

    public void CloseRadioMenu()
    {
        if (radioMenuPanel == null) return;

        radioMenuPanel.SetActive(false);

        if (backgroundOverlayButton != null && !AnyUpgradePanelOpen())
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void OpenRadioControlPanel()
    {
        if (radioControlPanel == null)
        {
            Debug.LogWarning("[Upgrades] Radio control panel not assigned.");
            return;
        }

        if (radioMenuPanel != null)
            radioMenuPanel.SetActive(false);

        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        radioControlPanel.SetActive(true);
    }

    public void CloseRadioControlPanel()
    {
        if (radioControlPanel != null)
            radioControlPanel.SetActive(false);

        if (backgroundOverlayButton != null && !AnyUpgradePanelOpen())
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    private void UpdateSoapMenuUI()
    {
        if (soapTiers == null || soapTiers.Count == 0) return;

        SoapTier current = soapTiers[Mathf.Clamp(currentSoapIndex, 0, soapTiers.Count - 1)];

        if (soapNameText != null)
            soapNameText.text = current.tierName;

        if (soapDescText != null)
            soapDescText.text = current.description;

        if (soapCostText != null)
            soapCostText.text = string.IsNullOrEmpty(current.loreDescription) ? string.Empty : current.loreDescription;

        if (soapButtonImage != null && current.icon != null)
            soapButtonImage.sprite = current.icon;

        bool hasNext = currentSoapIndex < soapTiers.Count - 1;

        if (soapUpgradeButton == null) return;

        TMP_Text btnText = soapUpgradeButton.GetComponentInChildren<TMP_Text>();

        if (hasNext)
        {
            SoapTier next = soapTiers[currentSoapIndex + 1];
            float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;

            soapUpgradeButton.interactable = scoreManager != null && wallet >= next.cost;

            if (btnText != null)
                btnText.SetText($"Upgrade for {BigNumberFormatter.FormatMoney((double)next.cost)}");
        }
        else
        {
            soapUpgradeButton.interactable = false;

            if (btnText != null)
                btnText.SetText("Max");
        }
    }

    private void UpdateGloveMenuUI()
    {
        if (gloveTiers == null || gloveTiers.Count == 0) return;

        GloveTier current = gloveTiers[Mathf.Clamp(currentGloveIndex, 0, gloveTiers.Count - 1)];

        if (gloveNameText != null)
            gloveNameText.text = current.tierName;

        if (gloveDescText != null)
            gloveDescText.text = current.description;

        if (gloveCostText != null)
            gloveCostText.text = string.IsNullOrEmpty(current.loreDescription) ? string.Empty : current.loreDescription;

        if (gloveButtonImage != null && current.icon != null)
            gloveButtonImage.sprite = current.icon;

        bool hasNext = currentGloveIndex < gloveTiers.Count - 1;

        if (gloveUpgradeButton == null) return;

        TMP_Text btnText = gloveUpgradeButton.GetComponentInChildren<TMP_Text>();

        if (hasNext)
        {
            GloveTier next = gloveTiers[currentGloveIndex + 1];
            float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;
            bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

            gloveUpgradeButton.interactable = unlocked && wallet >= next.cost;

            if (btnText != null)
            {
                btnText.SetText(unlocked
                    ? $"Upgrade for {BigNumberFormatter.FormatMoney((double)next.cost)}"
                    : $"Locked: {BigNumberFormatter.FormatNumber(next.requiredDishes)} dishes");
            }
        }
        else
        {
            gloveUpgradeButton.interactable = false;

            if (btnText != null)
                btnText.SetText("Max");
        }
    }
    private void UpdatePiggyBankMenuUI()
    {
        if (piggyBankTiers == null || piggyBankTiers.Count == 0)
            return;

        PiggyBankTier current =
            piggyBankTiers[Mathf.Clamp(currentPiggyBankIndex, 0, piggyBankTiers.Count - 1)];

        if (piggyBankNameText != null)
            piggyBankNameText.text = current.tierName;

        if (piggyBankDescText != null)
            piggyBankDescText.text = current.description;

        if (piggyBankCostText != null)
            piggyBankCostText.text =
                string.IsNullOrEmpty(current.loreDescription)
                ? string.Empty
                : current.loreDescription;

        if (piggyBankButtonImage != null && current.icon != null)
            piggyBankButtonImage.sprite = current.icon;

        bool hasNext = currentPiggyBankIndex < piggyBankTiers.Count - 1;

        if (piggyBankUpgradeButton == null)
            return;

        TMP_Text btnText =
            piggyBankUpgradeButton.GetComponentInChildren<TMP_Text>();

        if (hasNext)
        {
            PiggyBankTier next = piggyBankTiers[currentPiggyBankIndex + 1];

            float wallet =
                scoreManager != null ? scoreManager.GetTotalProfit() : 0f;

            bool unlocked =
                scoreManager != null &&
                scoreManager.GetTotalDishes() >= next.requiredDishes;

            piggyBankUpgradeButton.interactable =
                unlocked && wallet >= next.cost;

            if (btnText != null)
            {
                btnText.SetText(unlocked
                    ? $"Upgrade for {BigNumberFormatter.FormatMoney((double)next.cost)}"
                    : $"Locked: {BigNumberFormatter.FormatNumber(next.requiredDishes)} dishes");
            }
        }
        else
        {
            piggyBankUpgradeButton.interactable = false;

            if (btnText != null)
                btnText.SetText("Max");
        }
    }
    private void OnPiggyBankUpgradeButton()
    {
        if (currentPiggyBankIndex >= piggyBankTiers.Count - 1)
            return;

        PiggyBankTier next =
            piggyBankTiers[currentPiggyBankIndex + 1];

        if (scoreManager == null)
            return;

        if (scoreManager.GetTotalDishes() < next.requiredDishes)
            return;

        float wallet = scoreManager.GetTotalProfit();

        if (wallet < next.cost)
            return;

        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        currentPiggyBankIndex++;

        UpdatePiggyBankMenuUI();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName}");
    }
    private void UpdateSpongeMenuUI()
    {
        if (spongeTiers == null || spongeTiers.Count == 0) return;

        SpongeTier current = spongeTiers[Mathf.Clamp(currentSpongeIndex, 0, spongeTiers.Count - 1)];

        if (spongeNameText != null)
            spongeNameText.text = current.tierName;

        if (spongeDescText != null)
            spongeDescText.text = current.description;

        if (spongeCostText != null)
            spongeCostText.text = string.IsNullOrEmpty(current.loreDescription) ? string.Empty : current.loreDescription;

        if (spongeButtonImage != null && current.icon != null)
            spongeButtonImage.sprite = current.icon;

        bool hasNext = currentSpongeIndex < spongeTiers.Count - 1;

        if (spongeUpgradeButton == null) return;

        TMP_Text btnText = spongeUpgradeButton.GetComponentInChildren<TMP_Text>();

        if (hasNext)
        {
            SpongeTier next = spongeTiers[currentSpongeIndex + 1];
            float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;
            bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

            spongeUpgradeButton.interactable = unlocked && wallet >= next.cost;

            if (btnText != null)
            {
                btnText.SetText(unlocked
                    ? $"Upgrade for {BigNumberFormatter.FormatMoney((double)next.cost)}"
                    : $"Locked: {BigNumberFormatter.FormatNumber(next.requiredDishes)} dishes");
            }
        }
        else
        {
            spongeUpgradeButton.interactable = false;

            if (btnText != null)
                btnText.SetText("Max");
        }
    }

    public void UpdateRadioMenuUI()
    {
        if (radioTiers == null || radioTiers.Count == 0) return;

        RadioTier current = radioTiers[Mathf.Clamp(currentRadioIndex, 0, radioTiers.Count - 1)];

        if (radioNameText != null)
            radioNameText.text = current.tierName;

        if (radioDescText != null)
            radioDescText.text = current.description;

        if (radioCostText != null)
            radioCostText.text = string.IsNullOrEmpty(current.loreDescription) ? string.Empty : current.loreDescription;

        if (RadioButtonImage != null && current.icon != null)
            RadioButtonImage.sprite = current.icon;

        if (radioUpgradeButton == null) return;

        TMP_Text btnText = radioUpgradeButton.GetComponentInChildren<TMP_Text>();

        bool hasNext = currentRadioIndex < radioTiers.Count - 1;
        float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;

        if (hasNext)
        {
            RadioTier next = radioTiers[currentRadioIndex + 1];
            bool unlocked = scoreManager != null && scoreManager.GetTotalDishes() >= next.requiredDishes;

            radioUpgradeButton.interactable = unlocked && wallet >= next.cost;

            if (btnText != null)
            {
                btnText.SetText(unlocked
                    ? $"Upgrade for {BigNumberFormatter.FormatMoney((double)next.cost)}"
                    : $"Locked: {BigNumberFormatter.FormatNumber(next.requiredDishes)} dishes");
            }

            return;
        }

        bool canBuyRadio = !radioPurchased && wallet >= current.cost;
        radioUpgradeButton.interactable = canBuyRadio;

        if (btnText != null)
            btnText.SetText(radioPurchased ? "Owned" : $"Buy {BigNumberFormatter.FormatMoney((double)current.cost)}");
    }

    private void OnSoapUpgradeButton()
    {
        if (currentSoapIndex >= soapTiers.Count - 1) return;

        SoapTier next = soapTiers[currentSoapIndex + 1];

        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();

        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName}.");
            return;
        }

        scoreManager.SubtractProfit(next.cost, isPurchase: true);
        scoreManager.MultiplyDishProfit(next.multiplier);

        if (employeeManager == null)
            employeeManager = FindAnyObjectByType<EmployeeManager>();

        if (employeeManager != null)
            employeeManager.MultiplyEmployeeProfit(next.multiplier);

        currentSoapIndex++;
        UpdateSoapMenuUI();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName}.");
    }

    private void OnGloveUpgradeButton()
    {
        if (currentGloveIndex >= gloveTiers.Count - 1) return;

        GloveTier next = gloveTiers[currentGloveIndex + 1];

        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        if (scoreManager.GetTotalDishes() < next.requiredDishes)
        {
            Debug.Log($"[Upgrades] {next.tierName} locked.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();

        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName}.");
            return;
        }

        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        int currentAdded = gloveTiers[Mathf.Clamp(currentGloveIndex, 0, gloveTiers.Count - 1)].dishesAdded;
        int delta = next.dishesAdded - currentAdded;

        if (delta > 0)
            scoreManager.IncreaseDishCountIncrement(delta);

        currentGloveIndex++;
        UpdateGloveMenuUI();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName}.");
    }

    private void OnSpongeUpgradeButton()
    {
        if (currentSpongeIndex >= spongeTiers.Count - 1) return;

        SpongeTier next = spongeTiers[currentSpongeIndex + 1];

        if (scoreManager == null)
        {
            Debug.LogWarning("[Upgrades] ScoreManager not found.");
            return;
        }

        if (scoreManager.GetTotalDishes() < next.requiredDishes)
        {
            Debug.Log($"[Upgrades] {next.tierName} locked.");
            return;
        }

        float wallet = scoreManager.GetTotalProfit();

        if (wallet < next.cost)
        {
            Debug.Log($"[Upgrades] Not enough profit to buy {next.tierName}.");
            return;
        }

        scoreManager.SubtractProfit(next.cost, isPurchase: true);

        currentSpongeIndex++;
        UpdateSpongeMenuUI();
        AssignThisUpgradeSourceToDishClickers();

        Debug.Log($"[Upgrades] Upgraded to {next.tierName}.");
    }

    private void OnRadioUpgradeButton()
    {
        if (radioPurchased) return;
        if (radioTiers == null || radioTiers.Count == 0) return;
        if (scoreManager == null) return;

        RadioTier radio = radioTiers[Mathf.Clamp(currentRadioIndex, 0, radioTiers.Count - 1)];

        float wallet = scoreManager.GetTotalProfit();

        if (wallet < radio.cost)
        {
            Debug.Log("[Upgrades] Not enough money to buy Radio.");
            return;
        }

        scoreManager.SubtractProfit(radio.cost, isPurchase: true);

        radioPurchased = true;

        UpdateRadioMenuUI();
        CloseRadioMenu();
        OpenRadioControlPanel();

        try
        {
            if (AudioManager.instance != null)
                AudioManager.instance.DisableAmbientLooping();

            var radioController = FindAnyObjectByType<RadioCOntroller>();

            if (radioController != null)
            {
                radioController.MarkPurchased();
                radioController.StartRadio();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Upgrades] Failed to start radio after purchase: {ex.Message}");
        }

        Debug.Log("[Upgrades] Radio purchased.");
    }

    public int GetCurrentStagesPerClick()
    {
        if (spongeTiers == null || spongeTiers.Count == 0)
            return 1;

        return spongeTiers[Mathf.Clamp(currentSpongeIndex, 0, spongeTiers.Count - 1)].stagesPerClick;
    }

    public void SetSoapTierIndex(int targetIndex, bool spend = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, soapTiers.Count - 1);

        if (targetIndex == currentSoapIndex)
        {
            UpdateSoapMenuUI();
            return;
        }

        int from = currentSoapIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;
        float cumulativeMultiplier = 1f;

        for (int i = from + 1; i <= to; i++)
        {
            totalCost += soapTiers[i].cost;
            cumulativeMultiplier *= soapTiers[i].multiplier;
        }

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost)
            return;

        if (spend && scoreManager != null)
            scoreManager.SubtractProfit(totalCost, isPurchase: true);

        if (scoreManager != null)
            scoreManager.MultiplyDishProfit(cumulativeMultiplier);

        if (employeeManager != null)
            employeeManager.MultiplyEmployeeProfit(cumulativeMultiplier);

        currentSoapIndex = to;
        UpdateSoapMenuUI();
    }

    public void SetGloveTierIndex(int targetIndex, bool spend = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, gloveTiers.Count - 1);

        if (targetIndex == currentGloveIndex)
        {
            UpdateGloveMenuUI();
            return;
        }

        int from = currentGloveIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;
        int addDishesDelta = gloveTiers[to].dishesAdded - gloveTiers[from].dishesAdded;

        if (spend && scoreManager != null && scoreManager.GetTotalDishes() < gloveTiers[to].requiredDishes)
            return;

        for (int i = from + 1; i <= to; i++)
            totalCost += gloveTiers[i].cost;

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost)
            return;

        if (spend && scoreManager != null)
            scoreManager.SubtractProfit(totalCost, isPurchase: true);

        if (addDishesDelta > 0 && scoreManager != null)
            scoreManager.IncreaseDishCountIncrement(addDishesDelta);

        currentGloveIndex = to;
        UpdateGloveMenuUI();
    }

    public void SetSpongeTierIndex(int targetIndex, bool spend = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, spongeTiers.Count - 1);

        if (targetIndex == currentSpongeIndex)
        {
            UpdateSpongeMenuUI();
            return;
        }

        int from = currentSpongeIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;

        if (spend && scoreManager != null && scoreManager.GetTotalDishes() < spongeTiers[to].requiredDishes)
            return;

        for (int i = from + 1; i <= to; i++)
            totalCost += spongeTiers[i].cost;

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost)
            return;

        if (spend && scoreManager != null)
            scoreManager.SubtractProfit(totalCost, isPurchase: true);

        currentSpongeIndex = to;
        UpdateSpongeMenuUI();
        AssignThisUpgradeSourceToDishClickers();
    }

    public void SetRadioTierIndex(int targetIndex, bool spend = false)
    {
        if (radioTiers == null || radioTiers.Count == 0)
            return;

        targetIndex = Mathf.Clamp(targetIndex, 0, radioTiers.Count - 1);

        if (targetIndex == currentRadioIndex)
        {
            UpdateRadioMenuUI();
            return;
        }

        int from = currentRadioIndex;
        int to = Mathf.Max(targetIndex, from);

        float totalCost = 0f;
        int addDishesDelta = radioTiers[to].dishesAdded - radioTiers[from].dishesAdded;

        if (spend && scoreManager != null && scoreManager.GetTotalDishes() < radioTiers[to].requiredDishes)
            return;

        for (int i = from + 1; i <= to; i++)
            totalCost += radioTiers[i].cost;

        if (spend && scoreManager != null && scoreManager.GetTotalProfit() < totalCost)
            return;

        if (spend && scoreManager != null)
            scoreManager.SubtractProfit(totalCost, isPurchase: true);

        if (addDishesDelta > 0 && scoreManager != null)
            scoreManager.IncreaseDishCountIncrement(addDishesDelta);

        currentRadioIndex = to;
        UpdateRadioMenuUI();
    }



    public void GetSaveState(
    out int soap,
    out int glove,
    out int sponge,
    out int piggyBank,
    out bool radioOwned)
    {
        soap = currentSoapIndex;
        glove = currentGloveIndex;
        sponge = currentSpongeIndex;
        piggyBank = currentPiggyBankIndex;
        radioOwned = radioPurchased;
    }

    public void SetRadioOwnedForSave(bool owned)
    {
        radioPurchased = owned;
    }

    public bool GetRadioOwnedForSave()
    {
        return radioPurchased;
    }

    public void ApplySaveState(int soap, int glove, int sponge, int piggyBank)
    {
        ApplySaveState(soap, glove, sponge, radioPurchased,piggyBank);
    }

    public void ApplySaveState(int soap, int glove, int sponge, bool radioOwned, int piggyBank)
    {
        currentPiggyBankIndex =
    Mathf.Clamp(piggyBank, 0, piggyBankTiers.Count - 1);
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (employeeManager == null)
            employeeManager = FindAnyObjectByType<EmployeeManager>();

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
            CloseRadioMenu();

            try
            {
                if (AudioManager.instance != null)
                    AudioManager.instance.DisableAmbientLooping();

                var radioController = FindAnyObjectByType<RadioCOntroller>();

                if (radioController != null)
                {
                    radioController.MarkPurchased();
                    radioController.StartRadio();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Upgrades] Failed to restore radio state on load: {ex.Message}");
            }
        }
        else
        {
            CloseRadioControlPanel();
        }

        AssignThisUpgradeSourceToDishClickers();
    }

    private void AssignThisUpgradeSourceToDishClickers()
    {
        try
        {
            if (scoreManager != null && scoreManager.activeDish != null)
                scoreManager.activeDish.upgrades = this;

            var allClickers = FindObjectsByType<DishClicker>(FindObjectsSortMode.None);

            for (int i = 0; i < allClickers.Length; i++)
            {
                if (allClickers[i] != null)
                    allClickers[i].upgrades = this;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Upgrades] Failed to apply sponge refs: {ex.Message}");
        }
    }

    private bool AnyUpgradePanelOpen()
    {
        return (soapMenuPanel != null && soapMenuPanel.activeSelf)
            || (gloveMenuPanel != null && gloveMenuPanel.activeSelf)
            || (spongeMenuPanel != null && spongeMenuPanel.activeSelf)
            || (radioMenuPanel != null && radioMenuPanel.activeSelf)
            || (radioControlPanel != null && radioControlPanel.activeSelf)
            || (piggyBankMenuPanel != null && piggyBankMenuPanel.activeSelf);
    }

    private bool ClickedInsideOpenPanel(List<RaycastResult> results)
    {
        if (results == null)
            return false;

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit = results[i].gameObject;

            if (hit == null)
                continue;

            if (soapMenuPanel != null && soapMenuPanel.activeSelf && hit.transform.IsChildOf(soapMenuPanel.transform))
                return true;

            if (gloveMenuPanel != null && gloveMenuPanel.activeSelf && hit.transform.IsChildOf(gloveMenuPanel.transform))
                return true;

            if (spongeMenuPanel != null && spongeMenuPanel.activeSelf && hit.transform.IsChildOf(spongeMenuPanel.transform))
                return true;

            if (radioMenuPanel != null && radioMenuPanel.activeSelf && hit.transform.IsChildOf(radioMenuPanel.transform))
                return true;

            if (radioControlPanel != null && radioControlPanel.activeSelf && hit.transform.IsChildOf(radioControlPanel.transform))
                return true;
            if (piggyBankMenuPanel != null &&
    piggyBankMenuPanel.activeSelf &&
    hit.transform.IsChildOf(piggyBankMenuPanel.transform))
                return true;
        }

        return false;
    }

}