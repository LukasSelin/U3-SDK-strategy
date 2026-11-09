using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SDG.Unturned.Tools
{
	/// <summary>
	/// One-click launcher for <see cref="PlayTestSession"/>, plus buttons to rearrange the scenario while playing.
	/// See Docs/PlayTest.md.
	/// </summary>
	public class PlayTestTool : EditorWindow
	{
		private const string STARTUP_SCENE_PATH = "Assets/GameStartup.unity";
		private const string LEVEL_PREF_KEY = "PlayTestLevel";

		[MenuItem("Window/Unturned/Play Test")]
		public static void ShowWindow()
		{
			GetWindow(typeof(PlayTestTool));
		}

		/// <summary>
		/// Ctrl+Alt+P: enter play mode and load straight into the play test scenario on the saved level.
		/// </summary>
		[MenuItem("Window/Unturned/Start Play Test %&p")]
		public static void StartPlayTest()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Debug.LogWarning("Play test: already in play mode. Stop playing first, or use the Play Test window to reset the scenario.");
				return;
			}

			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return;
			}

			EditorSceneManager.OpenScene(STARTUP_SCENE_PATH);
			// Picked up by MenuStartup once the main menu has loaded.
			SessionState.SetString(PlayTestSession.EDITOR_PENDING_LEVEL_KEY, SavedLevel);
			EditorApplication.EnterPlaymode();
		}

		[MenuItem("Window/Unturned/Start Play Test %&p", true)]
		private static bool ValidateStartPlayTest()
		{
			return !EditorApplication.isPlayingOrWillChangePlaymode;
		}

		private static string SavedLevel
		{
			get
			{
				string level = EditorPrefs.GetString(LEVEL_PREF_KEY, PlayTestSession.DEFAULT_LEVEL);
				return string.IsNullOrWhiteSpace(level) ? PlayTestSession.DEFAULT_LEVEL : level.Trim();
			}
		}

		private void OnEnable()
		{
			titleContent = new GUIContent("Play Test");
		}

		private void OnInspectorUpdate()
		{
			// Keep button enabled states in sync with the game.
			Repaint();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);

			using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
			{
				string savedLevel = EditorPrefs.GetString(LEVEL_PREF_KEY, PlayTestSession.DEFAULT_LEVEL);
				string newLevel = EditorGUILayout.TextField(new GUIContent("Level", "Map folder name, e.g. PEI, Washington, Russia."), savedLevel);
				if (newLevel != savedLevel)
				{
					EditorPrefs.SetString(LEVEL_PREF_KEY, newLevel);
				}

				if (GUILayout.Button(new GUIContent("Start Play Test (Ctrl+Alt+P)", "Enter play mode, skip the menus, and load into singleplayer with the base, Lure Bots and kit ready. Uses a fresh save every time."), GUILayout.Height(32.0f)))
				{
					// Entering play mode from inside OnGUI upsets the layout pass.
					EditorApplication.delayCall += StartPlayTest;
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("While Playing", EditorStyles.boldLabel);

			bool isActive = EditorApplication.isPlaying && PlayTestSession.isActive && Player.LocalPlayer != null;
			if (!isActive)
			{
				EditorGUILayout.HelpBox("Available once a play test is running.", MessageType.Info);
			}

			using (new EditorGUI.DisabledScope(!isActive))
			{
				if (GUILayout.Button(new GUIContent("Reset Scenario", "Rebuild the base, respawn the Lure Bots, and put you back at the start.")))
				{
					PlayTestScenario.Reset(Player.LocalPlayer);
				}

				if (GUILayout.Button(new GUIContent("Move Scenario Here", "Use where you are standing as the start, and rebuild the base straight ahead of where you are looking.")))
				{
					PlayTestScenario.MoveHere(Player.LocalPlayer);
				}

				if (GUILayout.Button(new GUIContent("Spawn Lure Bot", "Spawn another Lure Bot in front of you, pointing the way you are looking.")))
				{
					PlayTestScenario.SpawnLureBot(Player.LocalPlayer);
				}

				if (GUILayout.Button(new GUIContent("Give Kit", "Rifles, magazines, binoculars and medkits.")))
				{
					PlayTestScenario.GiveKit(Player.LocalPlayer);
				}
			}

			if (isActive && PlayTestScenario.hasSite)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Base", PlayTestScenario.basePoint.ToString("0"));
				EditorGUILayout.LabelField("Start", PlayTestScenario.startPoint.ToString("0"));
			}
		}
	}
}
