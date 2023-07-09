// All rights reserved MWM

using System;
using System.Collections.Generic;
using AppElements.Features.Content;
using AppElements.Features.Events;
using UnityEngine;

namespace AppElements.Screens.CreatePromptScreen {

    public static class CreatePromptScreenContract {

        public interface IView {
            RectTransform GetHintAnchor();
            void ResetScreen();
            void ResetInputField();
            void DisplayResults();
            void DisplaySurpriseMeButton();

            void SetInputContent(string content);
            void SetGenerateButtonText(bool hasRemaining, string remaining);
            void SetCreateButtonPosition(float position);
            void SetInputFieldSize(float size);
            void SetSurpriseMeAlpha(float alpha);
            void SetResultsOverlayAlpha(float alpha);
            void SetCreateButtonState(bool interactable);

        }

        public interface IPresenter {

            void OnDisplay(EventEnums.CreatePromptOrigin origin);
            void OnCreateResultButtonClicked();
            void OnCloseButtonClicked();
            void OnSurpriseMeClicked();
            void OnBookmarkResult(Drawing drawing, bool bookmark);
            void OnPlayResult(Drawing drawing);
            void OnDrawingClicked(Drawing drawing);
            void CheckButtonCreateState(string prompt);
            void GenerateDrawings(string prompt, Action<List<Drawing>> onComplete);
            void EditorDisplayKeyboard(bool visible);
        }

    }

}