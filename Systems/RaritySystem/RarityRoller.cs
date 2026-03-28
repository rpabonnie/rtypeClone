using rtypeClone.Entities;

namespace rtypeClone.Systems.RaritySystem;

/// <summary>
/// Rolls enemy rarity and affixes at spawn time.
/// Weights escalate per wave — later waves push toward higher rarity tiers.
/// Uses Span-based output to avoid per-spawn allocation.
/// </summary>
public class RarityRoller
{
    // Base weights: Normal=70, Magic=22, Rare=7, Unique=1
    private static readonly float[] BaseWeights = { 70f, 22f, 7f, 1f };

    // Per-wave escalation: each wave shifts weight from Normal toward higher tiers
    // By wave 20, roughly: Normal=30, Magic=42, Rare=22, Unique=6
    private const float NormalDecayPerWave = 2f;
    private const float MagicGainPerWave = 1f;
    private const float RareGainPerWave = 0.75f;
    private const float UniqueGainPerWave = 0.25f;
    private const float MinNormalWeight = 20f;

    // Scratch arrays to avoid allocation during affix rolling
    private readonly bool[] _excludedAffixes;
    private readonly int[] _candidateIndices;

    public RarityRoller(int maxAffixPoolSize = 32)
    {
        _excludedAffixes = new bool[maxAffixPoolSize];
        _candidateIndices = new int[maxAffixPoolSize];
    }

    /// <summary>
    /// Roll a rarity tier using wave-escalated weights.
    /// </summary>
    public EnemyRarity RollRarity(int waveNumber)
    {
        float normalW = MathF.Max(BaseWeights[0] - NormalDecayPerWave * waveNumber, MinNormalWeight);
        float magicW  = BaseWeights[1] + MagicGainPerWave * waveNumber;
        float rareW   = BaseWeights[2] + RareGainPerWave * waveNumber;
        float uniqueW = BaseWeights[3] + UniqueGainPerWave * waveNumber;

        float total = normalW + magicW + rareW + uniqueW;
        float roll = Random.Shared.NextSingle() * total;

        if (roll < normalW) return EnemyRarity.Normal;
        roll -= normalW;
        if (roll < magicW) return EnemyRarity.Magic;
        roll -= magicW;
        if (roll < rareW) return EnemyRarity.Rare;
        return EnemyRarity.Unique;
    }

    /// <summary>
    /// Rolls affixes for a given rarity. Fills the provided span with affix IDs.
    /// Returns the number of affixes actually written.
    /// Respects min/max affix counts per rarity and incompatibility rules.
    /// </summary>
    public int RollAffixes(
        EnemyRarity rarity,
        AffixRegistry registry,
        Span<string> outAffixIds)
    {
        int min = RarityConstants.MinAffixes(rarity);
        int max = RarityConstants.MaxAffixes(rarity);
        if (max == 0 || outAffixIds.Length == 0) return 0;

        // Determine count to roll
        int count = min + Random.Shared.Next(0, max - min + 1);
        count = Math.Min(count, outAffixIds.Length);

        var candidates = registry.GetForRarity(rarity);
        if (candidates.Count == 0) return 0;

        // Reset exclusion flags
        Array.Clear(_excludedAffixes, 0, Math.Min(_excludedAffixes.Length, candidates.Count));

        int written = 0;
        for (int attempt = 0; attempt < count; attempt++)
        {
            // Build candidate pool (not excluded)
            int poolSize = 0;
            for (int i = 0; i < candidates.Count && i < _candidateIndices.Length; i++)
            {
                if (!_excludedAffixes[i])
                    _candidateIndices[poolSize++] = i;
            }

            if (poolSize == 0) break;

            // Pick one
            int pick = Random.Shared.Next(0, poolSize);
            int chosenIdx = _candidateIndices[pick];
            var chosen = candidates[chosenIdx];

            outAffixIds[written++] = chosen.Id;

            // Exclude the chosen affix itself (no duplicates)
            _excludedAffixes[chosenIdx] = true;

            // Exclude incompatible affixes
            foreach (var incompId in chosen.IncompatibleWith)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (string.Equals(candidates[i].Id, incompId, StringComparison.Ordinal))
                        _excludedAffixes[i] = true;
                }
            }
        }

        return written;
    }

    /// <summary>
    /// Builds a display name for an enemy from its affixes.
    /// E.g., "Swiftness Armored" for an enemy with the fast + armored affixes.
    /// Returns empty string for Normal enemies.
    /// </summary>
    public static string BuildDisplayName(
        EnemyRarity rarity,
        ReadOnlySpan<string> affixIds,
        AffixRegistry registry)
    {
        if (rarity == EnemyRarity.Normal || affixIds.Length == 0)
            return "";

        // Build name from affix display names
        // Use stackalloc-friendly approach for short strings
        var parts = new string[affixIds.Length];
        for (int i = 0; i < affixIds.Length; i++)
        {
            if (registry.TryGet(affixIds[i], out var def) && def != null)
                parts[i] = def.DisplayName;
            else
                parts[i] = affixIds[i];
        }

        return string.Join(" ", parts);
    }
}
