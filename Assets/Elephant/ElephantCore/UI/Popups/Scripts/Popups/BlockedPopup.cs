using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ElephantSDK
{
    public class BlockedPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private Button actionButton;
        
        private Action onActionCallback;

        public void Initialize(string content, string warning,
                              string buttonLabel, Action onAction)
        {
            Debug.Log("[BLOCKEDPopup] Initializing...");

            if (contentText != null)
            {
                contentText.text = HyperlinkUtils.CleanText(content);
                Debug.Log($"[BLOCKEDPopup] Content set: {content.Substring(0, Mathf.Min(50, content.Length))}...");
            }
            else
            {
                Debug.LogError("[BLOCKEDPopup] contentText is null!");
            }

            if (warningText != null)
            {
                warningText.text = warning;
                Debug.Log($"[BLOCKEDPopup] Warning set: {warning}");
            }
            else
            {
                Debug.LogError("[BLOCKEDPopup] warningText is null!");
            }
            
            if (actionButton != null)
            {
                var btnText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = buttonLabel;
            }
            
            this.onActionCallback = onAction;

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnActionClicked);
            }
        }

        private void OnActionClicked()
        {
            Debug.Log("[BLOCKEDPopup] Action button clicked");
            onActionCallback?.Invoke();
            Close();
        }

        public void Close()
        {
            Debug.Log("[BLOCKEDPopup] Closing");
            ElephantPopupManager.Instance.CloseCurrentPopup();
        }
    }
}