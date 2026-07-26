using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using ContextMenuWF = System.Windows.Forms.ContextMenu;
using MenuItemWF = System.Windows.Forms.MenuItem;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace msovideo_srgb
{
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;

        private ContextMenuWF _contextMenu;

        public MainWindow()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("Already running!");
                Close();
                return;
            }

            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;
            SystemEvents.DisplaySettingsChanged += _viewModel.OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged += _viewModel.OnPowerModeChanged;

            DisplayStateObserver.Init();
            DisplayStateObserver.OnDisplayWake += _viewModel.OnDisplaySettingsChanged;

            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0);

            if (args.Contains("-minimize"))
            {
                WindowState = WindowState.Minimized;
                Hide();
            }

            UpdatePresets();
            InitializeTrayIcon();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }

            base.OnStateChanged(e);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs o)
        {
            var window = new AboutWindow
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Windows.Cast<Window>().Any(x => x is AdvancedWindow)) return;
            var monitor = ((FrameworkElement)sender).DataContext as MonitorData;
            var window = new AdvancedWindow(monitor)
            {
                Owner = this
            };

            void CloseWindow(object o, EventArgs e2) => window.Close();

            SystemEvents.DisplaySettingsChanged += CloseWindow;
            if (window.ShowDialog() == false) return;
            SystemEvents.DisplaySettingsChanged -= CloseWindow;

            if (window.ChangedCalibration)
            {
                _viewModel.SaveConfig();
                monitor?.ReapplyClamp();
            }
        }

        private void ReapplyButton_Click(object sender, RoutedEventArgs e)
        {
            ReapplyMonitorSettings();
        }

        private void InitializeTrayIcon()
        {
            var notifyIcon = new NotifyIcon
            {
                Text = "Msovideo sRGB",
                Icon = Properties.Resources.icon,
                Visible = true
            };

            notifyIcon.MouseDoubleClick +=
                delegate
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            _contextMenu = new ContextMenuWF();

            _contextMenu.Popup += delegate { UpdateContextMenu(); };

            notifyIcon.ContextMenu = _contextMenu;

            Closed += delegate { notifyIcon.Dispose(); };
        }

        private void UpdateContextMenu()
        {
            _contextMenu.MenuItems.Clear();

            foreach (var monitor in _viewModel.Monitors)
            {
                var item = new MenuItemWF();
                _contextMenu.MenuItems.Add(item);
                item.Text = monitor.Name;
                item.Checked = monitor.Clamped;
                item.Enabled = monitor.CanClamp;
                item.Click += (sender, args) => monitor.Clamped = !monitor.Clamped;
            }

            _contextMenu.MenuItems.Add("-");

            var presets = Config.GetPresetNames();
            var activePreset = Config.GetActivePresetId();

            for (int i = 0; i < presets.Length; i++)
            {
                int presetId = i;
                var presetName = presets[i];
                var item = new MenuItemWF();
                _contextMenu.MenuItems.Add(item);
                item.Text = presetName;
                item.Checked = i == activePreset;
                item.Click += (s, e) =>
                {
                    if (presetId != Config.GetActivePresetId())
                    {
                        Config.SetActivePreset(presetId);
                        Config.SafeSave();
                        Presets.SelectedIndex = presetId;
                        _viewModel.OnDisplaySettingsChanged(null, null);
                    }
                };
            }

            _contextMenu.MenuItems.Add("-");

            var reapplyItem = new MenuItemWF();
            _contextMenu.MenuItems.Add(reapplyItem);
            reapplyItem.Text = "Reapply";
            reapplyItem.Click += delegate { ReapplyMonitorSettings(); };

            var exitItem = new MenuItemWF();
            _contextMenu.MenuItems.Add(exitItem);
            exitItem.Text = "Exit";
            exitItem.Click += delegate { Close(); };
        }

        private ContextMenu CreateContextMenu(Style menuItemStyle, int presetId, string presetName)
        {
            var contextMenu = new ContextMenu();

            var renameItem = new MenuItem();
            contextMenu.Items.Add(renameItem);
            renameItem.Header = "Rename";
            renameItem.Style = menuItemStyle;
            renameItem.Click += (s, e) =>
            {
                string name = Dialogs.InputDialog(presetName, "Rename");
                if (name != null && name != presetName)
                {
                    Config.RenamePreset(presetId, name);
                    Config.SafeSave();
                    UpdatePresets();
                }
            };

            var deleteItem = new MenuItem();
            contextMenu.Items.Add(deleteItem);
            deleteItem.Header = "Delete";
            deleteItem.Style = menuItemStyle;
            deleteItem.Click += (s, e) =>
            {
                var confirm = Dialogs.ConfirmDialog($"Delete {presetName}?", "Delete");            
                if (confirm)
                {
                    int activeId = Config.GetActivePresetId();
                    Config.DeletePreset(presetId);
                    Config.SafeSave();
                    UpdatePresets();
                    if (activeId == presetId)
                    {
                        _viewModel.OnDisplaySettingsChanged(null, null);
                    }
                }
            };   
            
            return contextMenu;
        }

        private void UpdatePresets()
        {
            var menuItemStyle = (Style)FindResource("MenuItemStyle");

            var presets = Config.GetPresetNames();
            var activePreset = Config.GetActivePresetId();

            var items = new List<ComboBoxItem>();

            for (int i = 0; i < presets.Length; i++)
            {
                int presetId = i;
                var presetName = presets[i];

                var contextMenu = CreateContextMenu(menuItemStyle, presetId, presetName);

                var item = new ComboBoxItem();
                item.Content = presetName;
                item.ContextMenu = contextMenu;
                item.Selected += (s, e) =>
                {
                    if (presetId != Config.GetActivePresetId())
                    {
                        Config.SetActivePreset(presetId);
                        Config.SafeSave();
                        _viewModel.OnDisplaySettingsChanged(null, null);
                    }
                };

                items.Add(item);
            }

            var addNewItem = new ComboBoxItem();
            addNewItem.Content = "+";           
            items.Add(addNewItem);

            Presets.ItemsSource = items;
            Presets.SelectedIndex = activePreset;
        }

        private void Presets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Presets.SelectedIndex == Presets.Items.Count - 1)
            {
                Presets.SelectedIndex = -1;
                Config.AddPreset();
                Config.SafeSave();
                UpdatePresets();
            }
        }

        private void ReapplyMonitorSettings()
        {
            _viewModel.ReapplyAll();
        }
    }
}