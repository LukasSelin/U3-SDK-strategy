using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned
{
	/// <summary>
	/// Builds the play test scenario in a running singleplayer world:
	/// an unowned "enemy" base next to zombie spawns, the player a short drive away facing it, Lure Bots parked
	/// beside the player, and a small kit. Everything is server-side and uses the regular spawn paths, so it
	/// behaves like content placed by players. See Docs/PlayTest.md.
	/// </summary>
	public static class PlayTestScenario
	{
		/// <summary>
		/// Lure Bot vehicle from Builds/Shared/Sandbox/PlayTest.
		/// </summary>
		public static readonly Guid LURE_BOT_GUID = new Guid("6f0f4a54c1d94d0b9e6b7a1f4c2d8e31");

		/// <summary>
		/// Distance from where the player starts to the middle of the base.
		/// Shorter than the Lure Bot's zombie leash (64 m) so a horde picked up at the start makes it all the way.
		/// </summary>
		public const float START_DISTANCE = 45.0f;

		/// <summary>
		/// The base is this many floors along each side.
		/// </summary>
		private const int BASE_SIZE = 3;
		private const float BASE_HALF_EXTENT = BASE_SIZE * HousingConnections.HALF_EDGE_LENGTH;

		private static readonly Guid FLOOR_GUID = new Guid("b4ac981d976b4b12b67f55fde662b70c"); // Pine Floor
		private static readonly Guid WALL_GUID = new Guid("dec7d69e836f4d7f9c890a9828784772"); // Pine Wall
		private static readonly Guid DOORWAY_GUID = new Guid("2718e79756684574a8223f055c70470f"); // Pine Doorway
		private static readonly Guid WINDOW_GUID = new Guid("a921ecb08b6740e397d467e66acf104b"); // Pine Window
		private static readonly Guid PILLAR_GUID = new Guid("26523c2cb43f429ea27e8db284e459de"); // Pine Pillar
		private static readonly Guid ROOF_GUID = new Guid("3a0f7c7ceb314cab9b52eea4e3e1ff23"); // Pine Roof

		/// <summary>
		/// Legacy item ID and amount.
		/// </summary>
		private static readonly (ushort id, byte amount)[] KIT = new (ushort, byte)[]
		{
			(18, 1), // Timberwolf (for sniping bots)
			(20, 4), // Timberwolf Magazine
			(363, 1), // Maplestrike
			(6, 4), // Military Magazine
			(333, 1), // Binoculars
			(15, 2), // Medkit
		};

		/// <summary>
		/// False until a scenario has been built this session.
		/// </summary>
		public static bool hasSite
		{
			get;
			private set;
		}

		/// <summary>
		/// Middle of the base at ground level.
		/// </summary>
		public static Vector3 basePoint
		{
			get;
			private set;
		}

		/// <summary>
		/// Where the player and Lure Bots start.
		/// </summary>
		public static Vector3 startPoint
		{
			get;
			private set;
		}

		/// <summary>
		/// Pick a site automatically (deterministic per map, so every run is in the same place) and build there.
		/// Falls back to the player's current position if the map has nowhere suitable.
		/// </summary>
		public static void Setup(Player player)
		{
			if (!CanRun(player))
				return;

			if (TryFindSite(out Vector3 foundBase, out Vector3 foundStart))
			{
				BuildAt(player, foundBase, foundStart);
			}
			else
			{
				UnturnedLog.warn("Play test: no clear site near zombie spawns on this map, building at the player instead");
				MoveHere(player);
			}
		}

		/// <summary>
		/// Tear down and rebuild at the current site, and put the player back at the start with fresh bots.
		/// </summary>
		public static void Reset(Player player)
		{
			if (!CanRun(player))
				return;

			if (hasSite)
			{
				BuildAt(player, basePoint, startPoint);
			}
			else
			{
				Setup(player);
			}
		}

		/// <summary>
		/// Rebuild with the player's current position as the start and the base straight ahead of them.
		/// Useful for testing on specific terrain: fly/walk somewhere, face where the base should go, and call this.
		/// </summary>
		public static void MoveHere(Player player)
		{
			if (!CanRun(player))
				return;

			Vector3 start = player.transform.position;
			Vector3 forward = Quaternion.Euler(0.0f, player.look.yaw, 0.0f) * Vector3.forward;
			BuildAt(player, start + forward * START_DISTANCE, start);
		}

		/// <summary>
		/// Spawn one more Lure Bot in front of the player, pointing the way they are facing.
		/// </summary>
		public static InteractableVehicle SpawnLureBot(Player player)
		{
			if (!CanRun(player))
				return null;

			Quaternion rotation = Quaternion.Euler(0.0f, player.look.yaw, 0.0f);
			return SpawnLureBotAt(player.transform.position + rotation * new Vector3(0.0f, 0.0f, 4.0f), rotation);
		}

		public static void GiveKit(Player player)
		{
			if (!CanRun(player))
				return;

			foreach ((ushort id, byte amount) in KIT)
			{
				if (!ItemTool.tryForceGiveItem(player, id, amount))
				{
					UnturnedLog.warn($"Play test: unable to give item {id}");
				}
			}
		}

		/// <summary>
		/// Forget the previous session's site and spawned vehicles (they went away with its world).
		/// </summary>
		internal static void ResetState()
		{
			hasSite = false;
			hasGivenKit = false;
			spawnedVehicles.Clear();
		}

		private static bool CanRun(Player player)
		{
			if (player == null)
			{
				UnturnedLog.warn("Play test: no player");
				return false;
			}

			// Building wipes every structure in the world, so only ever do it in the throwaway play test save.
			if (!PlayTestSession.isActive)
			{
				UnturnedLog.warn("Play test: not in a play test session (start one from Window > Unturned > Play Test)");
				return false;
			}

			return true;
		}

		private static void BuildAt(Player player, Vector3 newBasePoint, Vector3 newStartPoint)
		{
			newStartPoint.y = LevelGround.getHeight(newStartPoint);
			newBasePoint.y = LevelGround.getHeight(newBasePoint);

			Vector3 toBase = newBasePoint - newStartPoint;
			toBase.y = 0.0f;
			if (toBase.sqrMagnitude < 1.0f)
			{
				toBase = Vector3.forward;
			}
			toBase.Normalize();

			basePoint = newBasePoint;
			startPoint = newStartPoint;
			hasSite = true;

			// Remove the previous scenario. Bots first so their passengers are ejected before the teleport.
			foreach (InteractableVehicle vehicle in spawnedVehicles)
			{
				if (vehicle != null && !vehicle.isDead)
				{
					VehicleManager.askVehicleDestroy(vehicle);
				}
			}
			spawnedVehicles.Clear();
			StructureManager.askClearAllStructures();

			// Door faces the start.
			float baseYaw = Quaternion.LookRotation(-toBase).eulerAngles.y;
			int structureCount = BuildBase(newBasePoint, baseYaw);

			float startYaw = Quaternion.LookRotation(toBase).eulerAngles.y;
			player.teleportToLocationUnsafe(newStartPoint + new Vector3(0.0f, 0.5f, 0.0f), startYaw);

			// One bot on either side of the player, already aimed at the door.
			Quaternion botRotation = Quaternion.Euler(0.0f, startYaw, 0.0f);
			int botCount = 0;
			for (int side = -1; side <= 1; side += 2)
			{
				if (SpawnLureBotAt(newStartPoint + botRotation * new Vector3(side * 4.0f, 0.0f, 3.0f), botRotation) != null)
				{
					++botCount;
				}
			}

			if (!hasGivenKit)
			{
				hasGivenKit = true;
				GiveKit(player);
			}

			// Midday so there is something to see.
			LightingManager.time = (uint) (LightingManager.cycle * LevelLighting.bias * 0.5f);

			string summary = $"Play test ready: base ({structureCount} structures) {START_DISTANCE:0} m ahead, {botCount} Lure Bot(s) beside you";
			UnturnedLog.info($"{summary}. Base: {newBasePoint} Start: {newStartPoint}");
			ChatManager.say(summary, Color.green);
			if (botCount == 0)
			{
				ChatManager.say("Lure Bot vehicle is missing - is Builds/Shared/Sandbox/PlayTest present?", Color.red);
			}
		}

		private static InteractableVehicle SpawnLureBotAt(Vector3 point, Quaternion rotation)
		{
			VehicleAsset asset = Assets.find<VehicleAsset>(LURE_BOT_GUID);
			if (asset == null)
			{
				UnturnedLog.warn($"Play test: Lure Bot vehicle {LURE_BOT_GUID:N} not found (expected in Builds/Shared/Sandbox/PlayTest)");
				return null;
			}

			if (!asset.isLureBot)
			{
				UnturnedLog.warn($"Play test: {asset.name} has no Lure_Noise_Radius, so it will not lure anything");
			}

			point.y = LevelGround.getHeight(point) + 1.0f;
			InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(asset, point, rotation);
			if (vehicle != null)
			{
				spawnedVehicles.Add(vehicle);
			}
			return vehicle;
		}

		#region Base

		/// <summary>
		/// Square of floors with walls all the way around: doorway in the middle of the front (local +Z), windows in
		/// the middle of the other sides, and a roof over the back row. Front is the side at local +Z.
		/// </summary>
		/// <returns>Number of structures placed.</returns>
		private static int BuildBase(Vector3 center, float yaw)
		{
			ItemStructureAsset floorAsset = Assets.find<ItemStructureAsset>(FLOOR_GUID);
			ItemStructureAsset wallAsset = Assets.find<ItemStructureAsset>(WALL_GUID);
			ItemStructureAsset doorwayAsset = Assets.find<ItemStructureAsset>(DOORWAY_GUID);
			ItemStructureAsset windowAsset = Assets.find<ItemStructureAsset>(WINDOW_GUID);
			ItemStructureAsset pillarAsset = Assets.find<ItemStructureAsset>(PILLAR_GUID);
			ItemStructureAsset roofAsset = Assets.find<ItemStructureAsset>(ROOF_GUID);

			Quaternion baseRotation = Quaternion.Euler(0.0f, yaw, 0.0f);

			// Floors are level, so sit them on the highest ground under the footprint. Their foundations reach
			// well below the floor, which hides any gap on the downhill side.
			float floorHeight = float.MinValue;
			for (int x = -BASE_SIZE; x <= BASE_SIZE; ++x)
			{
				for (int z = -BASE_SIZE; z <= BASE_SIZE; ++z)
				{
					Vector3 samplePoint = center + baseRotation * new Vector3(x * HousingConnections.HALF_EDGE_LENGTH, 0.0f, z * HousingConnections.HALF_EDGE_LENGTH);
					floorHeight = Mathf.Max(floorHeight, LevelGround.getHeight(samplePoint));
				}
			}
			Vector3 origin = new Vector3(center.x, floorHeight + 0.25f, center.z);

			int count = 0;
			void Place(ItemStructureAsset asset, Vector3 localPosition, float localYaw)
			{
				if (asset == null)
					return;

				// Structure models are Z-up, hence the -90 pitch. Same rotation StructureManager.dropStructure builds.
				Quaternion rotation = Quaternion.Euler(-90.0f, yaw + localYaw, 0.0f);
				// Owner and group zero: nobody's base, so the player cannot salvage it and it counts as an enemy's.
				if (StructureManager.dropReplicatedStructure(new Structure(asset, asset.health), origin + baseRotation * localPosition, rotation, 0, 0))
				{
					++count;
				}
			}

			// Offset of the first floor's center so the grid is centered on the origin.
			const float edge = HousingConnections.EDGE_LENGTH;
			float first = -(BASE_SIZE - 1) * HousingConnections.HALF_EDGE_LENGTH;
			int middle = BASE_SIZE / 2;

			for (int x = 0; x < BASE_SIZE; ++x)
			{
				for (int z = 0; z < BASE_SIZE; ++z)
				{
					Place(floorAsset, new Vector3(first + x * edge, 0.0f, first + z * edge), 0.0f);
				}
			}

			// Pillars on every floor corner along the outside.
			for (int x = 0; x <= BASE_SIZE; ++x)
			{
				for (int z = 0; z <= BASE_SIZE; ++z)
				{
					bool isPerimeter = x == 0 || x == BASE_SIZE || z == 0 || z == BASE_SIZE;
					if (isPerimeter)
					{
						Place(pillarAsset, new Vector3(-BASE_HALF_EXTENT + x * edge, HousingConnections.WALL_PIVOT_OFFSET, -BASE_HALF_EXTENT + z * edge), 0.0f);
					}
				}
			}

			// Wall yaw offsets match HousingConnections.LinkSquareEdge for a floor with zero yaw: a floor's local +Y
			// edge (rotation 0) ends up on our -Z side, -Y (180) on +Z, +X (270) on +X, and -X (90) on -X.
			for (int index = 0; index < BASE_SIZE; ++index)
			{
				float along = first + index * edge;
				bool isMiddle = index == middle;
				Place(isMiddle ? doorwayAsset : wallAsset, new Vector3(along, HousingConnections.WALL_PIVOT_OFFSET, BASE_HALF_EXTENT), 180.0f); // Front
				Place(isMiddle ? windowAsset : wallAsset, new Vector3(along, HousingConnections.WALL_PIVOT_OFFSET, -BASE_HALF_EXTENT), 0.0f); // Back
				Place(isMiddle ? windowAsset : wallAsset, new Vector3(BASE_HALF_EXTENT, HousingConnections.WALL_PIVOT_OFFSET, along), 270.0f); // Right
				Place(isMiddle ? windowAsset : wallAsset, new Vector3(-BASE_HALF_EXTENT, HousingConnections.WALL_PIVOT_OFFSET, along), 90.0f); // Left
			}

			// Roof over the back row only, leaving the rest open so the inside is visible from a distance.
			for (int x = 0; x < BASE_SIZE; ++x)
			{
				Place(roofAsset, new Vector3(first + x * edge, HousingConnections.WALL_HEIGHT, first), 0.0f);
			}

			return count;
		}

		#endregion Base

		#region Site selection

		/// <summary>
		/// Radius around the base center that must be free of map objects and trees.
		/// </summary>
		private const float BASE_CLEAR_RADIUS = BASE_HALF_EXTENT * 1.5f;

		/// <summary>
		/// Half-width of the lane between start and base that must be free of map objects and trees.
		/// </summary>
		private const float LANE_CLEAR_RADIUS = 4.0f;

		/// <summary>
		/// Map objects only expose a pivot, so pad the search radius by a guess at how far each kind extends from it.
		/// </summary>
		private const float LARGE_OBJECT_PADDING = 18.0f;
		private const float MEDIUM_OBJECT_PADDING = 6.0f;
		private const float TREE_PADDING = 2.0f;

		private const float MAX_BASE_HEIGHT_RANGE = 1.5f;
		private const float MAX_LANE_HEIGHT_STEP = 2.5f;
		private const float LANE_STEP = 5.0f;

		/// <summary>
		/// Look for flat, unobstructed ground inside a zombie navigation area (zombies do not exist outside of them),
		/// preferring the areas with the most zombie spawns. No randomness: the same map always gives the same site.
		/// </summary>
		private static bool TryFindSite(out Vector3 foundBase, out Vector3 foundStart)
		{
			foundBase = default;
			foundStart = default;

			List<ZombieSpawnpoint>[] zombieSpawns = LevelZombies.zombies;
			if (zombieSpawns == null)
				return false;

			List<int> navs = new List<int>();
			for (int nav = 0; nav < zombieSpawns.Length; ++nav)
			{
				if (zombieSpawns[nav] != null && zombieSpawns[nav].Count > 0)
				{
					navs.Add(nav);
				}
			}
			// Stable sort by spawn count descending.
			navs.Sort((lhs, rhs) =>
			{
				int result = zombieSpawns[rhs].Count.CompareTo(zombieSpawns[lhs].Count);
				return result != 0 ? result : lhs.CompareTo(rhs);
			});

			// First pass wants the start inside the same navigation area too, so zombies are around from the very
			// beginning of the bot's run. Second pass settles for only the base being inside.
			for (int pass = 0; pass < 2; ++pass)
			{
				bool requireStartInNav = pass == 0;
				foreach (int nav in navs)
				{
					List<ZombieSpawnpoint> spawns = zombieSpawns[nav];
					// Bound the amount of work on maps with thousands of spawns.
					int stride = Mathf.Max(1, spawns.Count / 64);
					for (int spawnIndex = 0; spawnIndex < spawns.Count; spawnIndex += stride)
					{
						Vector3 spawnPoint = spawns[spawnIndex].point;
						for (float radius = 0.0f; radius <= 40.0f; radius += 20.0f)
						{
							int angleCount = radius > 0.0f ? 8 : 1;
							for (int angleIndex = 0; angleIndex < angleCount; ++angleIndex)
							{
								Vector3 candidateBase = spawnPoint + Quaternion.Euler(0.0f, angleIndex * 45.0f, 0.0f) * new Vector3(0.0f, 0.0f, radius);
								if (!IsInNav(candidateBase, nav) || !IsBaseSiteSuitable(candidateBase))
									continue;

								for (int startAngleIndex = 0; startAngleIndex < 8; ++startAngleIndex)
								{
									Vector3 candidateStart = candidateBase + Quaternion.Euler(0.0f, startAngleIndex * 45.0f, 0.0f) * new Vector3(0.0f, 0.0f, START_DISTANCE);
									if (requireStartInNav && !IsInNav(candidateStart, nav))
										continue;

									if (IsLaneSuitable(candidateStart, candidateBase))
									{
										foundBase = candidateBase;
										foundStart = candidateStart;
										return true;
									}
								}
							}
						}
					}
				}
			}

			return false;
		}

		private static bool IsInNav(Vector3 point, int nav)
		{
			return LevelNavigation.tryGetNavigation(point, out byte pointNav) && pointNav == nav;
		}

		private static bool IsBaseSiteSuitable(Vector3 center)
		{
			float minHeight = float.MaxValue;
			float maxHeight = float.MinValue;
			for (int x = -1; x <= 1; ++x)
			{
				for (int z = -1; z <= 1; ++z)
				{
					Vector3 samplePoint = center + new Vector3(x * BASE_CLEAR_RADIUS, 0.0f, z * BASE_CLEAR_RADIUS);
					if (!TryGetDryGroundHeight(samplePoint, out float height))
						return false;

					minHeight = Mathf.Min(minHeight, height);
					maxHeight = Mathf.Max(maxHeight, height);
				}
			}

			if (maxHeight - minHeight > MAX_BASE_HEIGHT_RANGE)
				return false;

			return IsClearOfObjects(center, BASE_CLEAR_RADIUS);
		}

		/// <summary>
		/// The bot does not steer, so it needs a straight, gentle, unobstructed run from the start to the door.
		/// </summary>
		private static bool IsLaneSuitable(Vector3 start, Vector3 baseCenter)
		{
			Vector3 delta = baseCenter - start;
			delta.y = 0.0f;
			float distance = delta.magnitude;
			Vector3 direction = delta / distance;

			if (!TryGetDryGroundHeight(start, out float previousHeight))
				return false;

			// Stop where the base's own clear area begins; that part has already been checked.
			float laneLength = distance - BASE_CLEAR_RADIUS;
			for (float along = 0.0f; along <= laneLength; along += LANE_STEP)
			{
				Vector3 samplePoint = start + direction * along;
				if (!TryGetDryGroundHeight(samplePoint, out float height))
					return false;

				if (Mathf.Abs(height - previousHeight) > MAX_LANE_HEIGHT_STEP)
					return false;
				previousHeight = height;

				if (!IsClearOfObjects(samplePoint, LANE_CLEAR_RADIUS))
					return false;
			}

			return true;
		}

		private static bool TryGetDryGroundHeight(Vector3 point, out float height)
		{
			if (!SDG.Framework.Landscapes.Landscape.getWorldHeight(point, out height))
				return false; // Off the terrain.

			point.y = height + 0.5f;
			return !LevelLighting.isPositionUnderwater(point) && !SDG.Framework.Water.WaterUtility.isPointUnderwater(point);
		}

		/// <summary>
		/// Uses level data rather than physics queries because colliders far from the player are not loaded.
		/// </summary>
		private static bool IsClearOfObjects(Vector3 center, float radius)
		{
			float searchRadius = radius + LARGE_OBJECT_PADDING;
			int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - searchRadius + 4096.0f) / Regions.REGION_SIZE));
			int maxX = Mathf.Min(Regions.WORLD_SIZE - 1, Mathf.FloorToInt((center.x + searchRadius + 4096.0f) / Regions.REGION_SIZE));
			int minY = Mathf.Max(0, Mathf.FloorToInt((center.z - searchRadius + 4096.0f) / Regions.REGION_SIZE));
			int maxY = Mathf.Min(Regions.WORLD_SIZE - 1, Mathf.FloorToInt((center.z + searchRadius + 4096.0f) / Regions.REGION_SIZE));

			for (int x = minX; x <= maxX; ++x)
			{
				for (int y = minY; y <= maxY; ++y)
				{
					List<LevelObject> objects = LevelObjects.objects?[x, y];
					if (objects != null)
					{
						foreach (LevelObject levelObject in objects)
						{
							if (levelObject == null || levelObject.asset == null || levelObject.transform == null)
								continue;

							float padding;
							switch (levelObject.asset.type)
							{
								case EObjectType.LARGE:
									padding = LARGE_OBJECT_PADDING;
									break;

								case EObjectType.MEDIUM:
									padding = MEDIUM_OBJECT_PADDING;
									break;

								default:
									// Small props and decals will not stop a base or a bot.
									continue;
							}

							if (IsWithinXZ(levelObject.transform.position, center, radius + padding))
								return false;
						}
					}

					List<ResourceSpawnpoint> trees = LevelGround.trees?[x, y];
					if (trees != null)
					{
						foreach (ResourceSpawnpoint tree in trees)
						{
							if (tree != null && IsWithinXZ(tree.point, center, radius + TREE_PADDING))
								return false;
						}
					}
				}
			}

			return true;
		}

		private static bool IsWithinXZ(Vector3 point, Vector3 center, float radius)
		{
			float dx = point.x - center.x;
			float dz = point.z - center.z;
			return (dx * dx) + (dz * dz) < radius * radius;
		}

		#endregion Site selection

		private static readonly List<InteractableVehicle> spawnedVehicles = new List<InteractableVehicle>();
		private static bool hasGivenKit;
	}
}
