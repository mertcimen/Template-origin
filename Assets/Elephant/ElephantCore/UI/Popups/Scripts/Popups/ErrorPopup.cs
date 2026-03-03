using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ElephantSDK
{
    public class ErrorPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button okButton;
        
        private Action onOkCallback;

        public void Initialize(string errorMessage, string buttonLabel, Action onOk = null)
        {
            Debug.Log("[ErrorPopup] Initializing with error message");

            if (contentText != null)
            {
                contentText.text = HyperlinkUtils.CleanText(errorMessage);
                Debug.Log($"[ErrorPopup] Error message set: {errorMessage}");
            }
            else
            {
                Debug.LogError("[ErrorPopup] contentText is null!");
            }

            if (okButton != null)
            {
                var btnText = okButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = buttonLabel;
            }
            else
            {
                Debug.LogError("[ErrorPopup] okButton is null!");
            }
            
            this.onOkCallback = onOk;
            SetupButton();
        }

        private void SetupButton()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(OnOkClicked);
            }
        }

        private void OnOkClicked()
        {
            Debug.Log("[ErrorPopup] OK button clicked");
            onOkCallback?.Invoke();
            Close();
        }

        public void Close()
        {
            Debug.Log("[ErrorPopup] Closing");
            ElephantPopupManager.Instance.CloseCurrentPopup();
        }
    }
}