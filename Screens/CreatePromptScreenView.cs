// All rights reserved MWM

using System.Collections.Generic;
using AppElements.Features.ApplicationManager;
using AppElements.Features.Content;
using AppElements.Features.Events;
using AppElements.Features.Navigation;
using DG.Tweening;
using MWM.Kit.BaseKit.Devices;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Screen = AppElements.Features.Navigation.Screen;

namespace AppElements.Screens.CreatePromptScreen {

    public class CreatePromptNavArgs : NavigationArguments {

        public readonly EventEnums.CreatePromptOrigin Origin;

        public CreatePromptNavArgs(EventEnums.CreatePromptOrigin origin) {
            Origin = origin;
        }

    }

    public class CreatePromptScreenView : Screen, CreatePromptScreenContract.IView {

        [SerializeField] private Button _createResultButton;
        [SerializeField] private RectTransform _createResultButtonRectTransform;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _surpriseMeButton;
        [SerializeField] private Button _surpriseMeInputButton;
        [SerializeField] private CanvasGroup _surpriseMeButtonCanvasGroup;
        [SerializeField] private CanvasGroup _surpriseMeInputButtonCanvasGroup;
        [SerializeField] private GameObject _createResultButtonOverlay;
        [SerializeField] private GameObject _surpriseMeButtonOverlay;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private RectTransform _inputFieldRectTransform;
        [SerializeField] private RectTransform _hintAnchor;
        [SerializeField] private PositionConstraint _inputFieldConstraint;
        [SerializeField] private TextMeshProUGUI _textRemaining;
        [SerializeField] private GameObject _generateRvIcon;

        [SerializeField] private CanvasGroup _inputGroup;
        [SerializeField] private CanvasGroup _resultGroup;
        [SerializeField] private CanvasGroup _resultOverlayGroup;

        [SerializeField] private ElementGridBehavior[] _results;

        [SerializeField] private RectTransform _canvasGroupRT;
        [SerializeField] private ScrollRect _scrollRect;

        private CreatePromptScreenContract.IPresenter _presenter;

        private const int RESULTS_COUNT = 4;

        private const string BUTTON_POSITION_ID = "button_position_id";
        private const string BUTTON_SURPRISE_ALPHA_ID = "button_surprise_alpha_id";
        private const string INPUT_FIELD_SIZE_ID = "input_field_size_id";
        private const string RESULTS_OVERLAY_ALPHA_ID = "results_overlay_alpha_id";

        private const string DISPLAY_ID = "display_id";

        public override ScreenId GetId() => ScreenId.CreatePrompt;
        
        public override void Initialize(Camera cam) {
            base.Initialize(cam);
            DeviceManager deviceManager = ApplicationGraph.DeviceManager;
            _presenter = new CreatePromptScreenPresenter(
                screenRouter: ApplicationGraph.ScreenRouter,
                navigationManager: ApplicationGraph.NavigationManager,
                mlDrawingManager: ApplicationGraph.MLDrawingManager,
                mlDrawingSaveManager: ApplicationGraph.MLDrawingSaveManager,
                bookmarkManager: ApplicationGraph.BookmarkManager,
                asyncJobExecutor: ApplicationGraph.AsyncJobExecutor,
                panelManager: ApplicationGraph.PanelManager,
                ratingManager: ApplicationGraph.RatingManager,
                localizationManager: ApplicationGraph.LocalizationManager,
                deviceManager: ApplicationGraph.DeviceManager,
                mlConsumableManager: ApplicationGraph.MLConsumableManager,
                adsManager: ApplicationGraph.AdsManager,
                featureDiscoveryManager: ApplicationGraph.FeatureDiscoveryManager,
                eventManager: ApplicationGraph.EventManager,
                view: this
            );

            _closeButton.onClick.AddListener(_presenter.OnCloseButtonClicked);
            _createResultButton.onClick.AddListener(_presenter.OnCreateResultButtonClicked);
            _surpriseMeButton.onClick.AddListener(_presenter.OnSurpriseMeClicked);
            _surpriseMeInputButton.onClick.AddListener(_presenter.OnSurpriseMeClicked);

            _inputField.shouldHideMobileInput = true;

            if (deviceManager.IsEditor()) {
                _inputField.onSelect.AddListener(_ => { _presenter.EditorChangeKeyboard(true); });
                _inputField.onDeselect.AddListener(_ => { _presenter.EditorChangeKeyboard(false); });
                _inputField.onEndEdit.AddListener(_ => { _presenter.EditorChangeKeyboard(false); });
            }

            _inputField.onValueChanged.AddListener(str => { _presenter.CheckButtonCreateState(str); });

            _inputField.onSubmit.AddListener(str => {
                if (str.Length > 0) {
                    _createResultButton.onClick.Invoke();
                }
                EventSystem.current.SetSelectedGameObject(null);
            });

            foreach (ElementGridBehavior elementGridBehavior in _results) {
                elementGridBehavior.Initialize(
                    ApplicationGraph.AsyncJobExecutor,
                    ApplicationGraph.BookmarkManager,
                    _presenter.OnPlayResult,
                    BookmarkResult,
                    _presenter.OnDrawingClicked
                );
            }
        }

        public override void Show(Transition transition, float duration, float startDelay,
            NavigationArguments navArgs) {
            base.Show(transition, duration, startDelay, navArgs);

            CreatePromptNavArgs createPromptNavArgs = (CreatePromptNavArgs) navArgs;
            _presenter.OnDisplay(createPromptNavArgs.Origin);
        }
        
        public override bool OnBackPressed() {
            _presenter.OnCloseButtonClicked();
            return true;
        }
        
