using Godot;
using System;

public partial class UpgradeCard : PanelContainer
{
	[Signal]
	public delegate void UpgradeSelectedEventHandler(Upgrade upgrade);
	
	private Label _nameLabel;
	private Label _descriptionLabel;
	private Button _button;
	private Upgrade _upgrade;
	
	public override void _Ready(){
		_nameLabel = GetNode<Label>("Button/VBoxContainer/NameLabel");
		_descriptionLabel = GetNode<Label>("Button/VBoxContainer/DescriptionLabel");
		_button = GetNode<Button>("Button");
		_button.Pressed += OnButtonPressed;
	}
	public void SetUpgrade(Upgrade upgrade)
	{
		_upgrade = upgrade;
		_nameLabel.Text = upgrade.Name;
		_descriptionLabel.Text = upgrade.Description;

		var styleBox = new StyleBoxFlat { BgColor = GetRarityColor(upgrade.Rarity) };
		AddThemeStyleboxOverride("panel", styleBox);
	}
	private void OnButtonPressed()
	{
		EmitSignal(SignalName.UpgradeSelected, _upgrade);
	}

	private Color GetRarityColor(Rarity rarity)
	{
		switch (rarity)
		{
			case Rarity.Common:
				return new Color("#a8a8a8");
			case Rarity.Rare:
				return new Color("#4d84ff");
	 		  case Rarity.Legendary:
				return new Color("#ffc54d");
			default:
				return new Color("#ffffff");
		}
	}
}
