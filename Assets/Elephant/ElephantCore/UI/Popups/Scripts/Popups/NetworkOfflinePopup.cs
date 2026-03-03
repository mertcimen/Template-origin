using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ElephantSDK
{
    public class NetworkOfflinePopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button retryButton;
        
        private Action onRetryCallback;

        public void Initialize(string content, string buttonLabel, Action onRetry)
        {
            Debug.Log("[NetworkOfflinePopup] Initializing");

            if (contentText != null)
            {
                contentText.text = HyperlinkUtils.CleanText(content);
                Debug.Log($"[NetworkOfflinePopup] Content set: {content}");
            }
            else
            {
                Debug.LogError("[NetworkOfflinePopup] contentText is null!");
            }

            if (retryButton != null)
            {
                var btnText = retryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = buttonLabel;
            }
            else
            {
                Debug.LogError("[NetworkOfflinePopup] retryButton is null!");
            }
            
            this.onRetryCallback = onRetry;
            SetupButton();
        }

        private void SetupButton()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClicked);
            }
        }

        private void OnRetryClicked()
        {
            Debug.Log("[NetworkOfflinePopup] Retry button clicked");
            onRetryCallback?.Invoke();
            Close();
        }

        public void Close()
        {
            Debug.Log("[NetworkOfflinePopup] Closing");
            ElephantPopupManager.Instance.CloseCurrentPopup();
        }
    }
}