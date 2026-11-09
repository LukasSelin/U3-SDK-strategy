using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SDG.Unturned
{
	/// <summary>
	/// One-click play test: skips the menus, loads straight into singleplayer (cheats on) using a throwaway save,
	/// then hands over to <see cref="PlayTestScenario"/> to build the target base and hand out the test kit.
	/// Started from the editor by Window > Unturned > Play Test. See Docs/PlayTest.md.
	/// </summary>
	public static class PlayTestSession
	{
		/// <summary>
		/// Save folder name (Worlds/PlayTest). Kept apart from the regular Singleplayer_N saves so that wiping it
		/// before every play test never touches a normal singleplayer world.
		/// </summary>
		public const string SERVER_ID = "PlayTest";

		public const string DEFAULT_LEVEL = "PEI";

#if UNITY_EDITOR
		/// <summary>
		/// SessionState (survives entering play mode, cleared when Unity closes) key holding the level name the
		/// editor tool wants to play test. Consumed by <see cref="TryStartFromEditorRequest"/>.
		/// </summary>
		public const string EDITOR_PENDING_LEVEL_KEY = "PlayTest_PendingLevel";
#endif // UNITY_EDITOR

		/// <summary>
		/// True while the running singleplayer world is a play test started by <see cref="Start"/>.
		/// </summary>
		public static bool isActive => Provider.isServer && Provider.isConnected && string.Equals(Provider.serverID, SERVER_ID);

		/// <summary>
		/// Set by <see cref="Start"/> and consumed by Provider.singleplayer to pick the save folder.
		/// </summary>
		internal static bool ConsumeIsStarting()
		{
			bool result = isStarting;
			isStarting = false;
			return result;
		}

#if UNITY_EDITOR
		/// <summary>
		/// Called by MenuStartup once the main menu is ready.
		/// </summary>
		internal static bool TryStartFromEditorRequest()
		{
			string levelName = SessionState.GetString(EDITOR_PENDING_LEVEL_KEY, string.Empty);
			if (string.IsNullOrEmpty(levelName))
			{
				return false;
			}

			SessionState.EraseString(EDITOR_PENDING_LEVEL_KEY);
			return Start(levelName);
		}
#endif // UNITY_EDITOR

		/// <summary>
		/// Call from the main menu. Loads the level in singleplayer with a fresh save and sets up the scenario
		/// once the player has spawned.
		/// </summary>
		public static bool Start(string levelName)
		{
			if (Level.getLevel(levelName) == null)
			{
				UnturnedLog.error($"Play test: unable to find level \"{levelName}\"");
				return false;
			}

			// Fresh world every time so that runs are comparable: no leftover bases, vehicles or inventory.
			string savePath = ServerSavedata.directory + "/" + SERVER_ID;
			if (ReadWrite.folderExists(savePath))
			{
				try
				{
					ReadWrite.deleteFolder(savePath);
				}
				catch (System.Exception e)
				{
					UnturnedLog.exception(e, "Play test: unable to wipe previous save");
				}
			}

			UnturnedLog.info($"Play test: starting on \"{levelName}\"");
			PlayTestScenario.ResetState();
			isStarting = true;
			Provider.map = levelName;
			Provider.singleplayer(EGameMode.NORMAL, true);
			isStarting = false;

			GameObject runnerObject = new GameObject("PlayTestRunner");
			Object.DontDestroyOnLoad(runnerObject);
			runnerObject.AddComponent<PlayTestRunner>();
			return true;
		}

		private static bool isStarting;
	}

	/// <summary>
	/// Waits for the level and local player, then builds the scenario.
	/// </summary>
	internal class PlayTestRunner : MonoBehaviour
	{
		private IEnumerator Start()
		{
			while (!Level.isLoaded || Player.LocalPlayer == null || Player.LocalPlayer.life == null || Player.LocalPlayer.life.isDead)
			{
				if (!PlayTestSession.isActive)
				{
					// Loading failed or was cancelled.
					Destroy(gameObject);
					yield break;
				}

				yield return null;
			}

			// Let spawn-in settle (initial teleport, region loading) before moving the player again.
			yield return new WaitForSeconds(1.0f);

			if (PlayTestSession.isActive && Player.LocalPlayer != null)
			{
				try
				{
					PlayTestScenario.Setup(Player.LocalPlayer);
				}
				catch (System.Exception e)
				{
					UnturnedLog.exception(e, "Play test: exception setting up scenario");
				}
			}

			Destroy(gameObject);
		}
	}
}
