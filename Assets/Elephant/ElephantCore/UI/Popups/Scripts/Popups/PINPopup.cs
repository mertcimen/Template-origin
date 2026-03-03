using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace ElephantSDK
{
    public class PINPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button backButton;

        private Action onBackCallback;

        public void Initialize(Pin pinData, string backButtonLabel, Action onBack)
        {
            Debug.Log("[PINPopup] Initializing PIN request");

            this.onBackCallback = onBack;

            if (contentText != null)
            {
                var hyperlinks = new List<HyperlinkData>
                {
                    new HyperlinkData(HyperlinkUtils.PRIVACY_MASK, 
                                     pinData.privacy_policy_text, 
                                     pinData.privacy_policy_url),
                    new HyperlinkData(HyperlinkUtils.TERMS_MASK, 
                                     pinData.terms_of_service_text, 
                                     pinData.terms_of_service_url),
                    new HyperlinkData(HyperlinkUtils.DATA_REQUEST_MASK, 
                                     pinData.data_request_text, 
                                     pinData.data_request_url)
                };

                string processedContent = HyperlinkUtils.ProcessHyperlinks(pinData.content, hyperlinks);
                contentText.text = HyperlinkUtils.CleanText(processedContent);
                contentText.text = processedContent;
                
                Debug.Log($"[PINPopup] Content set with hyperlinks processed");
                
                SetupLinkInteraction();
            }
            else
            {
                Debug.LogError("[PINPopup] contentText is null!");
            }

            SetupButton(backButtonLabel);
        }

        private void SetupButton(string buttonLabel)
        {
            if (backButton != null)
            {
                var btnText = backButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = buttonLabel;
                
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackClicked);
            }
            else
            {
                Debug.LogError("[PINPopup] backButton is null!");
            }
        }

        private void SetupLinkInteraction()
        {
            if (contentText == null) return;

            EventTrigger trigger = contentText.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = contentText.gameObject.AddComponent<EventTrigger>();
            }

            // Clear existing triggers to avoid duplicates
            trigger.triggers.Clear();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnLinkClicked((PointerEventData)data); });
            trigger.triggers.Add(entry);

            Debug.Log("[PINPopup] Link interaction setup complete");
        }

        private void OnLinkClicked(PointerEventData eventData)
        {
            if (contentText == null) return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(contentText, eventData.position, null);
            
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = contentText.textInfo.linkInfo[linkIndex];
                string url = linkInfo.GetLinkID();
                
                if (!string.IsNullOrEmpty(url))
                {
                    Debug.Log($"[PINPopup] Link clicked: {url}");
                    HyperlinkUtils.OpenURL(url);
                }
            }
        }

        private void OnBackClicked()
        {
            Debug.Log("[PINPopup] Back button clicked");
            onBackCallback?.Invoke();
            Close();
        }

        public void Close()
        {
            Debug.Log("[PINPopup] Closing");
            ElephantPopupManager.Instance.CloseCurrentPopup();
        }
    }
}