using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ElephantSDK
{
    public class PersonalizedAdsPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Toggle consentCheckbox;
        [SerializeField] private Button submitButton;

        private Action onAgreeCallback;
        private Action onDeclineCallback;
        
        public void Initialize(string title, string content,
                              string privacyPolicyText, string privacyPolicyUrl,
                              string declineButtonText, string agreeButtonText,
                              string backButtonText,
                              Action onAgree, Action onDecline)
        {
            Debug.Log("[PersonalizedAdsPopup] Initializing personalized ads popup");

            this.onAgreeCallback = onAgree;
            this.onDeclineCallback = onDecline;

            if (contentText != null)
            {
                var hyperlinks = new List<HyperlinkData>
                {
                    new HyperlinkData(HyperlinkUtils.PRIVACY_MASK, privacyPolicyText, privacyPolicyUrl)
                };

                string processedContent = HyperlinkUtils.ProcessHyperlinks(content, hyperlinks);
                contentText.text = HyperlinkUtils.CleanText(processedContent);

                Debug.Log("[PersonalizedAdsPopup] Content set with hyperlinks processed");

                SetupLinkInteraction();
            }
            else
            {
                Debug.LogError("[PersonalizedAdsPopup] contentText is null!");
            }

            if (consentCheckbox != null)
            {
                consentCheckbox.isOn = true;
                consentCheckbox.onValueChanged.RemoveAllListeners();
                consentCheckbox.onValueChanged.AddListener(OnCheckboxChanged);
            }
            else
            {
                Debug.LogError("[PersonalizedAdsPopup] consentCheckbox is null!");
            }

            if (submitButton != null)
            {
                var btnText = submitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Submit";
                
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
            }
            else
            {
                Debug.LogError("[PersonalizedAdsPopup] submitButton is null!");
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

            trigger.triggers.Clear();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnLinkClicked((PointerEventData)data); });
            trigger.triggers.Add(entry);

            Debug.Log("[PersonalizedAdsPopup] Link interaction setup complete");
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
                    Debug.Log($"[PersonalizedAdsPopup] Privacy policy link clicked: {url}");
                    HyperlinkUtils.OpenURL(url);
                }
            }
        }

        private void OnCheckboxChanged(bool isChecked)
        {
            Debug.Log($"[PersonalizedAdsPopup] Checkbox changed to: {isChecked}");
        }

        private void OnSubmitClicked()
        {
            try
            {
                if (consentCheckbox != null && consentCheckbox.isOn)
                {
                    Debug.Log("[PersonalizedAdsPopup] User AGREED to personalized ads");
                    onAgreeCallback?.Invoke();
                }
                else
                {
                    Debug.Log("[PersonalizedAdsPopup] User DECLINED personalized ads");
                    onDeclineCallback?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PersonalizedAdsPopup] {e.Message}");
            }
            finally
            {
                ElephantPopupManager.Instance.CloseCurrentPopup();
            }
        }
    }
}