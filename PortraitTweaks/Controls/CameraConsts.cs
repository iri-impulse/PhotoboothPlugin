using System.Numerics;
using Lumina.Excel.Sheets;

namespace PortraitTweaks.Controls;

/// <summary>
/// Named constants, extracted from the UIConst excel sheet, that control
/// the limits and handling of the camera in the portrait editor.
/// </summary>
public static class CameraConsts
{
    public static readonly float Scale = 100f;
    public static float OrbitIncrement => Get(Const.OrbitIncrement);

    public static float PivotXMin => Get(Const.PivotXMin);
    public static float PivotXMax => Get(Const.PivotXMax);
    public static float PivotYMin => Get(Const.PivotYMin);
    public static float PivotYMax => Get(Const.PivotYMax);
    public static float PivotZMin => Get(Const.PivotZMin);
    public static float PivotZMax => Get(Const.PivotZMax);

    public static float AngleIncrement => Get(Const.AngleIncrement);
    public static float PitchMin => Get(Const.PitchMin);
    public static float PitchMax => Get(Const.PitchMax);

    public static float DistanceIncrement => Get(Const.DistanceIncrement);
    public static float DistanceMin => Get(Const.DistanceMin);
    public static float DistanceMax => Get(Const.DistanceMax);

    public static Vector3 PivotMin => new(PivotXMin, PivotYMin, PivotZMin);

    public static Vector3 PivotMax => new(PivotXMax, PivotYMax, PivotZMax);

    private enum Const : int
    {
        OrbitIncrement = 2,
        PivotXMin = 12,
        PivotXMax = 15,
        PivotYMin = 13,
        PivotYMax = 16,
        PivotZMin = 14,
        PivotZMax = 17,

        AngleIncrement = 3,
        PitchMin = 10,
        PitchMax = 11,

        DistanceIncrement = 4,
        DistanceMin = 8,
        DistanceMax = 9,
    }

    private static float Get(Const id)
    {
        return (float)Plugin.DataManager.GetExcelSheet<UIConst>()!.GetRowAt((int)id).Unknown0;
    }
}
