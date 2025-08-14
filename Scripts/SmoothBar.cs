using Godot;

public partial class SmoothBar : TextureProgressBar
{
    [Export] public float LerpSpeed { get; set; } = 8f; // Higher = faster
    public float TargetValue { get; set; }

    public override void _Ready()
    {
        TargetValue = (float)Value;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        float v = (float)Value;
        if (Mathf.IsEqualApprox(v, TargetValue)) return;
        // Exponential smoothing for framerate independence
        float t = 1f - Mathf.Exp(-LerpSpeed * (float)delta);
        Value = Mathf.Lerp(v, TargetValue, t);
    }

    public void SetTargetPercent(float percent01)
    {
        percent01 = Mathf.Clamp(percent01, 0f, 1f);
        TargetValue = percent01 * (float)(MaxValue - MinValue) + (float)MinValue;
    }
}
