using UnityEngine;

public enum ScoopColorType
{
    Yellow,
    Pink,
    Green,
    Vanilla
}

public static class ScoopColorPalette
{
    public static Color GetColor(ScoopColorType colorType)
    {
        switch (colorType)
        {
            case ScoopColorType.Yellow:
                return new Color32(255, 221, 112, 255);

            case ScoopColorType.Pink:
                return new Color32(255, 166, 176, 255);

            case ScoopColorType.Green:
                return new Color32(157, 230, 166, 255);

            case ScoopColorType.Vanilla:
                return new Color32(235, 219, 210, 255);

            default:
                return Color.white;
        }
    }
}