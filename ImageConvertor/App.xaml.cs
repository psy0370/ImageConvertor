using ControlzEx.Theming;
using MaterialDesignThemes.Wpf;
using System.Windows;

namespace ImageConvertor
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // MahApps.MetroのテーマをWindowsの設定に追従させる
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
            ThemeManager.Current.SyncTheme();
        }

        /// <summary>
        /// テーマが変更されたときの処理を定義します。
        /// </summary>
        private void Current_ThemeChanged(object sender, ControlzEx.Theming.ThemeChangedEventArgs e)
        {
            // Material DesignのテーマをMahApps.Metroのテーマに追従させる
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            var bTheme = e.NewTheme.BaseColorScheme == "Dark" ? new MaterialDesignDarkTheme() : (IBaseTheme)new MaterialDesignLightTheme();
            theme.SetBaseTheme(bTheme);
            helper.SetTheme(theme);
        }
    }
}
