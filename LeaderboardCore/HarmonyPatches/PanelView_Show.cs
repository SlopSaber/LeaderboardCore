using HarmonyLib;
using System;
using System.Reflection;

namespace LeaderboardCore.HarmonyPatches
{
    [HarmonyPatch]
    internal class PanelView_Show
    {
		public static event Action ViewActivated;
		private static MethodBase TargetMethod() =>
			Plugin.Instance.scoreSaber.Assembly
				.GetType("ScoreSaber.Features.Leaderboards.UI.PanelView")
				.GetMethod("DidActivate", BindingFlags.Instance | BindingFlags.Public);

		private static void Postfix()
		{
			ViewActivated?.Invoke();
		}
	}
}
