using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace 云端管理
{
    public partial class MainWindow : Window
    {
        private List<SshProfile> _profiles = new List<SshProfile>();
        private string _masterPassword;
        private string _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.dat");

        public MainWindow(string password)
        {
            InitializeComponent();
            _masterPassword = password;
            RefreshList();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void RefreshList()
        {
            try
            {
                _profiles = ProfileManager.LoadProfiles(_masterPassword);
                ServerListBox.ItemsSource = null;
                ServerListBox.ItemsSource = _profiles;
            }
            catch (Exception ex)
            {
                if (!(ex is FileNotFoundException)) MessageBox.Show(ex.Message);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var win = new EditServerWindow();
            win.Owner = this;
            if (win.ShowDialog() == true)
            {
                _profiles.Add(win.ResultProfile);
                SaveAndRefresh();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ServerListBox.SelectedItem is SshProfile selected)
            {
                var win = new EditServerWindow(selected);
                win.Owner = this;
                if (win.ShowDialog() == true)
                {
                    var index = _profiles.IndexOf(selected);
                    _profiles[index] = win.ResultProfile;
                    SaveAndRefresh();
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ServerListBox.SelectedItem is SshProfile selected)
            {
                if (MessageBox.Show($"确定删除 {selected.Name} 吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _profiles.Remove(selected);
                    SaveAndRefresh();
                }
            }
        }

        private async void SaveAndRefresh()
        {
            // 1. 保存到本地
            ProfileManager.SaveProfiles(_profiles, _masterPassword);
            RefreshList();

            // 2. 异步上传到云端并清理旧版本
            await CloudSyncManager.UploadAndCleanAsync(_dataFile);
        }

        private void ServerListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ServerListBox.SelectedItem != null) Connect_Click(null, null);
        }

        private void Connect_Click(object sender, RoutedEventArgs? e)
        {
            if (ServerListBox.SelectedItem is SshProfile server)
            {
                string sshArgs = $"-p {server.Port} ";
                string tempKeyPath = "";
                string titleMsg = $"【{server.Name}】";

                if (server.AuthType == "Key")
                {
                    tempKeyPath = Path.Combine(Path.GetTempPath(), $"ssh_temp_{Guid.NewGuid()}");
                    File.WriteAllText(tempKeyPath, server.SecretData);
                    FixKeyPermissions(tempKeyPath);
                    sshArgs += $"-i \"{tempKeyPath}\" {server.Username}@{server.Host}";
                }
                else
                {
                    sshArgs += $"{server.Username}@{server.Host}";

                    // 核心修复：自动将密码复制到剪贴板
                    if (!string.IsNullOrEmpty(server.SecretData))
                    {
                        try
                        {
                            Clipboard.SetText(server.SecretData);
                            titleMsg += " (密码已复制，右键即可粘贴)";
                        }
                        catch { /* 忽略极低概率的剪贴板占用冲突 */ }
                    }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k title {titleMsg} && echo 正在连接 {server.Host}... && ssh {sshArgs}",
                    UseShellExecute = true
                });
            }
        }

        private void FixKeyPermissions(string keyPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "icacls.exe", Arguments = $"\"{keyPath}\" /inheritance:r", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })?.WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = "icacls.exe", Arguments = $"\"{keyPath}\" /grant \"{Environment.UserName}:F\"", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })?.WaitForExit();
            }
            catch { }
        }
    }
}