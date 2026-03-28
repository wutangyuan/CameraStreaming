using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CameraStreaming.Models;
using CameraStreaming.Services;
using WpfWindow = System.Windows.Window;

namespace CameraStreaming.Views
{
    public partial class SettingsWindow : WpfWindow
    {
        private CameraConfig _config = new CameraConfig();
        private List<CameraInfo>? _cameras;
        private readonly LocalizationService _lang = LocalizationService.Instance;

        public CameraConfig Config => _config;

        public SettingsWindow(CameraConfig currentConfig)
        {
            InitializeComponent();
            _config = currentConfig;
            LoadCurrentSettings();
            LoadLanguage();
            UpdateDynamicTexts();
            _lang.PropertyChanged += Lang_PropertyChanged;
        }

        private void Lang_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateDynamicTexts();
        }

        private void UpdateDynamicTexts()
        {
            txtLanguageLabel.Text = _lang["LanguageLabel"];
            txtWindowShapeLabel.Text = _lang["WindowShapeLabel"];
            if (_cameras == null || _cameras.Count == 0) return;

            var selectedIdx = cmbCameras.SelectedIndex;
            cmbCameras.Items.Clear();

            foreach (var camera in _cameras)
            {
                var item = new ComboBoxItem
                {
                    Content = string.Format(_lang["CameraInfo"], camera.Name, camera.Width, camera.Height, camera.Fps),
                    Tag = camera.Index
                };
                cmbCameras.Items.Add(item);
            }

            if (selectedIdx >= 0 && selectedIdx < cmbCameras.Items.Count)
                cmbCameras.SelectedIndex = selectedIdx;
        }

        private void LoadLanguage()
        {
            foreach (ComboBoxItem item in cmbLanguage.Items)
            {
                if (item.Tag?.ToString() == _config.Language)
                {
                    cmbLanguage.SelectedItem = item;
                    break;
                }
            }
        }

        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLanguage.SelectedItem is ComboBoxItem langItem && langItem.Tag != null)
            {
                var lang = langItem.Tag.ToString()!;
                if (lang != _config.Language)
                {
                    _config.Language = lang;
                    _lang.Language = lang;
                    ConfigService.SaveConfig(_config);
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbCameras.IsEnabled = false;
            txtLoading.Visibility = Visibility.Visible;

            await LoadCamerasAsync();

            cmbCameras.IsEnabled = _cameras != null && _cameras.Count > 0;
            txtLoading.Visibility = Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task LoadCamerasAsync()
        {
            try
            {
                _cameras = await CameraService.GetAvailableCamerasAsync();

                cmbCameras.Items.Clear();

                if (_cameras.Count == 0)
                {
                    cmbCameras.Items.Add(new ComboBoxItem { Content = _lang["NoCameraDetected"] });
                    cmbCameras.SelectedIndex = 0;
                    return;
                }

                foreach (var camera in _cameras)
                {
                    var item = new ComboBoxItem
                    {
                        Content = string.Format(_lang["CameraInfo"], camera.Name, camera.Width, camera.Height, camera.Fps),
                        Tag = camera.Index
                    };
                    cmbCameras.Items.Add(item);

                    if (camera.Index == _config.Index)
                    {
                        cmbCameras.SelectedIndex = cmbCameras.Items.Count - 1;
                    }
                }

                if (cmbCameras.SelectedIndex < 0 && cmbCameras.Items.Count > 0)
                {
                    cmbCameras.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                cmbCameras.Items.Add(new ComboBoxItem { Content = _lang["DetectCameraFailed"] });
                cmbCameras.SelectedIndex = 0;
                Debug.WriteLine($"检测摄像头异常: {ex.Message}");
            }
        }

        private void LoadCurrentSettings()
        {
            // Load window shape
            foreach (ComboBoxItem item in cmbWindowShape.Items)
            {
                if (item.Tag?.ToString() == _config.WindowShape)
                {
                    cmbWindowShape.SelectedItem = item;
                    break;
                }
            }
            if (cmbWindowShape.SelectedIndex < 0)
            {
                cmbWindowShape.SelectedIndex = 0;
            }

            foreach (ComboBoxItem item in cmbResolution.Items)
            {
                if (item.Tag?.ToString() == $"{_config.Width},{_config.Height}")
                {
                    cmbResolution.SelectedItem = item;
                    break;
                }
            }
            if (cmbResolution.SelectedIndex < 0)
            {
                cmbResolution.SelectedIndex = 0;
            }

            foreach (ComboBoxItem item in cmbFps.Items)
            {
                if (item.Tag?.ToString() == _config.Fps.ToString())
                {
                    cmbFps.SelectedItem = item;
                    break;
                }
            }
            if (cmbFps.SelectedIndex < 0)
            {
                cmbFps.SelectedIndex = 1;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCameras.SelectedItem is ComboBoxItem cameraItem && cameraItem.Tag != null)
                {
                    _config.Index = int.Parse(cameraItem.Tag.ToString()!);
                }

                if (cmbResolution.SelectedItem is ComboBoxItem resolutionItem && resolutionItem.Tag != null)
                {
                    var parts = resolutionItem.Tag.ToString()!.Split(',');
                    _config.Width = int.Parse(parts[0]);
                    _config.Height = int.Parse(parts[1]);
                }

                if (cmbFps.SelectedItem is ComboBoxItem fpsItem && fpsItem.Tag != null)
                {
                    _config.Fps = int.Parse(fpsItem.Tag.ToString()!);
                }

                if (cmbWindowShape.SelectedItem is ComboBoxItem shapeItem && shapeItem.Tag != null)
                {
                    _config.WindowShape = shapeItem.Tag.ToString()!;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(_lang["SaveSettingsFailed"], ex.Message), _lang["Error"],
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
