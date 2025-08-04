using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UpgradeManager : Node
{
	[Export] private Godot.Collections.Array<Upgrade> _upgradePool;
	
	private List<Upgrade> _commonUpgrades = new List<Upgrade>();
	private List<Upgrade> _rareUpgrades = new List<Upgrade>();
	private List<Upgrade> _legendaryUpgrades = new List<Upgrade>();
	
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	
	public override void _Ready(){
		foreach(var upgrade in _upgradePool){
			switch (upgrade.Rarity)
			{
				case Rarity.Common:
					_commonUpgrades.Add(upgrade);
					break;
				case Rarity.Rare:
					_rareUpgrades.Add(upgrade);
					break;
				case Rarity.Legendary:
					_legendaryUpgrades.Add(upgrade);
					break;
			   }
		}
	}
	
	public List<Upgrade> GetUpgradeChoices(float playerLuck){
		GD.Print("Geting upgrades");
		var choices = new List<Upgrade>();
		var availableUpgrades = new List<Upgrade>(_upgradePool);

		for (int i = 0; i < 3; i++)
		{
			if (availableUpgrades.Count == 0) break;

			Upgrade chosenUpgrade = PickOneUpgrade(playerLuck, availableUpgrades);
			choices.Add(chosenUpgrade);
			GD.Print($"Adding {chosenUpgrade.Name}");
		}
		
		return choices;
	}
	
	private Upgrade PickOneUpgrade(float playerLuck, List<Upgrade> availableUpgrades){
		float commonWeight = 70;
		float rareWeight = 25;
		float legendaryWeight = 5;
		
		rareWeight += playerLuck * 0.5f;
		legendaryWeight += playerLuck * 0.5f;
		
		var availableCommon = availableUpgrades.Where(u => u.Rarity == Rarity.Common).ToList();
		var availableRare = availableUpgrades.Where(u => u.Rarity == Rarity.Rare).ToList();
		var availableLegendary = availableUpgrades.Where(u => u.Rarity == Rarity.Legendary).ToList();
		
		if (availableLegendary.Count == 0)
		{
			rareWeight += legendaryWeight;
			legendaryWeight = 0;
		}
		if (availableRare.Count == 0)
		{
			commonWeight += rareWeight;
			rareWeight = 0;
		}
		if (availableCommon.Count == 0)
		{
			commonWeight = 0;
		}
		float totalWeight = commonWeight + rareWeight + legendaryWeight;
		float roll = _rng.Randf() * totalWeight;

		if (roll < legendaryWeight)
		{
			return availableLegendary[_rng.RandiRange(0, availableLegendary.Count - 1)];
		}
		else if (roll < legendaryWeight + rareWeight)
		{
			return availableRare[_rng.RandiRange(0, availableRare.Count - 1)];
		}
		else
		{
			return availableCommon[_rng.RandiRange(0, availableCommon.Count - 1)];
		}
	}
}
