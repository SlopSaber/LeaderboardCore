using System;
using HMUI;
using IPA.Utilities;
using Zenject;

namespace LeaderboardCore.Managers
{
	internal class MultiplayerLeaderboardManager : IInitializable, IDisposable
	{
		private readonly MainFlowCoordinator _mainFlowCoordinator;
		private readonly MultiplayerLobbyController _multiplayerLobbyController;
		private readonly ServerPlayerListViewController _serverPlayerListViewController;
		private readonly PlatformLeaderboardViewController _platformLeaderboardViewController;
		private readonly LevelSelectionNavigationController _levelSelectionNavigationController;

		public MultiplayerLeaderboardManager(MainFlowCoordinator mainFlowCoordinator, MultiplayerLobbyController multiplayerLobbyController, ServerPlayerListViewController serverPlayerListViewController, PlatformLeaderboardViewController platformLeaderboardViewController, LevelSelectionNavigationController levelSelectionNavigationController)
		{
			_mainFlowCoordinator = mainFlowCoordinator;
			_multiplayerLobbyController = multiplayerLobbyController;
			_serverPlayerListViewController = serverPlayerListViewController;
			_platformLeaderboardViewController = platformLeaderboardViewController;
			_levelSelectionNavigationController = levelSelectionNavigationController;
		}

		private void ShowLeaderboard(BeatmapLevel difficultyBeatmap)
		{
			if (!_multiplayerLobbyController.lobbyActivated)
				return;

			if (difficultyBeatmap is null)
			{
				HideLeaderboard();
				return;
			}

			// Preserve local behavior: Beat Saber 1.45 leaderboard data is populated elsewhere.

			_mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf().InvokeMethod<object, FlowCoordinator>("SetRightScreenViewController", _platformLeaderboardViewController, ViewController.AnimationType.In);
			_serverPlayerListViewController.DeactivateGameObject();
		}

		private void HideLeaderboard()
		{
			_mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf().InvokeMethod<object, FlowCoordinator>("SetRightScreenViewController", null, ViewController.AnimationType.Out);
		}

		private void LevelSelectionNavigationControllerOndidChangeLevelDetailContentEvent(LevelSelectionNavigationController levelSelectionNavigationController, StandardLevelDetailViewController.ContentType contentType)
		{
			if (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady)
			{
				ShowLeaderboard(levelSelectionNavigationController.beatmapLevel);
				return;
			}

			ShowLeaderboard(null);
		}

		private void LevelSelectionNavigationControllerOndidChangeDifficultyBeatmapEvent(LevelSelectionNavigationController levelSelectionNavigationController)
		{
			ShowLeaderboard(levelSelectionNavigationController.beatmapLevel);
		}

		public void Initialize()
		{
			// Preserve local behavior: avoid the difficulty-change callback on the 1.45 menu path.
			_levelSelectionNavigationController.didChangeLevelDetailContentEvent += LevelSelectionNavigationControllerOndidChangeLevelDetailContentEvent;
		}

		public void Dispose()
		{
			// Preserve local behavior: difficulty-change callback remains disabled.
			_levelSelectionNavigationController.didChangeLevelDetailContentEvent -= LevelSelectionNavigationControllerOndidChangeLevelDetailContentEvent;
		}
	}
}
