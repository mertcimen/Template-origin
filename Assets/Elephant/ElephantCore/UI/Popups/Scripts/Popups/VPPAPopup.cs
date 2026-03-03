using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ElephantSDK
{
    public class VPPAPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button acceptButton;
        
        private Action onAcceptCallback;

        public void Initialize(string content,
                              string acceptButtonLabel, Action onAccept)
        {
            Debug.Log("[TOSPopup] Initializing...");

            if (contentText != null)
            {
                contentText.text = HyperlinkUtils.CleanText(content);
                Debug.Log($"[TOSPopup] Content set: {content.Substring(0, Mathf.Min(50, content.Length))}...");
            }
            else
            {
                Debug.LogError("[TOSPopup] contentText is null!");
            }
            
            if (acceptButton != null)
            {
                var btnText = acceptButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = acceptButtonLabel;
            }
            
            this.onAcceptCallback = onAccept;

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }
        }

        private void OnAcceptClicked()
        {
            Debug.Log("[TOSPopup] Accept clicked");
            onAcceptCallback?.Invoke();
            Close();
        }

        public void Close()
        {
            Debug.Log("[TOSPopup] Closing");
            ElephantPopupManager.Instance.CloseCurrentPopup();
        }
    }
}