        protected override void AppendCustomIn(Sequence sequence, float duration) {
            _canvasGroupRT.anchoredPosition = new Vector2(0f, -2000f);
            sequence.Append(_canvasGroupRT.DOAnchorPosY(0f, duration).SetEase(Ease.OutBack, 1f));
        }

        protected override void AppendCustomOut(Sequence sequence, float duration) {
            sequence.Append(_canvasGroupRT.DOAnchorPosY(-2000f, duration).SetEase(Ease.InCubic));
        }
        
        protected override void SendDisplayedEvent() {}
        
        public RectTransform GetHintAnchor() {
            return _hintAnchor;
        }
        
        public void ResetScreen() {
            DOTween.Kill(BUTTON_POSITION_ID);
            DOTween.Kill(BUTTON_SURPRISE_ALPHA_ID);
            DOTween.Kill(INPUT_FIELD_SIZE_ID);
            DOTween.Kill(RESULTS_OVERLAY_ALPHA_ID);

            DOTween.Kill(DISPLAY_ID);
            _inputField.ActivateInputField();
            _inputField.text = "";
            _inputField.interactable = true;

            _inputGroup.alpha = 1f;
            _inputGroup.interactable = true;

            _resultGroup.alpha = 0f;
            _resultGroup.blocksRaycasts = false;

            _resultOverlayGroup.alpha = 0f;

            _inputFieldConstraint.constraintActive = true;

            for (int index = 0; index < 4; index++) {
                _results[index].CancelLoading();
            }
        }
        
        public void ResetInputField() {
            _inputField.text = "";
        }
        
        public void DisplayResults() {
            DisplaySurpriseMeButton(true);
            SetInputFieldPosition(50);
            _scrollRect.verticalNormalizedPosition = 1f;

            _inputGroup.DOFade(0f, .5f).SetId(DISPLAY_ID);
            _inputGroup.interactable = false;
            _resultGroup.DOFade(1f, 0.5f).SetId(DISPLAY_ID).onComplete += () => { _resultGroup.blocksRaycasts = true; };

            SetCreateResultButtonInteractable(false);
            SetSurpriseMeButtonInteractable(false);
            _inputField.interactable = false;
            _presenter.GenerateDrawings(_inputField.text, OnDrawingsGenerated);

            float delay = 0f;
            
            for (int index = 0; index < RESULTS_COUNT; index++) {
                _results[index].DisplayGridElement(delay += 0.1f);
            }
        }
        
        public void DisplaySurpriseMeButton() {
            DOTween.Complete(BUTTON_SURPRISE_ALPHA_ID);
            _surpriseMeButtonCanvasGroup.gameObject.SetActive(true);
            _surpriseMeButtonCanvasGroup.alpha = 1f;
            _surpriseMeInputButtonCanvasGroup.gameObject.SetActive(false);
            _surpriseMeInputButtonCanvasGroup.alpha = 0f;
        }

        public void SetInputContent(string content) {
            _inputField.text = content;
        }

        public void SetGenerateButtonText(bool hasRemaining, string text) {
            _generateRvIcon.SetActive(hasRemaining is false);
            _textRemaining.text = text;
        }

        public void SetCreateButtonPosition(float position) {
            DOTween.Kill(BUTTON_POSITION_ID);
            _createResultButtonRectTransform.DOAnchorPosY(position / _canvas.scaleFactor, 0.35f, true)
                .SetId(BUTTON_POSITION_ID).SetEase(Ease.OutBack);
        }
        
        public void SetInputFieldSize(float size) {
            DOTween.Kill(INPUT_FIELD_SIZE_ID);
            Vector2 currentDelta = _inputFieldRectTransform.sizeDelta;
            _inputFieldRectTransform.DOSizeDelta(new Vector2(currentDelta.x, size), 0.15f, true)
                .SetId(INPUT_FIELD_SIZE_ID).SetEase(Ease.OutBack);
        }

        public void SetSurpriseMeAlpha(float alpha) {
            DOTween.Kill(BUTTON_SURPRISE_ALPHA_ID);
            _surpriseMeButtonCanvasGroup.DOFade(alpha, 0.2f).SetId(BUTTON_SURPRISE_ALPHA_ID);
            _surpriseMeInputButtonCanvasGroup.DOFade(alpha, 0.2f).SetId(BUTTON_SURPRISE_ALPHA_ID);
        }

        public void SetResultsOverlayAlpha(float alpha) {
            DOTween.Kill(RESULTS_OVERLAY_ALPHA_ID);
            _resultOverlayGroup.DOFade(alpha, 0.2f).SetId(RESULTS_OVERLAY_ALPHA_ID);
        }
        
        public void SetCreateButtonState(bool interactable) {
            SetCreateResultButtonInteractable(interactable);
        }
        
        private void OnDrawingsGenerated(List<Drawing> drawings) {
            SetCreateResultButtonInteractable(true);
            SetSurpriseMeButtonInteractable(true);
            _inputField.interactable = true;
            _scrollRect.verticalNormalizedPosition = 1f;
            
            for (int i = 0; i < 4; i++) {
                _results[i].OnDrawingGenerated(drawings[i]);
            }
        }

        private void BookmarkResult(Drawing drawing, bool bookmark) {
            _presenter.OnBookmarkResult(drawing, bookmark);
        }
        
        private void SetSurpriseMeButtonInteractable(bool interactable) {
            _surpriseMeInputButton.interactable = interactable;
            _surpriseMeButtonOverlay.SetActive(interactable is false);
        }

        private void SetCreateResultButtonInteractable(bool interactable) {
            _createResultButton.interactable = interactable;
            _createResultButtonOverlay.SetActive(interactable is false);
        }
        
        private void SetInputFieldPosition(float position) {
            _inputFieldConstraint.constraintActive = false;
            _inputFieldRectTransform.DOAnchorPosY(position, 0.2f);
        }
        
    }

}
