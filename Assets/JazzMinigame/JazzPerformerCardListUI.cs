using System.Collections.Generic;
using UnityEngine;

public class JazzPerformerCardListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JazzBandBookingManager bookingManager;

    [Tooltip("Prefab with JazzPerformerCardUI attached.")]
    [SerializeField] private JazzPerformerCardUI cardPrefab;

    [Tooltip("Parent object where performer cards will be spawned.")]
    [SerializeField] private Transform cardParent;

    [Header("Options")]
    [Tooltip("If true, cards are rebuilt when this object enables.")]
    [SerializeField] private bool buildOnEnable = true;

    [Tooltip("If true, existing spawned cards are destroyed before rebuilding.")]
    [SerializeField] private bool clearBeforeBuild = true;

    [Header("Optional Role Filter")]
    [Tooltip("If false, shows every performer in the manager's available performer list.")]
    [SerializeField] private bool useRoleFilter = false;

    [SerializeField] private PerformerRole roleFilter;

    private readonly List<JazzPerformerCardUI> spawnedCards = new List<JazzPerformerCardUI>();

    private void OnEnable()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (buildOnEnable)
            BuildCards();
    }

    [ContextMenu("Build Performer Cards")]
    public void BuildCards()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
        {
            Debug.LogWarning("[JazzPerformerCardListUI] Missing JazzBandBookingManager.");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogWarning("[JazzPerformerCardListUI] Missing card prefab.");
            return;
        }

        if (cardParent == null)
        {
            Debug.LogWarning("[JazzPerformerCardListUI] Missing card parent.");
            return;
        }

        if (clearBeforeBuild)
            ClearCards();

        List<PerformerData> performers = bookingManager.availablePerformers;

        if (performers == null || performers.Count == 0)
        {
            Debug.LogWarning("[JazzPerformerCardListUI] No available performers assigned to booking manager.");
            return;
        }

        for (int i = 0; i < performers.Count; i++)
        {
            PerformerData performer = performers[i];

            if (performer == null)
                continue;

            if (useRoleFilter && performer.role != roleFilter)
                continue;

            JazzPerformerCardUI card = Instantiate(cardPrefab, cardParent);
            card.Initialize(performer, bookingManager);

            spawnedCards.Add(card);
        }
    }

    [ContextMenu("Clear Performer Cards")]
    public void ClearCards()
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            if (spawnedCards[i] != null)
                DestroyImmediateSafe(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();

        if (cardParent == null)
            return;

        for (int i = cardParent.childCount - 1; i >= 0; i--)
        {
            Transform child = cardParent.GetChild(i);

            if (child != null)
                DestroyImmediateSafe(child.gameObject);
        }
    }

    public void RefreshCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                spawnedCards[i].Refresh();
        }
    }

    private void DestroyImmediateSafe(GameObject obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}