using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JazzChemistryEvent
{
    public string performerName;
    public string targetName;
    public PersonalityTag matchedTag;
    public int scoreChange;
    public string explanation;
}

[Serializable]
public class JazzPerformanceScoreResult
{
    public int rawTalentScore;
    public int chemistryBonus;
    public int chemistryPenalty;
    public int finalPerformanceScore;

    public bool hasCompleteBand;

    public List<PerformerRole> missingRoles = new List<PerformerRole>();
    public List<JazzChemistryEvent> chemistryEvents = new List<JazzChemistryEvent>();

    public int GetNetChemistry()
    {
        return chemistryBonus - chemistryPenalty;
    }
}

public static class JazzPerformanceScorer
{
    public const int LikeBonus = 5;
    public const int DislikePenalty = 8;

    public static JazzPerformanceScoreResult CalculateScore(List<PerformerData> selectedPerformers, bool logDebug = false)
    {
        JazzPerformanceScoreResult result = new JazzPerformanceScoreResult();

        if (selectedPerformers == null || selectedPerformers.Count == 0)
        {
            result.hasCompleteBand = false;
            AddAllRolesAsMissing(result);

            if (logDebug)
                Debug.Log("[JazzPerformanceScorer] No performers selected.");

            return result;
        }

        List<PerformerData> cleanPerformers = RemoveNullPerformers(selectedPerformers);

        CalculateRawTalent(cleanPerformers, result);
        CheckMissingRoles(cleanPerformers, result);
        CalculateChemistry(cleanPerformers, result);

        result.finalPerformanceScore =
            result.rawTalentScore +
            result.chemistryBonus -
            result.chemistryPenalty;

        if (result.finalPerformanceScore < 0)
            result.finalPerformanceScore = 0;

        if (logDebug)
            LogResult(cleanPerformers, result);

        return result;
    }

    private static List<PerformerData> RemoveNullPerformers(List<PerformerData> selectedPerformers)
    {
        List<PerformerData> cleanPerformers = new List<PerformerData>();

        for (int i = 0; i < selectedPerformers.Count; i++)
        {
            if (selectedPerformers[i] != null)
            {
                cleanPerformers.Add(selectedPerformers[i]);
            }
        }

        return cleanPerformers;
    }

    private static void CalculateRawTalent(List<PerformerData> performers, JazzPerformanceScoreResult result)
    {
        result.rawTalentScore = 0;

        for (int i = 0; i < performers.Count; i++)
        {
            result.rawTalentScore += Mathf.Clamp(performers[i].talentScore, 0, 100);
        }
    }

    private static void CheckMissingRoles(List<PerformerData> performers, JazzPerformanceScoreResult result)
    {
        result.missingRoles.Clear();

        if (!HasRole(performers, PerformerRole.Drummer))
            result.missingRoles.Add(PerformerRole.Drummer);

        if (!HasRole(performers, PerformerRole.Bassist))
            result.missingRoles.Add(PerformerRole.Bassist);

        if (!HasRole(performers, PerformerRole.Singer))
            result.missingRoles.Add(PerformerRole.Singer);

        if (!HasRole(performers, PerformerRole.SaxPlayer))
            result.missingRoles.Add(PerformerRole.SaxPlayer);

        result.hasCompleteBand = result.missingRoles.Count == 0;
    }

