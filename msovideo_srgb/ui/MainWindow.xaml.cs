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
            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;
            SystemEvents.DisplaySettingsChanged += _viewModel.OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged += _viewModel.OnPowerModeChanged;

            GlobalEventsObserver.OnDisplayWake += _viewModel.OnDisplaySettingsChanged;
            GlobalEventsObserver.OnSessionUnlock += _viewModel.OnDisplaySettingsChanged;
            GlobalEventsObserver.OnHotKey += _viewModel.OnHotkey;

            ProcessArgs();

            InitializeTrayIcon();
        }

        private void ProcessArgs()
        {
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0);

            string helpMessage = "";
            bool autoclose = false;
            foreach (var arg in args)
            {
                if (arg.Equals("-minimize", StringComparison.OrdinalIgnoreCase))
                {
                    WindowState = WindowState.Minimized;
                    Hide();
                }
                else if (arg.Equals("-autoclose", StringComparison.OrdinalIgnoreCase))
                {
                    WindowState = WindowState.Minimized;
                    Hide();
                    autoclose = true;
                }
                else if (arg.StartsWith("-preset=", StringComparison.OrdinalIgnoreCase))
                {
                    string val = arg.Substring("-preset=".Length);
                    if (int.TryParse(val, out int presetId))
                    {
                        presetId--;
                        presetId = Math.Max(presetId, 0);
                        presetId = Math.Min(presetId, _viewModel.Presets.Count - 1);
                        _viewModel.ActivePreset = _viewModel.Presets[presetId];
                    }
                    else
                    {
                        helpMessage += $"Must be a number: {val}\n";
                    }
                }
                else if (arg.Equals("-force", StringComparison.OrdinalIgnoreCase));
                else
                {
                    helpMessage += $"Unknown argument: {arg}\n";
                }
            }
            if (helpMessage != "")
            {
                helpMessage += "\n";
                helpMessage += "-force\t\tClose other instances at startup\n";
                helpMessage += "-minimize\tMinimize at startup\n";
                helpMessage += "-autoclose\tClose after startup\n";
                helpMessage += "-preset=<id>\tSet preset #<id> at startup";

                MessageBox.Show(helpMessage);
            }
            if (autoclose)
            {
                ActionScheduler.Add("autoclose", () =>
                {
                    App.CurrentApp.OnExit();
                    Environment.Exit(1);
                });
            }
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Preset preset)
            {
                string action = menuItem.Header.ToString();
                if (action == "Rename")
                {
                    string name = Dialogs.InputDialog(preset.Name, "Rename");
                    if (name != null && name != preset.Name)
                    {
                        _viewModel.RenamePreset(preset, name);
                    }
                }
                else if (action == "Hotkey")
                {
                    var hotkey = Dialogs.HotkeyDialog(preset.Hotkey, $"{preset.Name}: ", "Press or select hotkey");
                    if (hotkey != null && !hotkey.Equals(preset.Hotkey))
                    {
                        if (!_viewModel.Presets.Any(p => hotkey.IsBindable && hotkey.Equals(p.Hotkey)))
                        {
                            _viewModel.SetPresetHotkey(preset, hotkey);
                        }
                        else
                        {
                            Dialogs.NotifyDialog($"{hotkey}\nAlready used!", preset.Name);
                        }
                    }
                }
                else if (action == "Delete")
                {
                    var confirm = Dialogs.ConfirmDialog($"Delete {preset.Name}?", "Delete");
                    if (confirm)
                    {
                        _viewModel.DeletePreset(preset);
                    }
                }
            }
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

            var activePreset = _viewModel.ActivePreset;

            foreach (var preset in _viewModel.Presets)
            {
                if (preset.Id == -1) continue;
                
                var item = new MenuItemWF();
                _contextMenu.MenuItems.Add(item);
                item.Text = preset.Name;
                item.Checked = preset == activePreset;
                item.Click += (s, e) => _viewModel.ActivePreset = preset;
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

        private void ReapplyMonitorSettings()
        {
            _viewModel.ReapplyAll();
        }
    }
}