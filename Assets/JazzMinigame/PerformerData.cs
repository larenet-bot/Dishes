using System.Collections.Generic;
using UnityEngine;

public enum PerformerRole
{
    Drummer,
    Bassist,
    Singer,
    SaxPlayer
}

public enum PersonalityTag
{
    Rookie,
    Nervous,
    Professional,
    Mentor,
    Diva,
    Loud,
    Improviser,
    Perfectionist,
    Confident,
    OldSchool,
    PartyAnimal,
    Patient,
    Experimental,
    Traditional,
    Unreliable,
    Showboat,
    Quiet,
    Moody,
    Disciplined,
    Friendly
}

[CreateAssetMenu(fileName = "NewPerformerData", menuName = "Jazz Minigame/Performer Data")]
public class PerformerData : ScriptableObject
{
    [Header("Basic Info")]
    public string performerName;

    public PerformerRole role;

    [Tooltip("Optional portrait used on the performer card.")]
    public Sprite portrait;

    [Header("Economy")]
    [Min(0f)]
    public float cost = 100f;

    [Header("Hidden Talent Score")]
    [Tooltip("Hidden score used by the performance calculation. Player sees stars instead.")]
    [Range(0, 100)]
    public int talentScore = 50;

    [Header("User-Facing Scouting Text")]
    [TextArea(2, 5)]
    [Tooltip("Shown to the player. Should imply the hidden personality tags without naming them directly.")]
    public string temperamentText;

    [TextArea(2, 5)]
    [Tooltip("Shown to the player. Should imply what kind of performers this person works well with.")]
    public string worksWellWithText;

    [TextArea(2, 5)]
    [Tooltip("Shown to the player. Should imply what kind of performers may cause problems.")]
    public string watchOutForText;

    [Header("Hidden Scoring Tags")]
    [Tooltip("What this performer is like. Other performers compare their likes/dislikes against these tags.")]
    public List<PersonalityTag> personalityTags = new List<PersonalityTag>();

    [Tooltip("If another selected performer has one of these personality tags, this performer adds chemistry.")]
    public List<PersonalityTag> likesTags = new List<PersonalityTag>();

    [Tooltip("If another selected performer has one of these personality tags, this performer loses chemistry.")]
    public List<PersonalityTag> dislikesTags = new List<PersonalityTag>();

    public int GetStarRating()
    {
        if (talentScore <= 0)
            return 0;

        return Mathf.Clamp(Mathf.CeilToInt(talentScore / 20f), 1, 5);
    }

    public string GetStarRatingText()
    {
        int rating = GetStarRating();
        string stars = "";

        for (int i = 0; i < rating; i++)
        {
            stars += "*";
        }

        for (int i = rating; i < 5; i++)
        {
            stars += "□";
        }

        return stars;
    }

    public string GetRoleDisplayName()
    {
        switch (role)
        {
            case PerformerRole.Drummer:
                return "Drummer";

            case PerformerRole.Bassist:
                return "Bass Player";

            case PerformerRole.Singer:
                return "Singer";

            case PerformerRole.SaxPlayer:
                return "Sax Player";

            default:
                return role.ToString();
        }
    }

    public string GetFormattedCost()
    {
        return BigNumberFormatter.FormatMoney(cost);
    }

    private void OnValidate()
    {
        if (cost < 0f)
        {
            cost = 0f;
        }

        talentScore = Mathf.Clamp(talentScore, 0, 100);
    }
}