    private static bool HasRole(List<PerformerData> performers, PerformerRole role)
    {
        for (int i = 0; i < performers.Count; i++)
        {
            if (performers[i] != null && performers[i].role == role)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddAllRolesAsMissing(JazzPerformanceScoreResult result)
    {
        result.missingRoles.Clear();
        result.missingRoles.Add(PerformerRole.Drummer);
        result.missingRoles.Add(PerformerRole.Bassist);
        result.missingRoles.Add(PerformerRole.Singer);
        result.missingRoles.Add(PerformerRole.SaxPlayer);
    }

    private static void CalculateChemistry(List<PerformerData> performers, JazzPerformanceScoreResult result)
    {
        result.chemistryBonus = 0;
        result.chemistryPenalty = 0;
        result.chemistryEvents.Clear();

        for (int i = 0; i < performers.Count; i++)
        {
            PerformerData reactingPerformer = performers[i];

            if (reactingPerformer == null)
                continue;

            for (int j = 0; j < performers.Count; j++)
            {
                PerformerData targetPerformer = performers[j];

                if (targetPerformer == null)
                    continue;

                if (reactingPerformer == targetPerformer)
                    continue;

                ScoreReactionToTarget(reactingPerformer, targetPerformer, result);
            }
        }
    }

    private static void ScoreReactionToTarget(
        PerformerData reactingPerformer,
        PerformerData targetPerformer,
        JazzPerformanceScoreResult result)
    {
        if (targetPerformer.personalityTags == null)
            return;

        List<PersonalityTag> alreadyScoredTags = new List<PersonalityTag>();

        for (int i = 0; i < targetPerformer.personalityTags.Count; i++)
        {
            PersonalityTag targetTag = targetPerformer.personalityTags[i];

            if (alreadyScoredTags.Contains(targetTag))
                continue;

            alreadyScoredTags.Add(targetTag);

            bool dislikesTag =
                reactingPerformer.dislikesTags != null &&
                reactingPerformer.dislikesTags.Contains(targetTag);

            bool likesTag =
                reactingPerformer.likesTags != null &&
                reactingPerformer.likesTags.Contains(targetTag);

            // Dislikes win if a designer accidentally places the same tag in both lists.
            if (dislikesTag)
            {
                result.chemistryPenalty += DislikePenalty;

                result.chemistryEvents.Add(new JazzChemistryEvent
                {
                    performerName = reactingPerformer.performerName,
                    targetName = targetPerformer.performerName,
                    matchedTag = targetTag,
                    scoreChange = -DislikePenalty,
                    explanation = $"{reactingPerformer.performerName} clashes with {targetPerformer.performerName}'s {targetTag} side."
                });

                continue;
            }

            if (likesTag)
            {
                result.chemistryBonus += LikeBonus;

                result.chemistryEvents.Add(new JazzChemistryEvent
                {
                    performerName = reactingPerformer.performerName,
                    targetName = targetPerformer.performerName,
                    matchedTag = targetTag,
                    scoreChange = LikeBonus,
                    explanation = $"{reactingPerformer.performerName} works well with {targetPerformer.performerName}'s {targetTag} side."
                });
            }
        }
    }

    private static void LogResult(List<PerformerData> performers, JazzPerformanceScoreResult result)
    {
        Debug.Log("========== Jazz Performance Score ==========");

        Debug.Log("Selected Band:");

        for (int i = 0; i < performers.Count; i++)
        {
            PerformerData performer = performers[i];

            if (performer == null)
                continue;

            Debug.Log(
                $"{performer.GetRoleDisplayName()}: {performer.performerName} | Talent: {performer.talentScore} | Stars: {performer.GetStarRatingText()}"
            );
        }

        if (!result.hasCompleteBand)
        {
            string missingText = "";

            for (int i = 0; i < result.missingRoles.Count; i++)
            {
                missingText += result.missingRoles[i].ToString();

                if (i < result.missingRoles.Count - 1)
                    missingText += ", ";
            }

            Debug.LogWarning($"Missing roles: {missingText}");
        }

        Debug.Log($"Raw Talent: {result.rawTalentScore}");

        for (int i = 0; i < result.chemistryEvents.Count; i++)
        {
            JazzChemistryEvent chemistryEvent = result.chemistryEvents[i];

            string sign = chemistryEvent.scoreChange >= 0 ? "+" : "";

            Debug.Log($"{sign}{chemistryEvent.scoreChange}: {chemistryEvent.explanation}");
        }

        Debug.Log($"Chemistry Bonus: +{result.chemistryBonus}");
        Debug.Log($"Chemistry Penalty: -{result.chemistryPenalty}");
        Debug.Log($"Net Chemistry: {result.GetNetChemistry()}");
        Debug.Log($"Final Performance Score: {result.finalPerformanceScore}");

        Debug.Log("============================================");
    }
}