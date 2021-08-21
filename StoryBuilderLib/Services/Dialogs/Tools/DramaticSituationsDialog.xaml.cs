using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels.Tools;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Services.Dialogs.Tools
{
    public sealed partial class DramaticSituationsDialog
    {
        public DramaticSituationsViewModel DramaticSituationsVm => Ioc.Default.GetService<DramaticSituationsViewModel>();
        public DramaticSituationsDialog()
        {
            InitializeComponent();
        }
    }
}
