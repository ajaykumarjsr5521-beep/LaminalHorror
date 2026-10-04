using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Settings;

namespace NocturneAnnex.UI
{
    /// <summary>Binds SettingsScreenModel to controls. Back always reverts unsaved edits so previews never stick.</summary>
    public class SettingsView : MonoBehaviour
    {
        public Slider LookSensitivity;
        public Toggle InvertY;
        public Slider MasterVolume;
        public Slider MusicVolume;
        public Slider SfxVolume;
        public Toggle Captions;
        public Slider TouchScale;
        public Toggle StoryMode;
        public TMP_Dropdown Quality;
        public Button SaveButton;
        public Button BackButton;
        public Button ResetButton;
        public TMP_Text ErrorText;

        public event Action Closed;

        SettingsScreenModel _model;
        bool _wired;

        public void Bind(SettingsScreenModel model, IList<string> qualityNames)
        {
            _model = model;
            Configure(qualityNames);
            if (!_wired)
            {
                Listen(LookSensitivity, v => _model.Working.LookSensitivity = v);
                Listen(MasterVolume, v => _model.Working.MasterVolume = v);
                Listen(MusicVolume, v => _model.Working.MusicVolume = v);
                Listen(SfxVolume, v => _model.Working.SfxVolume = v);
                Listen(TouchScale, v => _model.Working.TouchControlsScale = v);
                Listen(InvertY, v => _model.Working.InvertY = v);
                Listen(Captions, v => _model.Working.CaptionsEnabled = v);
                Listen(StoryMode, v => _model.Working.StoryMode = v);
                Quality.onValueChanged.AddListener(i => { _model.Working.QualityLevel = i - 1; _model.NotifyChanged(); Render(); });
                SaveButton.onClick.AddListener(() => { _model.Save(); Render(); });
                ResetButton.onClick.AddListener(() => { _model.ResetToDefaults(); Render(); });
                BackButton.onClick.AddListener(Back);
                _wired = true;
            }
            Render();
        }

        void Configure(IList<string> qualityNames)
        {
            LookSensitivity.minValue = SettingsData.MinSensitivity;
            LookSensitivity.maxValue = SettingsData.MaxSensitivity;
            foreach (var s in new[] { MasterVolume, MusicVolume, SfxVolume }) { s.minValue = 0f; s.maxValue = 1f; }
            TouchScale.minValue = InputMath.MinControlsScale;
            TouchScale.maxValue = InputMath.MaxControlsScale;

            var options = new List<string> { Loc.Get("settings.quality_auto") };
            options.AddRange(qualityNames);
            Quality.ClearOptions();
            Quality.AddOptions(options);
        }

        void Back()
        {
            _model.Revert();
            Closed?.Invoke();
        }

        void Listen(Slider s, Action<float> set) => s.onValueChanged.AddListener(v => { set(v); _model.NotifyChanged(); Render(); });
        void Listen(Toggle t, Action<bool> set) => t.onValueChanged.AddListener(v => { set(v); _model.NotifyChanged(); Render(); });

        /// <summary>Pushes model values into controls without raising change events.</summary>
        void Render()
        {
            var w = _model.Working;
            LookSensitivity.SetValueWithoutNotify(w.LookSensitivity);
            InvertY.SetIsOnWithoutNotify(w.InvertY);
            MasterVolume.SetValueWithoutNotify(w.MasterVolume);
            MusicVolume.SetValueWithoutNotify(w.MusicVolume);
            SfxVolume.SetValueWithoutNotify(w.SfxVolume);
            Captions.SetIsOnWithoutNotify(w.CaptionsEnabled);
            TouchScale.SetValueWithoutNotify(w.TouchControlsScale);
            StoryMode.SetIsOnWithoutNotify(w.StoryMode);
            Quality.SetValueWithoutNotify(w.QualityLevel + 1);

            SaveButton.interactable = _model.IsDirty;
            ErrorText.text = _model.Error ?? string.Empty;
            ErrorText.gameObject.SetActive(!string.IsNullOrEmpty(_model.Error));
        }
    }
}
