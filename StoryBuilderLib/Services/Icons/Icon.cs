namespace Mvvm.Services
{
    public static class Icon
    {
        public static string GetIcon(string name)
        {
            return (string) Microsoft.UI.Xaml.Application.Current.Resources[name];
        }
    }
}
