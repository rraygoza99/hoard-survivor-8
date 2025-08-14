using Godot;

public partial class SegmentedHealthBar : TextureRect
{
    [Export] public Texture2D EmptyTexture;
    [Export] public Texture2D QuarterTexture; // ~25%
    [Export] public Texture2D HalfTexture;    // ~50%
    [Export] public Texture2D ThreeQuarterTexture; // ~75%
    [Export] public Texture2D FullTexture; // 100%

    private float _currentPercent = 1f; // 0..1
    [Export] public float ScaleFactor { get; set; } = 2f; // Multiply source texture size
    [Export] public Vector2 OverrideSize { get; set; } = Vector2.Zero; // If set (>0), use directly

    public override void _Ready()
    {
    // Use scaling so the texture fills the rect (was Keep, which left original small size)
    StretchMode = StretchModeEnum.Scale;
    TextureFilter = TextureFilterEnum.Nearest; // keep pixel crisp
        ApplyDesiredSize();
        UpdateVisual();
    }

    private void ApplyDesiredSize()
    {
        Vector2 size = Vector2.Zero;
        if (OverrideSize.X > 0 && OverrideSize.Y > 0)
        {
            size = OverrideSize;
        }
        else if (FullTexture != null)
        {
            var texSizeI = FullTexture.GetSize();
            size = new Vector2(texSizeI.X, texSizeI.Y) * ScaleFactor;
        }
        if (size != Vector2.Zero)
        {
            CustomMinimumSize = size;
            Size = size; // runtime adjust so StretchMode=Scale enlarges texture
        }
    }

    public void SetPercent(float value)
    {
        _currentPercent = Mathf.Clamp(value, 0f, 1f);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (FullTexture == null) return;
        Texture2D chosen;
        if (_currentPercent <= 0.01f)
            chosen = EmptyTexture ?? EmptyFallback();
        else if (_currentPercent <= 0.25f)
            chosen = QuarterTexture ?? EmptyTexture ?? FullTexture;
        else if (_currentPercent <= 0.5f)
            chosen = HalfTexture ?? QuarterTexture ?? FullTexture;
        else if (_currentPercent <= 0.75f)
            chosen = ThreeQuarterTexture ?? HalfTexture ?? FullTexture;
        else if (_currentPercent < 0.999f)
            chosen = ThreeQuarterTexture ?? FullTexture;
        else
            chosen = FullTexture;
        Texture = chosen;
    }

    private Texture2D EmptyFallback()
    {
        // simple fallback tint if empty not provided
        return FullTexture;
    }
}
