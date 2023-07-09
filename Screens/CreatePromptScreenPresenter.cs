// All rights reserved MWM

using System;
using System.Collections;
using System.Collections.Generic;
using AppElements.Features.Ads;
using AppElements.Features.ApplicationManager;
using AppElements.Features.Content;
using AppElements.Features.Events;
using AppElements.Features.FeatureDiscovery;
using AppElements.Features.MachineLearning;
using AppElements.Features.Navigation;
using AppElements.Features.Rating;
using AppElements.Features.Save;
using AppElements.Features.Utils;
using AppElements.Panels.NavBarPanel;
using MWM.Kit.BaseKit.Async;
using MWM.Kit.BaseKit.Devices;
using MWM.Kit.BaseKit.Utils;
using MWM.Kit.LocalizationKit;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AppElements.Screens.CreatePromptScreen {

    public class CreatePromptScreenPresenter : CreatePromptScreenContract.IPresenter {

        private const int DRAWING_RESOLUTION = 64;
        private const int MIN_COLOR_NUMBER = 10;
        private const int MAX_COLOR_NUMBER = 30;
        private const bool REMOVE_DRAWING_BACKGROUND = true;
        
        private const float DELAY_BETWEEN_ML_RESULT_AND_RATING = 0.5f;

        private const float ANCHORED_POSITION_KEYBOARD_VISIBLE = 120;
        private const float ANCHORED_POSITION_KEYBOARD_HIDDEN = 167.4f;

        private const float INPUT_FIELD_SIZE_KEYBOARD_VISIBLE = 288;
        private const float INPUT_FIELD_SIZE_KEYBOARD_HIDDEN = 168;

        private readonly IScreenRouter _screenRouter;
        private readonly INavigationManager _navigationManager;
        private readonly IMLDrawingManager _mlDrawingManager;
        private readonly IMLDrawingSaveManager _mlDrawingSaveManager;
        private readonly IBookmarkManager _bookmarkManager;
        private readonly AsyncJobExecutor _asyncJobExecutor;
        private readonly IPanelManager _panelManager;
        private readonly IRatingManager _ratingManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly CreatePromptScreenContract.IView _view;
        private readonly IMLConsumableManager _mlConsumableManager;
        private readonly IAdsManagerBridge _adsManager;
        private readonly IFeatureDiscoveryManager _featureDiscoveryManager;
        private readonly DeviceManager _deviceManager;
        private readonly IEventManager _eventManager;
        
        private string _latestSentPrompt;
        private bool _keyboardVisible;
        private bool _resultsDisplayed;
        private bool _currentPromptIsSurprise;

        private static readonly string[] _surpriseMeStrings = {
            "Exemple_1", "Exemple_2",
        };

        public CreatePromptScreenPresenter(
            IScreenRouter screenRouter,
            INavigationManager navigationManager,
            IMLDrawingManager mlDrawingManager,
            IMLDrawingSaveManager mlDrawingSaveManager,
            IBookmarkManager bookmarkManager,
            AsyncJobExecutor asyncJobExecutor,
            IPanelManager panelManager,
            IRatingManager ratingManager,
            ILocalizationManager localizationManager,
            DeviceManager deviceManager,
            IMLConsumableManager mlConsumableManager,
            IAdsManagerBridge adsManager,
            IFeatureDiscoveryManager featureDiscoveryManager,
            IEventManager eventManager,
            CreatePromptScreenContract.IView view
        ) {
            Precondition.CheckNotNull(view, screenRouter, navigationManager, mlDrawingManager,
                mlDrawingSaveManager, bookmarkManager, asyncJobExecutor, panelManager, ratingManager,
                localizationManager, deviceManager, eventManager
            );

            _screenRouter = screenRouter;
            _navigationManager = navigationManager;
            _mlDrawingSaveManager = mlDrawingSaveManager;
            _mlDrawingManager = mlDrawingManager;
            _bookmarkManager = bookmarkManager;
            _asyncJobExecutor = asyncJobExecutor;
            _view = view;
            _panelManager = panelManager;
            _ratingManager = ratingManager;
            _localizationManager = localizationManager;
            _deviceManager = deviceManager;
            _mlConsumableManager = mlConsumableManager;
            _adsManager = adsManager;
            _featureDiscoveryManager = featureDiscoveryManager;
            _localizationManager = localizationManager;
            _eventManager = eventManager;

            if (!deviceManager.IsEditor()) {
                AsyncJob job = new(CheckKeyboardRoutine(), callback: null);
                _asyncJobExecutor.Execute(job);
            }
        }
        
        public void OnDisplay(EventEnums.CreatePromptOrigin origin) {
            UpdateGenerations();
            ResetScreen();
            _eventManager.SendCreatePromptDisplayed(origin);
        }
        public void OnCreateResultButtonClicked() {
            if (_mlConsumableManager.Count > 0) {
                _view.DisplayResults();
                _view.SetInputFieldSize(INPUT_FIELD_SIZE_KEYBOARD_HIDDEN);
                _resultsDisplayed = true;
            } else {
                _adsManager.DisplayRewardedVideo(
                    EventEnums.RewardedVideoOrigin.RefillPrompt,
                    associatedDrawingId: null,
                    (source, success) => {
                        if (success) {
                            _mlConsumableManager.Add(count: 1);
                            UpdateGenerations();
                            _view.DisplayResults();
                            _view.SetInputFieldSize(INPUT_FIELD_SIZE_KEYBOARD_HIDDEN);
                            _resultsDisplayed = true;
                        }
                    });
            }
        }
        
        public void OnCloseButtonClicked() {
            if (_mlDrawingManager.CancelGeneration()) {
                _eventManager.SendPromptCancelled(_latestSentPrompt, _mlDrawingManager.GetTimeSinceGenerationStart());
                _view.ResetInputField();
                ResetScreen();
            }

            INavBarPanel.SelectableTab selectedTab = _navigationManager.GeSelectedTabScreen();
            switch (selectedTab) {
                case INavBarPanel.SelectableTab.Catalog:
                    INavBarPanel.SubCatalogTab selectedCatalogTab = _navigationManager.GetSelectedCatalogTabScreen();
                    switch (selectedCatalogTab) {
                        case INavBarPanel.SubCatalogTab.Catalog:
                            _screenRouter.CreatePromptToCatalog();
                            break;
                        case INavBarPanel.SubCatalogTab.BookDetail:
                            _screenRouter.CreatePromptToBookDetail();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case INavBarPanel.SelectableTab.Profile:
                    _screenRouter.CreatePromptToProfile();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void OnSurpriseMeClicked() {
            _view.SetInputContent(_surpriseMeStrings[Random.Range(0, _surpriseMeStrings.Length)]);
            _currentPromptIsSurprise = true;
        }
        public void OnBookmarkResult(Drawing drawing, bool bookmark) {
            if (bookmark) {
                _bookmarkManager.SetBookmarked(drawing.Id, EventEnums.BookmarkOrigin.CreatePrompt);
                // This check prevents us saving a same drawing multiple times
                if (_mlDrawingSaveManager.TryGetMLDrawing(drawing.Id, out Drawing _) is false) {
                    _mlDrawingSaveManager.SaveMLDrawing(drawing);
                }
            } else {
                _bookmarkManager.RemoveBookmark(drawing.Id, EventEnums.BookmarkOrigin.CreatePrompt);
            }
        }
        public void OnPlayResult(Drawing drawing) {
            // Drawing could have already been saved from a bookmark for ex.
            if (_mlDrawingSaveManager.TryGetMLDrawing(drawing.Id, out Drawing _) is false) {
                _mlDrawingSaveManager.SaveMLDrawing(drawing);
            }
            if (_featureDiscoveryManager.IsFeatureDiscovered(Feature.FirstDrawingStarted)) {
                _adsManager.DisplayInterstitial(
                    EventEnums.InterstitialOrigin.DrawingStarted,
                    associatedDrawingId: drawing.Id,
                    (source, success) => {
                        _screenRouter.CreatePromptToIngame(drawing);
                    }
                );
            } else {
                _screenRouter.CreatePromptToIngame(drawing);
            }
        }

        public void OnDrawingClicked(Drawing drawing) {
            _panelManager.ShowDrawingPreview(drawing, EventEnums.DrawingPreviewOrigin.CreatePrompt,
                () => _screenRouter.CreatePromptToIngame(drawing));
        }
        public void CheckButtonCreateState(string prompt) {
            _currentPromptIsSurprise = false;
            _view.SetCreateButtonState(prompt.Length > 0);
        }
        
        public void GenerateDrawings(string prompt, Action<List<Drawing>> onComplete) {
            _ratingManager.WarmUpRating();

            float requestStart = Time.realtimeSinceStartup;

            void RequestComplete(List<Drawing> drawings) {
                _mlConsumableManager.Remove(count: 1);
                if (_mlConsumableManager.Count == 0 &&
                    _featureDiscoveryManager.IsFeatureToDiscover(Feature.AiCreditsSpent)) {
                    _featureDiscoveryManager.SetState(Feature.AiCreditsSpent, DiscoveryState.Discovered);
                }
                UpdateGenerations();
                AsyncJob ratingJob = new(
                    task: AskForRatingAfterDelay(DELAY_BETWEEN_ML_RESULT_AND_RATING),
                    callback: null
                );
                _asyncJobExecutor.Execute(ratingJob);

                _eventManager.SendPromptGenerated(
                    prompt,
                    _currentPromptIsSurprise ? EventEnums.PromptType.Surprise : EventEnums.PromptType.User,
                    Time.realtimeSinceStartup - requestStart
                );

                onComplete(drawings);
            }

            void RequestFailed(int error) {
                string promptErrorTitle =
                    _localizationManager.GetLocalizedValue(LocalizationKeys.CreatePrompt.PROMPT_ERROR_TITLE);
                string promptErrorContent =
                    _localizationManager.GetLocalizedValue(LocalizationKeys.CreatePrompt.PROMPT_ERROR_GENERIC);

                EventEnums.PromptFailureReason reason;
                switch (error) {
                    case 408:
                        reason = EventEnums.PromptFailureReason.TimeOut;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 513:
                        reason = EventEnums.PromptFailureReason.InvalidPyxelateInputParams;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 514:
                        reason = EventEnums.PromptFailureReason.ErrorWhileDownloadingInputImage;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 515:
                        reason = EventEnums.PromptFailureReason.ErrorDuringPyxelateProcessing;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 516:
                        reason = EventEnums.PromptFailureReason.ErrorWhileUploadingOutputFile;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 517:
                        reason = EventEnums.PromptFailureReason.InvalidPixelSamplingInputParams;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 518:
                        reason = EventEnums.PromptFailureReason.ErrorWhileRequestingStableDiffusionImageGeneration;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    case 519:
                        reason = EventEnums.PromptFailureReason.ProfanityPrompt;
                        _panelManager.ShowHint(
                            _localizationManager.GetLocalizedValue(LocalizationKeys.CreatePrompt.PROMPT_ERROR_TOS),
                            _view.GetHintAnchor());
                        break;
                    case 520:
                        reason = EventEnums.PromptFailureReason.ErrorDuringSampledImagePostProcessing;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                    default: // Includes 500 and 0
                        reason = EventEnums.PromptFailureReason.RequestError;
                        _panelManager.ShowError(promptErrorTitle, promptErrorContent);
                        break;
                }

                _eventManager.SendPromptGenerationFail(prompt, reason, reason.ToStringValue());
                _view.ResetInputField();
                ResetScreen();
            }

            _latestSentPrompt = prompt;
            _mlDrawingManager.GenerateDrawings(
                prompt,
                nbColors: Random.Range(MIN_COLOR_NUMBER, MAX_COLOR_NUMBER + 1),
                resolution: DRAWING_RESOLUTION,
                removeBackground: REMOVE_DRAWING_BACKGROUND,
                onSuccess: RequestComplete,
                onError: RequestFailed
            );
        }
        
        public void EditorDisplayKeyboard(bool visible) {
            DisplayKeyboard(visible);
        }
        
        private void ResetScreen() {
            _view.ResetScreen();
            _view.SetInputFieldSize(INPUT_FIELD_SIZE_KEYBOARD_VISIBLE);
            _view.DisplaySurpriseMeButton(false);
            _resultsDisplayed = false;
            CheckButtonCreateState("");
        }
        
        private void DisplayKeyboard(bool visible) {
            _keyboardVisible = visible;

            int keyboardHeight = KeyboardUtils.GetKeyboardHeight(true);

            float createButtonHeight = _keyboardVisible
                ? keyboardHeight + ANCHORED_POSITION_KEYBOARD_VISIBLE
                : ANCHORED_POSITION_KEYBOARD_HIDDEN;
            float inputFieldHeight;

            if (!_resultsDisplayed) {
                inputFieldHeight = INPUT_FIELD_SIZE_KEYBOARD_VISIBLE;
            } else {
                inputFieldHeight =
                    _keyboardVisible ? INPUT_FIELD_SIZE_KEYBOARD_VISIBLE : INPUT_FIELD_SIZE_KEYBOARD_HIDDEN;

                _view.SetResultsOverlayAlpha(_keyboardVisible ? 1f : 0f);
            }

            _view.SetCreateButtonPosition(createButtonHeight);
            _view.SetInputFieldSize(inputFieldHeight);
            _view.SetSurpriseMeAlpha(_keyboardVisible ? 0f : 1f);
        }
        
        private IEnumerator CheckKeyboardRoutine() {
            while (true) {
                if (TouchScreenKeyboard.visible != _keyboardVisible) {
                    yield return null;
                    ToggleKeyboard(TouchScreenKeyboard.visible);
                }
                yield return null;
            }
        }
        
        private void UpdateGenerations() {
            string text =
                _localizationManager.GetLocalizedValue(LocalizationKeys.CreatePrompt.GENERATE_BUTTON);
            if (_mlConsumableManager.Count > 0 &&
                _featureDiscoveryManager.IsFeatureToDiscover(Feature.AiCreditsSpent)) {
                string remaining = $"{_mlConsumableManager.Count} / {_mlConsumableManager.BaseCount}";
                text = string.Format(_localizationManager.GetLocalizedValue(LocalizationKeys.CreatePrompt
                    .GENERATE_BUTTON_WITH_QUANTITY), remaining);
            }
            _view.SetGenerateButtonText(_mlConsumableManager.Count > 0, text);
        }
        
        private IEnumerator AskForRatingAfterDelay(float delay) {
            yield return new WaitForSeconds(delay);
            _ratingManager.AskForRating();
        }

    }

}