using UnityEngine;
using TMPro;
using System.Collections;

namespace ElephantSDK
{
    public class LoadingPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loadingSpinner;
        [SerializeField] private TextMeshProUGUI loadingText;

        private Coroutine _autoCloseRoutine;

        public void Initialize(string message = "Loading...", float timeoutSeconds = 5f)
        {
            Debug.Log("[LoadingPopup] Showing loading");

            if (loadingSpinner != null)
            {
                loadingSpinner.SetActive(true);
            }

            if (loadingText != null)
            {
                loadingText.text = message;
            }
            
            if (timeoutSeconds > 0f)
            {
                if (_autoCloseRoutine != null)
                {
                    StopCoroutine(_autoCloseRoutine);
                }
                _autoCloseRoutine = StartCoroutine(AutoCloseAfterDelay(timeoutSeconds));
            }
        }

        private IEnumerator AutoCloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"[LoadingPopup] Auto-closing after {delay} seconds");
            Close();
        }

        public void Close()
        {
            if (_autoCloseRoutine != null)
            {
                StopCoroutine(_autoCloseRoutine);
                _autoCloseRoutine = null;
            }

            Debug.Log("[LoadingPopup] Closing");
            ElephantPopupManager.Instance.CloseCurrentPopup();
        }

        private void OnDisable()
        {
            if (_autoCloseRoutine != null)
            {
                StopCoroutine(_autoCloseRoutine);
                _autoCloseRoutine = null;
            }
        }
    }
}