using HarmonyLib;
using HMUI;
using System;
using System.Reflection;

namespace LeaderboardCore.HarmonyPatches
{
    [HarmonyPatch]
    internal class PanelView_SetPrompt
    {
		public static event Action ScoreUploaded;
		private static MethodBase TargetMethod() =>
			Plugin.Instance.scoreSaber.Assembly
				.GetType("ScoreSaber.Features.Leaderboards.UI.PanelView")
				.GetMethod("SetPrompt", BindingFlags.Instance | BindingFlags.Public);

		private static void Postfix(string status)
		{
			if (string.IsNullOrEmpty(status) || !status.Contains("Score uploaded"))
				return;

			ScoreUploaded?.Invoke();
		}
	}
}
