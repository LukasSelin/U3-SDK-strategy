////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;

namespace SDG.Unturned
{
	/// <summary>
	/// Finds explosive items for the Items.Explosive_Loot_Chance and Items.Allow_Explosive_Crafting options.
	/// </summary>
	public static class ExplosiveItemTool
	{
		/// <summary>
		/// True for charges (C4 etc), and for throwables, traps, magazines (rockets, explosive rounds) and
		/// consumeables that have an explosion.
		/// </summary>
		public static bool IsExplosive(ItemAsset asset)
		{
			switch (asset)
			{
				case ItemChargeAsset:
					return true;
				case ItemThrowableAsset throwableAsset:
					return throwableAsset.isExplosive;
				case ItemTrapAsset trapAsset:
					return trapAsset.isExplosive;
				case ItemMagazineAsset magazineAsset:
					return magazineAsset.isExplosive;
				case ItemConsumeableAsset consumeableAsset:
					return consumeableAsset.IsExplosive;
				default:
					return false;
			}
		}

		/// <summary>
		/// True if any of the blueprint's outputs is explosive.
		/// </summary>
		public static bool DoesBlueprintProduceExplosive(Blueprint blueprint)
		{
			if (blueprint?.outputs == null)
			{
				return false;
			}

			foreach (BlueprintOutput output in blueprint.outputs)
			{
				ItemAsset outputAsset = output.FindItemAsset();
				if (outputAsset != null && IsExplosive(outputAsset))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Rolls Items.Explosive_Loot_Chance. True if an explosive rolled as loot should be kept.
		/// </summary>
		public static bool ShouldKeepExplosiveLoot()
		{
			float chance = Provider.modeConfigData?.Items.Explosive_Loot_Chance ?? 1.0f;
			if (chance >= 1.0f)
			{
				return true;
			}

			return chance > 0.0f && Random.value < chance;
		}
	}
}
