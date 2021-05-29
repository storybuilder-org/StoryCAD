using System.Threading.Tasks;

namespace StoryBuilder.Services.Navigation
{
    public interface INavigable
    {   Task  Activate(object parameter);
        Task Deactivate(object parameter);
    }
}
