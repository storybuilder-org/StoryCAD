using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views
{
    public sealed partial class SettingPage : BindablePage
    {
        public SettingViewModel SettingVm => Ioc.Default.GetService<SettingViewModel>();

        public SettingPage()
        {
            InitializeComponent();
            DataContext = SettingVm;
        }
    }
}
