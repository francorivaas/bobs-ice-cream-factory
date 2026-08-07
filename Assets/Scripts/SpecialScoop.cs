using UnityEngine;

public enum SpecialScoopType
{
    None,
    Bomb,
    Golden
}

public class SpecialScoop : MonoBehaviour
{
    [SerializeField]
    private SpecialScoopType specialType =
        SpecialScoopType.None;

    public SpecialScoopType Type => specialType;

    public void Configure(SpecialScoopType type)
    {
        specialType = type;

        gameObject.name = type switch
        {
            SpecialScoopType.Bomb => "Scoop_Bomb",
            SpecialScoopType.Golden => "Scoop_Golden",
            _ => "Scoop_Special"
        };
    }
}