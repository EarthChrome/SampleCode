// All rights reserved MWM

using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using AppElements.Features.Navigation;


namespace AppElements.Panels.ToastPanel {
    
    public class ToastNavArgs : NavigationArguments {

        public readonly string Message;
        public readonly bool AutoClose;

        public ToastNavArgs(string message, bool autoClose) {
            Message = message;
            AutoClose = autoClose;
        }

    }
    public class ToastPanel : Panel {
        
        [SerializeField] protected TextMeshProUGUI _toastContent;
        [SerializeField] protected RectTransform _locator;
        [SerializeField] private Button _closeButton;

        private bool _autoClose;

        public override PanelId GetId() => PanelId.ToastMessage;

        public override void Initialize(Camera cam) {
            base.Initialize(cam);
            _closeButton.onClick.AddListener(OnCloseButton);
        }

        public override void Show(Transition transition, float duration, float startDelay,
            NavigationArguments navArgs) {
            
            _autoClose = ((ToastNavArgs) navArgs).AutoClose;
            _canvasGroup.blocksRaycasts = !_autoClose;
            _toastContent.text = ((ToastNavArgs) navArgs).Message;

            if (Visibility is Visibility.Shown or Visibility.Showing) {
                base.Hide(Transition.Custom, duration: 0f, startDelay: 0f);
            }
            
            _canvasGroup.alpha = 0f;
            _toastContent.alpha = 0f;
            _locator.localScale = new Vector3(x: 1f, y: 0f, z: 1f);
            
            base.Show(transition, duration, startDelay, navArgs);
        }

        protected override void AppendCustomIn(Sequence sequence, float duration) {
            sequence.Prepend(_canvasGroup.DOFade(endValue: 1f, duration: 0.15f));
            sequence.Join(_locator.DOScaleY(endValue: 1f, duration: 0.3f));
            sequence.Join(_toastContent.DOFade(endValue: 1f, duration: 0.2f).SetDelay(0.2f));

            if (_autoClose) {
                sequence.AppendInterval(2f);
                sequence.AppendCallback(OnCloseButton);
            }
        }

        private void OnCloseButton() {
            Hide(Transition.Custom, duration: .2f, startDelay: 0f);
        }

        protected override void AppendCustomOut(Sequence sequence, float duration) {
            sequence.Append(_canvasGroup.DOFade(endValue: 0f, duration: 0.2f));
        }
    }

}