namespace NocturneAnnex.Controls
{
    /// <summary>Touch-control size setting (80-130%); persistence comes with F-06/F-07.</summary>
    public static class ControlsLayout
    {
        static float _scale = 1f;

        public static float Scale
        {
            get => _scale;
            set => _scale = InputMath.ClampControlsScale(value);
        }
    }
}
