using System;
using System.Collections.Generic;
using AppElements.Features.ApplicationManager;
using AppElements.Features.Content;
using AppElements.Features.Events;
using AppElements.Features.Gameplay.Bonus;
using AppElements.Panels.DrawingPreviewPanel;
using AppElements.Panels.DrawingResumePanel;
using AppElements.Panels.NavBarPanel;
using AppElements.Panels.RefillBonusPanel;
using AppElements.Panels.ToastPanel;
using MWM.Kit.BaseKit;
using MWM.Kit.BaseKit.Devices;
using MWM.Kit.ToolsKit.Runtime;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AppElements.Features.Navigation {

    public class PanelManager : MonoBehaviour, IPanelManager {

        public event IPanelManager.PanelVisibilityEvent OnPanelVisibilityChanged;

        [SerializeField] private Transform _panelsContainer;
        [SerializeField] private List<PanelHolder> _panelHolders;

        private readonly Dictionary<PanelId, Panel> _panelPrefabsDictionary = new();
        private readonly Dictionary<PanelId, Panel> _panels = new();

        private DeviceManager _deviceManager;

        public const float DURATION = 0.25f;

        private bool _isTablet;

#if UNITY_EDITOR

        [Button] [PropertyOrder(-1)]
        private void FetchHolders() {
            Undo.RecordObject(this, "Fetch panel holders");
            _panelHolders = AssetUtils.FetchAssetsOfType<PanelHolder>();
        }

#endif

        public void SynchronousInit() {
            _deviceManager = ApplicationGraph.DeviceManager;
            _isTablet = _deviceManager.GetScreenRatio() is ScreenRatio.ratio_4_3;
            foreach (PanelHolder panelHolder in _panelHolders) {
                Panel panel = panelHolder.GetPanel();
                PanelId panelId = panel.GetId();
                if (_panelPrefabsDictionary.ContainsKey(panelId)) {
                    Debug.LogError($"PanelId {panelId} has already been added, this should not happen !");
                    continue;
                }
                _panelPrefabsDictionary.Add(panelId, panel);
            }
        }

        private void Show(PanelId panelId, Transition transition, float duration, float startDelay,
            NavigationArguments navArgs = null) {
            Panel panel = GetPanel(panelId);
            panel.Show(transition, duration, startDelay, navArgs);
        }

        private void Hide(PanelId panelId, Transition transition, float duration, float startDelay) {
            Panel panel = GetPanel(panelId);
            panel.Hide(transition, duration, startDelay);
        }

        public Panel GetPanel(PanelId panelId) {
            if (_panels.ContainsKey(panelId)) return _panels[panelId];

            Panel panel = Instantiate(_panelPrefabsDictionary[panelId], _panelsContainer);
            panel.OnVisibilityChange += visibility => {
                OnPanelVisibilityChanged?.Invoke(panel: panelId, visibility: visibility);
            };
            panel.Initialize(ApplicationGraph.UICamera);
            panel.AdaptForRatio(_deviceManager.GetScreenRatio());
            _panels.Add(panelId, panel);
            return panel;
        }

        public bool IsVisible(PanelId panelId) {
            return _panels.ContainsKey(panelId) && _panels[panelId].IsVisible;
        }

        public INavBarPanel GetNavBar() {
            return GetPanel(PanelId.NavBar) as INavBarPanel;
        }

        public void ShowLoader() {
            Show(PanelId.Loader, Transition.None, 0f, 0f);
        }

        public void HideLoader() {
            Hide(PanelId.Loader, Transition.None, 0f, 0f);
        }

        public void ShowNavBar(float duration) {
            Show(PanelId.NavBar, Transition.Fade, duration, 0f);
        }

        public void HideNavBar() {
            Hide(PanelId.NavBar, Transition.None, 0, 0f);
        }

        public void ShowRefillBonus(Bonus bonus) {
            Show(PanelId.RefillBonus, Transition.Fade, DURATION, 0f, new RefillBonusNavArgs(bonus));
        }

        public void ShowDrawingPreview(Drawing drawing, EventEnums.DrawingPreviewOrigin origin, Action onPlayClicked) {
            Transition transition = _isTablet ? Transition.Fade : Transition.BottomToTop;
            Show(PanelId.DrawingPreview, transition, DURATION, 0, new DrawingPreviewNavArgs(drawing, origin, onPlayClicked));
        }

        public void ShowDrawingResume(Drawing drawing) {
            Show(PanelId.DrawingResume, Transition.None, duration: 0, startDelay: 0, new DrawingResumeNavArgs(drawing));
        }

        [Button]
        public void ShowMessage(string content, bool autoclose = false) {
            Show(PanelId.ToastMessage, Transition.Custom, 2f, 0f, new ToastNavArgs(content, autoclose));
        }
        [Button]
        public void ShowError(string title, string content, bool autoclose = true) {
            Show(PanelId.ToastError, Transition.Custom, 2f, 0f, new ToastErrorNavArgs(title, content, autoclose));
        }
        [Button]
        public void ShowValidation(string content, bool autoclose = true) {
            Show(PanelId.ToastValidation, Transition.Custom, 2f, 0f, new ToastNavArgs(content, autoclose));
        }
        [Button]
        public void ShowHint(string content, RectTransform rt, bool autoclose = false) {
            Show(PanelId.ToastHint, Transition.Custom, 2f, 0f, new ToastHintNavArgs(content, rt, autoclose));
        }

    }

}
