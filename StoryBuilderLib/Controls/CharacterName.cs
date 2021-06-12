using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using StoryBuilder.Models;
using System.Collections.ObjectModel;

namespace StoryBuilder.Controls
{
    public sealed class CharacterName : ComboBox
    {
        private CollectionViewSource charViewSource;
        
        public CharacterName()
        {
            DefaultStyleKey = typeof(CharacterName);
            charViewSource = new CollectionViewSource();
            //charViewSource.Source = new ObservableCollection<StoryElement>
            //    (StoryElement.StoryElements.Values
                 
            //var x = charViewSource
        }
    }
}
