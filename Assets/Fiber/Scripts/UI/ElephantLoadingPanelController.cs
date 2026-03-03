using ElephantSDK;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fiber.UI
{
    public class ElephantLoadingPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image imgFillBar;
        [SerializeField] private GameObject loadingPanelParent;
        [Space]
        [SerializeField] private Image imgBackground;
        [SerializeField] private Image imgLoadingScreen;
        [SerializeField] private Image imgLoadingScreenTitle;
        [SerializeField] private float destroyDelayTime = 0.5f;

        private Coroutine _autoFillCoroutine;
        private float _lastDuration = 2.5f;
        private bool _keepAliveAcrossSceneLoad;
        private GameObject _persistentRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAutoAttach()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
                TryAttachToScene(activeScene);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryAttachToScene(scene);
        }

        private static void TryAttachToScene(Scene scene)
        {
            if (!HasElephantUi(scene))
                return;

            GameObject loadingRoot = FindByName(scene, "LoadingPanel") ?? FindByName(scene, "loader");
            if (loadingRoot == null)
                return;

            if (loadingRoot.GetComponent<ElephantLoadingPanelController>() == null)
                loadingRoot.AddComponent<ElephantLoadingPanelController>();
        }

        private static bool HasElephantUi(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].GetComponentInChildren<ElephantUI>(true) != null)
                    return true;
            }

            return false;
        }

        private static GameObject FindByName(Scene scene, string objectName)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindInChildren(roots[i].transform, objectName);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        private static Transform FindInChildren(Transform parent, string objectName)
        {
            if (parent.name == objectName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindInChildren(parent.GetChild(i), objectName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void Awake()
        {
            if (loadingPanelParent == null)
                loadingPanelParent = gameObject;

            if (imgFillBar == null)
                imgFillBar = ResolveFillImage();
            if (imgBackground == null)
                imgBackground = ResolveImageByName("img_Background");
            if (imgLoadingScreen == null)
                imgLoadingScreen = ResolveImageByName("img_LoadingScreen");
            if (imgLoadingScreenTitle == null)
                imgLoadingScreenTitle = ResolveImageByName("img_GameTitle");

            DisableLegacyLoadingControllers();
        }

        private void OnEnable()
        {
            ElephantUI.OnElephantLoadingStarted -= HandleLoadingStarted;

            ElephantUI.OnElephantLoadingStarted += HandleLoadingStarted;
        }

        private void OnDisable()
        {
            ElephantUI.OnElephantLoadingStarted -= HandleLoadingStarted;
            StopAutoFill();
        }

        private void HandleLoadingStarted(float duration)
        {
            StopAutoFill();
            SetFillAmount(0f);
            float extraDelay = Mathf.Max(0f, destroyDelayTime);
            _lastDuration = Mathf.Max(0f, duration) + extraDelay;

            if (loadingPanelParent != null)
                loadingPanelParent.SetActive(true);

            if (extraDelay > 0f)
                EnsurePersistentRoot();

            if (_lastDuration <= 0.0001f)
            {
                SetFillAmount(1f);
                FinalizeLoadingVisual();
                return;
            }

            _autoFillCoroutine = StartCoroutine(AutoFillRoutine(_lastDuration));
        }

        private IEnumerator AutoFillRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                SetFillAmount(normalized);
                yield return null;
            }

            SetFillAmount(1f);
            _autoFillCoroutine = null;
            FinalizeLoadingVisual();
        }

        private void StopAutoFill()
        {
            if (_autoFillCoroutine == null)
                return;

            StopCoroutine(_autoFillCoroutine);
            _autoFillCoroutine = null;
        }

        private void SetFillAmount(float amount)
        {
            if (imgFillBar != null)
                imgFillBar.fillAmount = Mathf.Clamp01(amount);
        }

        private void EnsurePersistentRoot()
        {
            if (_keepAliveAcrossSceneLoad)
                return;

            _persistentRoot = loadingPanelParent != null ? loadingPanelParent : gameObject;
            DontDestroyOnLoad(_persistentRoot);
            _keepAliveAcrossSceneLoad = true;
        }

        private void FinalizeLoadingVisual()
        {
            if (loadingPanelParent != null)
                loadingPanelParent.SetActive(false);

            if (!_keepAliveAcrossSceneLoad || _persistentRoot == null)
                return;

            GameObject rootToDestroy = _persistentRoot;
            _persistentRoot = null;
            _keepAliveAcrossSceneLoad = false;
            Destroy(rootToDestroy);
        }

        public void SetLoadingScreen(Sprite background, Sprite loadingScreen, Sprite loadingScreenTitle)
        {
            if (imgBackground != null)
                imgBackground.sprite = background;
            if (imgLoadingScreen != null)
                imgLoadingScreen.sprite = loadingScreen;
            if (imgLoadingScreenTitle != null)
                imgLoadingScreenTitle.sprite = loadingScreenTitle;
        }

        private void DisableLegacyLoadingControllers()
        {
            LoadingPanelController[] legacyControllers = GetComponentsInChildren<LoadingPanelController>(true);
            for (int i = 0; i < legacyControllers.Length; i++)
            {
                if (legacyControllers[i] != null)
                    legacyControllers[i].enabled = false;
            }
        }

        private Image ResolveFillImage()
        {
            Transform fillTransform = transform.Find("FillBar/Fill");
            if (fillTransform != null && fillTransform.TryGetComponent(out Image fillImage))
                return fillImage;

            Image[] allImages = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < allImages.Length; i++)
            {
                if (allImages[i] != null && allImages[i].type == Image.Type.Filled)
                    return allImages[i];
            }

            return null;
        }

        private Image ResolveImageByName(string objectName)
        {
            Transform target = FindInChildren(transform, objectName);
            if (target != null && target.TryGetComponent(out Image image))
                return image;

            return null;
        }
    }
}
