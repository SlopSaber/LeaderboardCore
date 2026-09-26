using System.Collections;
using BeatSaberMarkupLanguage.FloatingScreen;
using HMUI;
using LeaderboardCore.Utilities;
using UnityEngine;

namespace LeaderboardCore.Models
{
    /// <summary>
    /// Abstract class for the Custom Leaderboard. Must provide the panel and leaderboard views.
    /// </summary>
    public abstract class CustomLeaderboard
    {
        /// <summary>
        /// The ViewController for the leaderboard's panel.
        /// </summary>
        protected abstract ViewController panelViewController { get; }

        /// <summary>
        /// The ViewController for the leaderboard itself.
        /// </summary>
        protected abstract ViewController leaderboardViewController { get; }

        /// <summary>
        /// The ID for the leaderboard.
        /// Must be a unique string if the mod has multiple leaderboards.
        /// </summary>
        protected virtual string leaderboardId { get; } = "";

        /// <summary>
        /// Should your leaderboard show for a specific level.
        /// Default is to show for all custom maps.
        /// </summary>
        public virtual bool ShowForLevel(BeatmapKey? beatmapKey) {
            // Preserve local behavior: custom leaderboards remain available for every selected level.
            return true;
        }

        internal string pluginId;

        internal string LeaderboardId => $"{pluginId}{leaderboardId}";

        internal bool IsActive => leaderboardViewController != null && leaderboardViewController.isActivated;

        private Coroutine pendingShow;
        private SharedCoroutineStarter pendingShowStarter;

        internal void Show(FloatingScreen panelScreen, Vector3 leaderboardPosition, PlatformLeaderboardViewController platformLeaderboardViewController)
        {
            if (!platformLeaderboardViewController.isActivated)
            {
                return;
            }

            if (!panelScreen.gameObject.activeSelf)
            {
                panelScreen.gameObject.SetActive(true);
            }

            if (!panelScreen.isActiveAndEnabled || platformLeaderboardViewController.screen == null)
            {
                if (pendingShow == null)
                {
                    pendingShowStarter = SharedCoroutineStarter.instance;
                    if (pendingShowStarter != null)
                    {
                        pendingShow = pendingShowStarter.StartCoroutine(WaitForScreen(panelScreen, leaderboardPosition,
                            platformLeaderboardViewController));
                    }
                }
                return;
            }

            StopPendingShow();

            if (panelViewController != null)
            {
                panelScreen.SetRootViewController(panelViewController, ViewController.AnimationType.None);
                if (!panelViewController.isActiveAndEnabled)
                {
                    panelViewController.gameObject.SetActive(true);
                }
            }
            else
            {
                panelScreen.SetRootViewController(null, ViewController.AnimationType.None);
            }

            if (leaderboardViewController != null)
            {
                // Parent before initialization and activation. A first-time view has no
                // screen hierarchy yet; activating it first leaves its content at the
                // root until a later navigation click reuses the already-parented view.
                leaderboardViewController.transform.SetParent(platformLeaderboardViewController.transform, false);
                leaderboardViewController.transform.localPosition = leaderboardPosition;

                if (leaderboardViewController.screen == null)
                {
                    leaderboardViewController.__Init(platformLeaderboardViewController.screen, platformLeaderboardViewController, null);
                }

                leaderboardViewController.__Activate(false, false);
            }
        }

        private IEnumerator WaitForScreen(FloatingScreen panelScreen, Vector3 leaderboardPosition,
            PlatformLeaderboardViewController platformLeaderboardViewController)
        {
            while (!panelScreen.isActiveAndEnabled || platformLeaderboardViewController.screen == null)
            {
                if (!platformLeaderboardViewController.isActivated)
                {
                    pendingShow = null;
                    pendingShowStarter = null;
                    yield break;
                }
                yield return null;
            }
            pendingShow = null;
            pendingShowStarter = null;
            Show(panelScreen, leaderboardPosition, platformLeaderboardViewController);
        }

        private void StopPendingShow()
        {
            if (pendingShow != null && pendingShowStarter != null)
            {
                pendingShowStarter.StopCoroutine(pendingShow);
            }
            pendingShow = null;
            pendingShowStarter = null;
        }

        internal void Hide(FloatingScreen panelScreen)
        {
            StopPendingShow();
            if (panelScreen != null)
            {
                if (panelScreen.isActiveAndEnabled)
                {
                    panelScreen.SetRootViewController(null, ViewController.AnimationType.None);
                }
                else
                {
                    panelViewController.DeactivateGameObject();
                }
            }

            if (leaderboardViewController != null)
            {
                leaderboardViewController.__Deactivate(false, true, false);
            }
        }
    }
}
