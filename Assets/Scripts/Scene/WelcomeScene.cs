#nullable enable
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using TMPro;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    [RequireComponent(typeof (Animator))]
    public class WelcomeScene : MonoBehaviour
    {
        [SerializeField] TMP_Text? VersionText, DownloadInfoText;
        [SerializeField] private VersionHolder? VersionHolder;
        [SerializeField] private LocalizedStringTable? L10nTable;
        [SerializeField] Animator? CubeAnimator;
        [SerializeField] Button? EnterButton, DownloadButton, ReplayButton;
        [SerializeField] Button? ResourcePacksButton;

        [SerializeField] WelcomeScreen? WelcomeScreen;

        private static WelcomeScene? instance;
        public static WelcomeScene Instance
        {
            get {
                if (instance == null)
                    instance = Component.FindFirstObjectByType<WelcomeScene>();

                return instance!;
            }
        }

        private Animator? downloadButtonAnimator;
        private bool downloadingRes = false;

        public static string GetL10nString(string key, params object[] p)
        {
            var str = Instance.L10nTable!.GetTable().GetEntry(key);
            if (str == null) return $"<{key}>";
            return string.Format(str.Value, p);
        }

        private void UpdateSelectedVersion()
        {
            if (VersionHolder == null) return;

            var verIndex = VersionHolder.SelectedVersion;
            var version = VersionHolder.Versions[verIndex];

            if (VersionText != null)
                VersionText.text = $"< {version.Name} >";
            
            var newResPath = PathHelper.GetPackFile($"vanilla-{version.ResourceVersion}", "pack.mcmeta");
            var resPresent = File.Exists(newResPath);

            downloadButtonAnimator!.SetBool("Hidden", resPresent);

            if (EnterButton != null)
                EnterButton.interactable = resPresent;
            
            if (ReplayButton != null)
                ReplayButton.interactable = resPresent;
            
            if (ResourcePacksButton != null)
                ResourcePacksButton.interactable = resPresent;
        }

        void Start()
        {
            if (VersionHolder == null || DownloadButton == null || DownloadInfoText == null) return;

            downloadButtonAnimator = DownloadButton.GetComponent<Animator>();
            DownloadInfoText.text = $"v{Application.version}";

            if (VersionHolder.Versions.Length <= 0)
                return;
            
            VersionHolder.SelectedVersion = 0;
            
            UpdateSelectedVersion();
        }

        public string GetResourceVersion()
        {
            return VersionHolder!.Versions[VersionHolder.SelectedVersion].ResourceVersion;
        }

        public int GetResPackFormatInt()
        {
            return VersionHolder!.Versions[VersionHolder.SelectedVersion].ResPackFormatInt;
        }

        public void PrevVersion()
        {
            if (VersionHolder == null || downloadingRes) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion - 1 + count) % count;
            
            UpdateSelectedVersion();

            CubeAnimator!.SetTrigger("Left");
        }

        public void NextVersion()
        {
            if (VersionHolder == null || downloadingRes) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion + 1) % count;
            
            UpdateSelectedVersion();

            CubeAnimator!.SetTrigger("Right");
        }

        public void NextLanguage()
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            var selected = LocalizationSettings.SelectedLocale;
            var selectedIndex = locales.IndexOf(selected);
            if (selectedIndex >= 0)
                LocalizationSettings.SelectedLocale = locales[(selectedIndex + 1) % locales.Count];

            Debug.Log($"Locale changed to {LocalizationSettings.SelectedLocale}");
        }

        public void DownloadResource()
        {
            if (VersionHolder == null || downloadingRes) return;

            var verIndex = VersionHolder.SelectedVersion;
            var version = VersionHolder.Versions[verIndex];

            downloadingRes = true;
            StartCoroutine(ResourceDownloader.DownloadResource(version.ResourceVersion,
                    (status) => DownloadInfoText!.text = GetL10nString(status),
                    () => downloadButtonAnimator!.SetBool("Hidden", true),
                    (succeeded) => {
                        downloadButtonAnimator!.SetBool("Hidden", succeeded);
                        downloadingRes = false;

                        DownloadInfoText!.text = succeeded ? $"v{Application.version}" :
                                GetL10nString("status.error.download_resource_failure", version.ResourceVersion);

                        // Refresh buttons
                        UpdateSelectedVersion();
                    }));
        }

        private IEnumerator MarkovCoroutine()
        {
            if (WelcomeScreen != null)
            {
                WelcomeScreen.SetEnterGameTrigger();
            }

            yield return new WaitForSecondsRealtime(0.32F);

            var op = SceneManager.LoadSceneAsync("Scenes/Generation", LoadSceneMode.Single);

            while (op.progress < 0.9F)
                yield return null;
        }

        public void EnterMarkov()
        {
            if (VersionHolder == null || downloadingRes) return;

            StartCoroutine(MarkovCoroutine());
        }

        private IEnumerator ReplayCoroutine()
        {
            GetComponent<Animator>().SetTrigger("Enter");

            yield return new WaitForSecondsRealtime(0.32F);

            var op = SceneManager.LoadSceneAsync("Scenes/Replay", LoadSceneMode.Single);

            while (op.progress < 0.9F)
                yield return null;
        }

        public void EnterReplay()
        {
            if (VersionHolder == null || downloadingRes) return;

            StartCoroutine(ReplayCoroutine());
        }

        public void QuitApp()
        {
            Application.Quit();
        }
    }
}