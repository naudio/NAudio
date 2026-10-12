namespace NAudio.Dmo.Effect;

/// <summary>
/// Chorus Phase
/// </summary>
[System.Obsolete("Only used by the obsolete DMO effects. Use the managed effects in NAudio.Effects instead. This type will be removed in a future major version.")]
public enum ChorusPhase : int
{
    /// <summary>
    /// DSFXCHORUS_PHASE_NEG_180
    /// </summary>
    Neg180 = 0,
    /// <summary>
    /// DSFXCHORUS_PHASE_NEG_90
    /// </summary>
    Neg90 = 1,
    /// <summary>
    /// DSFXCHORUS_PHASE_ZERO
    /// </summary>
    Zero = 2,
    /// <summary>
    /// DSFXCHORUS_PHASE_90
    /// </summary>
    Pos90 = 3,
    /// <summary>
    /// DSFXCHORUS_PHASE_180
    /// </summary>
    Pos180 = 4,
}
