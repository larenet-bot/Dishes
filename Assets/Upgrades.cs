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

    // New: PiggyBank tier — separate from soap, grants additive offline cap seconds.
    [Serializable]
    public class PiggyBankTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;
        public float cost;
        [Tooltip("Additional offline earnings cap in seconds granted by this tier (additive).")]
        public int offlineCapSeconds = 0;
        public Sprite icon;
        [Tooltip("Number of total completed dishes required to unlock this tier.")]
        public int requiredDishes = 0;
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

    [Header("Piggy Bank Tiers")]
    public List<PiggyBankTier> piggyBankTiers = new List<PiggyBankTier>();

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

    // Track current selected tiers
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
    }

    private void Start()
    {
        UpdateSoapMenuUI();
        UpdateGloveMenuUI();
        UpdateSpongeMenuUI();
        UpdateRadioMenuUI();
    }

    private void Update()
    {
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
    }

    private void SeedDefaultTiers()
    {
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

        // Seed Piggy Bank tiers (separate from soap)
        if (piggyBankTiers.Count == 0)
        {
            piggyBankTiers.Add(new PiggyBankTier
            {
                tierName = "Copper Piggy",
                description = "A small piggy bank. Slightly increases offline cap.",
                cost = 200f,
                offlineCapSeconds = 3600, // +1 hour
                icon = null,
                requiredDishes = 0
            });

            piggyBankTiers.Add(new PiggyBankTier
            {
                tierName = "Silver Piggy",
                description = "A sturdier piggy. Grants a bigger offline cap.",
                cost = 1200f,
                offlineCapSeconds = 7200, // +2 hours
                icon = null,
                requiredDishes = 10
            });

            piggyBankTiers.Add(new PiggyBankTier
            {
                tierName = "Golden Piggy",
                description = "Large piggy bank. Significantly increases offline cap.",
                cost = 5000f,
                offlineCapSeconds = 14400, // +4 hours
                icon = null,
                requiredDishes = 100
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

    private void WireButtons()
    {
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
            });

            if (backgroundOverlayButton.gameObject.activeSelf)
                backgroundOverlayButton.gameObject.SetActive(false);
        }
    }

    // Returns the total offline-cap bonus (seconds) granted by currently applied piggy bank tiers.
    public int GetPiggyBankBonusSeconds()
    {
        if (piggyBankTiers == null || piggyBankTiers.Count == 0)
            return 0;

        int total = 0;
        int index = Mathf.Clamp(currentPiggyBankIndex, 0, piggyBankTiers.Count - 1);

        for (int i = 0; i <= index; i++)
        {
            if (piggyBankTiers[i] != null)
                total += piggyBankTiers[i].offlineCapSeconds;
        }

        return total;
    }

    public void GetSaveState(out int soap, out int glove, out int sponge, out int piggy, out bool radioOwned)
    {
        soap = currentSoapIndex;
        glove = currentGloveIndex;
        sponge = currentSpongeIndex;
        piggy = currentPiggyBankIndex;
        radioOwned = radioPurchased;
    }

    public void GetSaveState(out int soap, out int glove, out int sponge, out int piggy)
    {
        soap = currentSoapIndex;
        glove = currentGloveIndex;
        sponge = currentSpongeIndex;
        piggy = currentPiggyBankIndex;
    }

    public void GetSaveState(out int soap, out int glove, out int sponge)
    {
        soap = currentSoapIndex;
        glove = currentGloveIndex;
        sponge = currentSpongeIndex;
    }

    public void SetRadioOwnedForSave(bool owned)
    {
        radioPurchased = owned;
    }

    public bool GetRadioOwnedForSave()
    {
        return radioPurchased;
    }

    public void ApplySaveState(int soap, int glove, int sponge)
    {
        ApplySaveState(soap, glove, sponge, 0, radioPurchased);
    }

    public void ApplySaveState(int soap, int glove, int sponge, int piggy, bool radioOwned)
    {
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (employeeManager == null)
            employeeManager = FindAnyObjectByType<EmployeeManager>();

        currentSoapIndex = Mathf.Clamp(soap, 0, soapTiers.Count - 1);
        currentGloveIndex = Mathf.Clamp(glove, 0, gloveTiers.Count - 1);
        currentSpongeIndex = Mathf.Clamp(sponge, 0, spongeTiers.Count - 1);
        currentPiggyBankIndex = Mathf.Clamp(piggy, 0, Math.Max(0, piggyBankTiers.Count - 1));
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
            || (radioControlPanel != null && radioControlPanel.activeSelf);
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
        }

        return false;
    }
}