using HarmonyLib;
using System;
using System.Reflection;

namespace LeaderboardCore.HarmonyPatches
{
	[HarmonyPatch]
    internal class PanelView_SetIsLoaded
    {
		public static event Action<bool> IsLoadedChanged;
		private static MethodBase TargetMethod() =>
			Plugin.Instance.scoreSaber.Assembly
				.GetType("ScoreSaber.Features.Leaderboards.UI.PanelView")
				.GetMethod("set_isLoaded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		private static void Postfix(bool value)
        {
			IsLoadedChanged?.Invoke(value);
        }
	}
}
