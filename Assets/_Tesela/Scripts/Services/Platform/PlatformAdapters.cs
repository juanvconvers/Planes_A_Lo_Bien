using System;
using System.Collections;
using Tesela.Core;
using UnityEngine;

namespace Tesela.Services
{
#if TESELA_PGS
    /// <summary>Google Play Games v2: inicio de sesión, clasificaciones y logros. Sin verificar en editor.</summary>
    public sealed class PlayGamesService : IPlatformGames
    {
        private readonly ExternalIds _ids;
        public PlayGamesService(ExternalIds ids) { _ids = ids; GooglePlayGames.PlayGamesPlatform.Activate(); }

        public bool IsSignedIn { get; private set; }

        public void SignIn(Action<bool> onDone)
        {
            GooglePlayGames.PlayGamesPlatform.Instance.Authenticate(status =>
            {
                IsSignedIn = status == GooglePlayGames.BasicApi.SignInStatus.Success;
                onDone?.Invoke(IsSignedIn);
            });
        }

        private string Board(string key) => key == "daily" ? _ids.leaderboardDaily : _ids.leaderboardClassic;

        public void SubmitScore(string leaderboardKey, long score)
        {
            if (IsSignedIn && ExternalIds.IsSet(Board(leaderboardKey))) Social.ReportScore(score, Board(leaderboardKey), _ => { });
        }

        public void UnlockAchievement(string internalId)
        {
            string id = _ids.AchievementId(internalId);
            if (IsSignedIn && id != null) Social.ReportProgress(id, 100.0, _ => { });
        }

        public void ShowLeaderboard(string leaderboardKey)
        {
            if (IsSignedIn) GooglePlayGames.PlayGamesPlatform.Instance.ShowLeaderboardUI(Board(leaderboardKey));
        }

        // El guardado en nube de Play Games requiere la API de partidas guardadas; queda pendiente (QA_PENDIENTE.md).
        public void CloudSave(string data, Action<bool> onDone) => onDone?.Invoke(false);
        public void CloudLoad(Action<string> onLoaded) => onLoaded?.Invoke(null);
    }
#endif

    /// <summary>Notificaciones locales con permiso explícito (POST_NOTIFICATIONS en Android 13+).</summary>
    public sealed class AndroidLocalNotifications : INotificationScheduler
    {
        private const string Channel = "tesela_daily";

        public void RequestPermission(Action<bool> onDone)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string permission = "android.permission.POST_NOTIFICATIONS";
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission)) { onDone?.Invoke(true); return; }
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += _ => onDone?.Invoke(true);
            callbacks.PermissionDenied += _ => onDone?.Invoke(false);
            UnityEngine.Android.Permission.RequestUserPermission(permission, callbacks);
#else
            onDone?.Invoke(false);
#endif
        }

        public void ScheduleAt(DateTime localTime, string title, string body)
        {
#if TESELA_NOTIFICATIONS && UNITY_ANDROID
            Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(
                new Unity.Notifications.Android.AndroidNotificationChannel(Channel, "Tesela", "Reto diario y racha", Unity.Notifications.Android.Importance.Default));
            Unity.Notifications.Android.AndroidNotificationCenter.CancelAllScheduledNotifications();
            Unity.Notifications.Android.AndroidNotificationCenter.SendNotification(
                new Unity.Notifications.Android.AndroidNotification(title, body, localTime), Channel);
#endif
        }

        public void CancelAll()
        {
#if TESELA_NOTIFICATIONS && UNITY_ANDROID
            Unity.Notifications.Android.AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif
        }
    }

    /// <summary>In-App Review (solo tras un momento positivo) e In-App Updates flexibles.</summary>
    public sealed class PlayReviewService : IReviewService
    {
        private readonly MonoBehaviour _runner;
        public PlayReviewService(MonoBehaviour runner) { _runner = runner; }

        public void RequestReview() => _runner.StartCoroutine(Review());
        public void CheckForUpdate() => _runner.StartCoroutine(UpdateFlow());

        private IEnumerator Review()
        {
#if TESELA_PLAYREVIEW
            var manager = new Google.Play.Review.ReviewManager();
            var request = manager.RequestReviewFlow();
            yield return request;
            if (request.Error != Google.Play.Review.ReviewErrorCode.NoError) yield break;
            yield return manager.LaunchReviewFlow(request.GetResult());
#else
            yield break;
#endif
        }

        private IEnumerator UpdateFlow()
        {
#if TESELA_PLAYUPDATE
            var manager = new Google.Play.AppUpdate.AppUpdateManager();
            var info = manager.GetAppUpdateInfo();
            yield return info;
            if (!info.IsSuccessful) yield break;
            var result = info.GetResult();
            if (result.UpdateAvailability != Google.Play.AppUpdate.UpdateAvailability.UpdateAvailable) yield break;
            yield return manager.StartUpdate(result, Google.Play.AppUpdate.AppUpdateOptions.FlexibleAppUpdateOptions());
#else
            yield break;
#endif
        }
    }
}
