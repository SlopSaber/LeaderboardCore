#nullable enable
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using LeaderboardCore.Interfaces;
using LeaderboardCore.Models;
using System;
using System.Collections.Generic;
using LeaderboardCore.Configuration;
using UnityEngine;
using Zenject;
using System.Linq;
using LeaderboardCore.Utilities;

namespace LeaderboardCore.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\LeaderboardNavigationButtons.bsml")]
    [ViewDefinition("LeaderboardCore.UI.Views.LeaderboardNavigationButtons.bsml")]
    internal class LeaderboardNavigationButtonsController : BSMLAutomaticViewController, IInitializable, IDisposable, INotifyLeaderboardSet, INotifyLeaderboardLoad, INotifyCustomLeaderboardsChange
    {
        [Inject]
        private readonly PlatformLeaderboardViewController platformLeaderboardViewController = null!;

        [Inject]
        private readonly PluginConfig pluginConfig = null!;

        [InjectOptional]
        private readonly ScoreSaberCustomLeaderboard? scoreSaberCustomLeaderboard = null!;

        private FloatingScreen? buttonsFloatingScreen;
        private FloatingScreen? customPanelFloatingScreen;

        private Transform? containerTransform;
        private Vector3 containerPosition;
        private Vector3 hiddenPosition;

        private BeatmapKey? selectedLevelKey;

        private readonly List<CustomLeaderboard> orderedCustomLeaderboardsCache = new List<CustomLeaderboard>();
        private readonly List<CustomLeaderboard> orderedCustomLeaderboards = new List<CustomLeaderboard>();
        private readonly Dictionary<string, CustomLeaderboard> customLeaderboardsById = new Dictionary<string, CustomLeaderboard>();
        private int currentIndex;

        public void Initialize()
        {
            buttonsFloatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(120f, 25f), false, Vector3.zero, Quaternion.identity);

            var buttonsFloatingScreenTransform = buttonsFloatingScreen.transform;
            buttonsFloatingScreenTransform.SetParent(platformLeaderboardViewController.transform);
            buttonsFloatingScreenTransform.localPosition = new Vector3(3f, 50f);
            buttonsFloatingScreenTransform.localScale = Vector3.one;

            var buttonsFloatingScreenGO = buttonsFloatingScreen.gameObject;
            buttonsFloatingScreenGO.SetActive(false);
            buttonsFloatingScreenGO.SetActive(true);
            buttonsFloatingScreenGO.name = "LeaderboardNavigationButtonsPanel";

            customPanelFloatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 25f), false, Vector3.zero, Quaternion.identity);

            var customFloatingScreenTransform = customPanelFloatingScreen.transform;
            customFloatingScreenTransform.SetParent(platformLeaderboardViewController.transform);
            customFloatingScreenTransform.localPosition = new Vector3(3f, 50f);
            customFloatingScreenTransform.localScale = Vector3.one;

            var customFloatingScreenGO = customPanelFloatingScreen.gameObject;
            customFloatingScreenGO.SetActive(false);
            customFloatingScreenGO.SetActive(true);
            customFloatingScreenGO.name = "CustomLeaderboardPanel";

            platformLeaderboardViewController.didActivateEvent += OnLeaderboardActivated;
            platformLeaderboardViewController.didDeactivateEvent += OnLeaderboardDeactivated;
        }

        public void Dispose()
        {
            if (buttonsFloatingScreen != null && buttonsFloatingScreen.gameObject != null)
            {
                Destroy(buttonsFloatingScreen.gameObject);
            }

            if (customPanelFloatingScreen != null && customPanelFloatingScreen.gameObject != null)
            {
                Destroy(customPanelFloatingScreen.gameObject);
            }

            platformLeaderboardViewController.didActivateEvent -= OnLeaderboardActivated;
            platformLeaderboardViewController.didDeactivateEvent -= OnLeaderboardDeactivated;
        }

        public void OnEnable()
        {
            if (containerTransform == null)
            {
                containerTransform = platformLeaderboardViewController.transform.Find("Container");
                containerPosition = containerTransform.localPosition;
                hiddenPosition = new Vector3(-999, -999, -999);
            }

            if (!isActivated)
            {
                OnLeaderboardLoaded();
            }
        }

        private void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
        {
            buttonsFloatingScreen!.gameObject.SetActive(true);
            customPanelFloatingScreen!.gameObject.SetActive(true);
            buttonsFloatingScreen!.SetRootViewController(this, AnimationType.None);
            OnLeaderboardLoaded();

            if (currentIndex > 0 && currentIndex <= orderedCustomLeaderboards.Count)
            {
                var currentLeaderboard = orderedCustomLeaderboards[currentIndex - 1];
                if (!currentLeaderboard.IsActive)
                {
                    currentLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);
                }
            }
        }

        private void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            buttonsFloatingScreen?.gameObject.SetActive(false);
            customPanelFloatingScreen?.gameObject.SetActive(false);

            if (currentIndex > 0 && currentIndex <= orderedCustomLeaderboards.Count)
            {
                orderedCustomLeaderboards[currentIndex - 1].Hide(customPanelFloatingScreen!);
            }
        }

        public void OnLeaderboardSet(BeatmapKey selectedKey)
        {
            selectedLevelKey = selectedKey;
            OnLeaderboardLoaded();
        }

        public void OnLeaderboardLoaded(bool loaded = true)
        {
            if (!platformLeaderboardViewController.isActivated || (!isActiveAndEnabled && !isActivated))
                return;

            if (selectedLevelKey != null) {
                this.orderedCustomLeaderboards.Clear();
                this.orderedCustomLeaderboards.AddRange(
                    this.orderedCustomLeaderboardsCache
                    .Where(ld => ld.ShowForLevel(selectedLevelKey))
                    .ToList());
            }

            // If not loaded or leaderboard removed
            if (pluginConfig.LastLeaderboard != null && 
                ((!customLeaderboardsById.ContainsKey(pluginConfig.LastLeaderboard) && currentIndex != 0) ||
                customLeaderboardsById.ContainsKey(pluginConfig.LastLeaderboard) && 
                !customLeaderboardsById[pluginConfig.LastLeaderboard ?? ""].ShowForLevel(selectedLevelKey)))
            {
                SwitchToDefault();
            }
            else if (customLeaderboardsById.ContainsKey(pluginConfig.LastLeaderboard ?? "") && currentIndex == 0)
            {
                SwitchToLastLeaderboard();
            }
            else if (currentIndex > 0)
            {
                currentIndex = orderedCustomLeaderboards.FindIndex(leaderboard => leaderboard.LeaderboardId == pluginConfig.LastLeaderboard) + 1;
            }

            // A saved leaderboard can become unavailable for the selected map.
            // Apply the fallback in this notification, even after SwitchToDefault.
            if (currentIndex == 0 && !ShowDefaultLeaderboard)
            {
                RightButtonClick();
            }

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        private void SwitchToDefault(CustomLeaderboard? lastLeaderboard = null)
        {
            if (containerTransform != null && containerTransform.localPosition == hiddenPosition)
            {
                lastLeaderboard ??= customLeaderboardsById.TryGetValue(pluginConfig.LastLeaderboard ?? "", out var outLastLeaderboard)
                    ? outLastLeaderboard
                    : null;
                lastLeaderboard?.Hide(customPanelFloatingScreen);
                UnYeetDefault();
            }

            currentIndex = 0;
        }

        private void SwitchToIndex(int index, CustomLeaderboard? lastLeaderboard = null)
        {
            if (index == 0 || index > orderedCustomLeaderboards.Count)
            {
                SwitchToDefault(lastLeaderboard);
            }
            else
            {
                lastLeaderboard ??= customLeaderboardsById.TryGetValue(pluginConfig.LastLeaderboard ?? "", out var outLastLeaderboard)
                    ? outLastLeaderboard
                    : null;
                lastLeaderboard?.Hide(customPanelFloatingScreen);

                currentIndex = index;
                var currentLeaderboard = orderedCustomLeaderboards[currentIndex - 1];
                pluginConfig.LastLeaderboard = currentLeaderboard.LeaderboardId;
                YeetDefault();
                currentLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);
            }
        }

        private void SwitchToLastLeaderboard()
        {
            if (customLeaderboardsById.TryGetValue(pluginConfig.LastLeaderboard ?? "", out var lastLeaderboard))
            {
                var lastLeaderboardIndex = orderedCustomLeaderboards.IndexOf(lastLeaderboard);
                if (lastLeaderboardIndex < 0)
                {
                    SwitchToDefault(lastLeaderboard);
                    return;
                }

                currentIndex = lastLeaderboardIndex + 1;
                YeetDefault();
                lastLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);
            }
        }

        private void YeetDefault()
        {
            if (containerTransform != null && containerTransform.localPosition != hiddenPosition)
            {
                containerTransform.localPosition = hiddenPosition;
            }
            scoreSaberCustomLeaderboard?.YeetSS();
        }

        private void UnYeetDefault()
        {
            if (containerTransform != null && containerTransform.localPosition != containerPosition)
            {
                containerTransform.localPosition = containerPosition;
            }
            scoreSaberCustomLeaderboard?.UnYeetSS();
        }

        [UIAction("left-button-click")]
        private void LeftButtonClick()
        {
            if (currentIndex <= 0 || (!ShowDefaultLeaderboard && currentIndex == 1) || pluginConfig.LastLeaderboard == null)
            {
                return;
            }

            if (customLeaderboardsById.TryGetValue(pluginConfig.LastLeaderboard, out var outLastLeaderboard))
            {
                outLastLeaderboard.Hide(customPanelFloatingScreen);
            }

            currentIndex--;

            if (currentIndex == 0)
            {
                UnYeetDefault();
                pluginConfig.LastLeaderboard = null;
            }
            else
            {
                var lastLeaderboard = orderedCustomLeaderboards[currentIndex - 1];
                pluginConfig.LastLeaderboard = lastLeaderboard.LeaderboardId;
                lastLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);
            }

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        [UIAction("right-button-click")]
        private void RightButtonClick() => RightButtonClick(null);

        private void RightButtonClick(CustomLeaderboard? lastLeaderboard)
        {
            if (currentIndex >= orderedCustomLeaderboards.Count)
            {
                return;
            }

            if (currentIndex == 0)
            {
                YeetDefault();
            }
            else
            {
                lastLeaderboard ??= customLeaderboardsById.TryGetValue(pluginConfig.LastLeaderboard ?? "", out var outLastLeaderboard)
                    ? outLastLeaderboard
                    : null;
                lastLeaderboard?.Hide(customPanelFloatingScreen);
            }

            currentIndex++;
            var currentLeaderboard = orderedCustomLeaderboards[currentIndex - 1];
            pluginConfig.LastLeaderboard = currentLeaderboard.LeaderboardId;
            currentLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        public void OnLeaderboardsChanged(IEnumerable<CustomLeaderboard> orderedCustomLeaderboards, Dictionary<string, CustomLeaderboard> customLeaderboardsById)
        {
            this.orderedCustomLeaderboardsCache.Clear();
            this.orderedCustomLeaderboardsCache.AddRange(orderedCustomLeaderboards);

            this.orderedCustomLeaderboards.Clear();
            if (selectedLevelKey != null) {
                this.orderedCustomLeaderboards.AddRange(
                    this.orderedCustomLeaderboardsCache
                    .Where(ld => ld.ShowForLevel(selectedLevelKey))
                    .ToList());
            } else {
                this.orderedCustomLeaderboards.AddRange(orderedCustomLeaderboards);
            }

            // I hate how this library is so scuffed and really hope scoresaber uses it instead of having to do this
            // So this piece of scuffed code takes the last leaderboard if it was part of the current list and gives it for switching out
            var lastLeaderboard = this.customLeaderboardsById.TryGetValue(pluginConfig.LastLeaderboard ?? "", out var outLastLeaderboard) ? outLastLeaderboard : null;

            this.customLeaderboardsById.Clear();
            foreach (var customLeaderboard in customLeaderboardsById)
            {
                this.customLeaderboardsById[customLeaderboard.Key] = customLeaderboard.Value;
            }

            if (this == null || !platformLeaderboardViewController.isActivated || (!isActiveAndEnabled && !isActivated))
                return;

            // We only want to display the default leaderboard if there's no other custom leaderboards
            if (this.orderedCustomLeaderboards.Count == 0)
            {
                SwitchToDefault(lastLeaderboard);
            }
            else if (pluginConfig.LastLeaderboard != null && currentIndex != 0 &&
                !this.orderedCustomLeaderboards.Any(leaderboard => leaderboard.LeaderboardId == pluginConfig.LastLeaderboard))
            {
                SwitchToIndex(1, lastLeaderboard);
            }
            else if (
                customLeaderboardsById.ContainsKey(pluginConfig.LastLeaderboard ?? "") && 
                currentIndex == 0)
            {
                if (selectedLevelKey != null && customLeaderboardsById[pluginConfig.LastLeaderboard ?? ""].ShowForLevel(selectedLevelKey)) {
                    SwitchToLastLeaderboard();
                } else {
                    SwitchToDefault(lastLeaderboard);
                }
            }
            else if (currentIndex > 0)
            {
                currentIndex = this.orderedCustomLeaderboards.FindIndex(leaderboard => leaderboard.LeaderboardId == pluginConfig.LastLeaderboard) + 1;
            }

            if (currentIndex == 0 && !ShowDefaultLeaderboard)
            {
                RightButtonClick(lastLeaderboard);
            }

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        [UIValue("left-button-active")]
        private bool LeftButtonActive => currentIndex > 0 && (ShowDefaultLeaderboard || currentIndex > 1);

        [UIValue("right-button-active")]
        private bool RightButtonActive => currentIndex < orderedCustomLeaderboards.Count;

        // Legacy ScoreSaber replaces the platform page. Current ScoreSaber
        // registers a separate custom page, so its presence alone must not keep
        // the empty platform page selectable on custom maps. Check all registered
        // pages, not just those that support the current map.
        private bool HasLegacyScoreSaber =>
            scoreSaberCustomLeaderboard != null &&
            !orderedCustomLeaderboardsCache.Any(leaderboard => leaderboard.pluginId == "ScoreSaber");

        private bool ShowDefaultLeaderboard =>
            HasLegacyScoreSaber ||
            !(selectedLevelKey?.levelId.Contains(Constants.CustomLevelPrefix) ?? false) || 
            orderedCustomLeaderboards.Count == 0;
    }
}
