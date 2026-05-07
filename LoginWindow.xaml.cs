using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace 云端管理
{
    public partial class LoginWindow : Window
    {
        private string _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.dat");

        public LoginWindow()
        {
            InitializeComponent();
            // 订阅 Loaded 事件，因为云端检测是异步的，不建议直接在构造函数里跑
            this.Loaded += LoginWindow_Loaded;
            PasswordInput.Focus();
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            var config = CloudSyncManager.GetConfig();
            bool localFileExists = File.Exists(_dataFile);
            bool cloudEnabled = config != null && config.IsEnabled;

            // 核心逻辑：本地数据丢失，但云同步配置存在
            if (!localFileExists && cloudEnabled)
            {
                var result = MessageBox.Show(
                    "检测到本地数据缺失，但云同步已配置。\n是否从云端拉取配置库？\n\n(选择“否”将清空云同步并重新新建本地库)",
                    "数据恢复建议",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusLabel.Text = "正在从云端拉取数据...";
                    LoginBtn.IsEnabled = false;

                    bool success = await CloudSyncManager.DownloadLatestAsync(_dataFile);

                    if (success)
                    {
                        StatusLabel.Text = "同步成功，请输入主密码解锁:";
                    }
                    else
                    {
                        MessageBox.Show("云端拉取失败，请检查网络或 WebDAV 配置。", "同步失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusLabel.Text = "请手动设置主密码或检查配置:";
                    }
                    LoginBtn.IsEnabled = true;
                }
                else
                {
                    // 用户选择不拉取，彻底销毁内存与本地的云同步配置数据
                    CloudSyncManager.ClearConfig();
                    StatusLabel.Text = "已重置。请设置新的主密码:";
                }
            }
            // 纯粹的首次启动
            else if (!localFileExists)
            {
                StatusLabel.Text = "首次启动，请设置主密码:";
            }
            // 正常登录
            else
            {
                StatusLabel.Text = "请输入主密码解锁:";
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void CloudSettings_Click(object sender, RoutedEventArgs e)
        {
            var cloudWin = new CloudSettingsWindow();
            cloudWin.Owner = this;
            if (cloudWin.ShowDialog() == true)
            {
                // 如果在设置窗口里配置成功了，回来重新跑一遍状态检查
                _ = InitializeAppAsync();
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await VerifyAndLogin();
        }

        private async void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await VerifyAndLogin();
        }

        private async Task VerifyAndLogin()
        {
            string pwd = PasswordInput.Password;
            if (string.IsNullOrWhiteSpace(pwd)) return;

            LoginBtn.IsEnabled = false;
            string originalStatus = StatusLabel.Text;

            try
            {
                var config = CloudSyncManager.GetConfig();

                // 再次确认：如果是登录现有库，先跑一次同步，确保拿到云端最新的 profiles_{timestamp}.dat
                if (File.Exists(_dataFile) && config.IsEnabled)
                {
                    StatusLabel.Text = "同步中...";
                    await CloudSyncManager.DownloadLatestAsync(_dataFile);
                }

                // 验证密码
                if (File.Exists(_dataFile))
                {
                    try
                    {
                        ProfileManager.LoadProfiles(pwd);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("主密码错误，请重试。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusLabel.Text = originalStatus;
                        PasswordInput.Clear();
                        LoginBtn.IsEnabled = true;
                        return;
                    }
                }
                else
                {
                    // 如果文件还是不存在（用户新建库），直接进入主界面，
                    // 主界面在后续保存时会自动根据 pwd 生成 profiles.dat
                }

                MainWindow mainWindow = new MainWindow(pwd);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误: {ex.Message}");
                StatusLabel.Text = originalStatus;
                LoginBtn.IsEnabled = true;
            }
        }
    }
}