using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace 云端管理
{
    public partial class CloudSettingsWindow : Window
    {
        public CloudSettingsWindow()
        {
            InitializeComponent();
            LoadCurrentConfig();
        }

        private void LoadCurrentConfig()
        {
            var config = CloudSyncManager.GetConfig();
            if (config != null)
            {
                UrlInput.Text = config.Url;
                UserInput.Text = config.User;
                PwdInput.Password = config.Password;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlInput.Text) ||
                string.IsNullOrWhiteSpace(UserInput.Text) ||
                string.IsNullOrWhiteSpace(PwdInput.Password))
            {
                MessageBox.Show("请填写完整的 WebDAV 信息。", "提示");
                return;
            }

            var config = new CloudConfig
            {
                Url = UrlInput.Text.Trim().EndsWith("/") ? UrlInput.Text.Trim() : UrlInput.Text.Trim() + "/",
                User = UserInput.Text.Trim(),
                Password = PwdInput.Password,
                IsEnabled = true
            };

            var originalContent = ((System.Windows.Controls.Button)sender).Content;
            ((System.Windows.Controls.Button)sender).Content = "验证中...";
            ((System.Windows.Controls.Button)sender).IsEnabled = false;

            try
            {
                CloudSyncManager.SaveConfig(config);

                string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.dat");
                bool success = await CloudSyncManager.DownloadLatestAsync(dataPath);

                if (success)
                {
                    MessageBox.Show("云端连接成功！已同步最新的加密配置文件。", "同步成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("云端连接成功！未发现历史同步文件，后续修改将自动上传。", "连接成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                config.IsEnabled = false;
                CloudSyncManager.SaveConfig(config);
                MessageBox.Show($"云端验证失败，请检查地址或账号密码。\n错误详情: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ((System.Windows.Controls.Button)sender).Content = originalContent;
                ((System.Windows.Controls.Button)sender).IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}