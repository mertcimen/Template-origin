#if UNITY_EDITOR
using System;
using GameAnalyticsSDK.Setup;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameAnalyticsBuildVersionAutoSync
{
    private const string SettingsAssetPath = "Assets/Resources/GameAnalytics/Settings.asset";
    private const string SessionKey = "GameAnalyticsBuildVersionAutoSync.LastBundleVersion";
    private const double PollIntervalSeconds = 1.0d;

    private static bool _isSyncing;
    private static double _nextPollTime;

    static GameAnalyticsBuildVersionAutoSync()
    {
        EditorApplication.delayCall += ForceSync;
        EditorApplication.update += Poll;
    }

    [MenuItem("Tools/GameAnalytics/Sync Build Versions From Player Settings")]
    private static void ForceSync()
    {
        TrySyncBuildVersions(forceAllByBundleVersion: true);
    }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        double now = EditorApplication.timeSinceStartup;
        if (now < _nextPollTime)
            return;
        _nextPollTime = now + PollIntervalSeconds;

        string currentBundleVersion = PlayerSettings.bundleVersion ?? string.Empty;
        string lastBundleVersion = SessionState.GetString(SessionKey, string.Empty);
        bool versionChanged = !string.Equals(currentBundleVersion, lastBundleVersion, StringComparison.Ordinal);

        if (versionChanged)
        {
            TrySyncBuildVersions(forceAllByBundleVersion: true);
            return;
        }

        // Version aynı kalsa bile platform eklenirse yeni satırları senkronize et.
        TrySyncBuildVersions(forceAllByBundleVersion: false);
    }

    private static void TrySyncBuildVersions(bool forceAllByBundleVersion)
    {
        if (_isSyncing)
            return;
        if (!TryGetSettingsAsset(out Settings settings))
            return;

        _isSyncing = true;
        try
        {
            string bundleVersion = PlayerSettings.bundleVersion ?? string.Empty;
            bool changed = false;

            int platformCount = settings.Platforms != null ? settings.Platforms.Count : 0;

            if (settings.Build == null)
            {
                settings.Build = new System.Collections.Generic.List<string>();
                changed = true;
            }

            while (settings.Build.Count < platformCount)
            {
                settings.Build.Add(bundleVersion);
                changed = true;
            }

            while (settings.Build.Count > platformCount)
            {
                settings.Build.RemoveAt(settings.Build.Count - 1);
                changed = true;
            }

            if (forceAllByBundleVersion)
            {
                for (int i = 0; i < settings.Build.Count; i++)
                {
                    if (string.Equals(settings.Build[i], bundleVersion, StringComparison.Ordinal))
                        continue;

                    settings.Build[i] = bundleVersion;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }

            SessionState.SetString(SessionKey, bundleVersion);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private static bool TryGetSettingsAsset(out Settings settings)
    {
        settings = AssetDatabase.LoadAssetAtPath<Settings>(SettingsAssetPath);
        if (settings != null)
            return true;

        string[] guids = AssetDatabase.FindAssets("t:Settings", new[] { "Assets/Resources/GameAnalytics", "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Settings candidate = AssetDatabase.LoadAssetAtPath<Settings>(path);
            if (candidate == null)
                continue;

            settings = candidate;
            return true;
        }

        return false;
    }
}
#endif
