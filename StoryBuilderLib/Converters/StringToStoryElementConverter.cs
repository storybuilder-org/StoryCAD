using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;

namespace StoryBuilder.Converters
{
    /// <summary>
    /// Several controls (CharacterName and SettingName)
    /// really refer to a StoryElement by its Name property.
    /// It's important to bind to the StoryElement rather than
    /// the name, which can be changed at any time. To allow
    /// this, StoryElement references are stored and retrieved 
    /// as the string values of their Guids, and this converter 
    /// is used when binding to the target control from the 
    /// string source.
    /// </summary>
    public class StringToStoryElementConverter : IValueConverter
    {
        /// <summary>
        /// Convert a string to its StoryElement instance.
        /// 
        /// The string is normally the Guid of a story element
        /// and is looked up from the StoryModel's StoryElements
        /// collection. 
        /// 
        /// Legacy (V1) story outlines may also contain a
        /// StoryElement's Name value as a string. In that case
        /// the first StoryElement with a matching name is returned.
        /// 
        /// If the source isn't a string, or is empty or not one of 
        /// the above, null is returned.
        /// </summary>
        /// <param name="value">The source parameter</param>
        /// <param name="targetType">The source's Type</param>
        /// <param name="parameter">The target (UI's) Type</param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;
            if (value.GetType() != typeof(string))
                return null;
            if (value.Equals(string.Empty))
                return null;
            // Get the current StoryModel's StoryElementsCollection
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            StoryElementCollection elements = shell.StoryModel.StoryElements;
            // legacy: locate the StoryElement from its Name
            foreach (StoryElement element in elements)  // Character or Setting??? Search both?
            {
                if (element.Type == StoryItemType.Character | element.Type == StoryItemType.Setting)
                {
                    string source = (string)value;
                    if (source.Trim().Equals(element.Name.Trim()))
                        return element;
                }
            }
            // Look for the StoryElement corresponding to the passed guid
            // (This is the normal approach)
            Guid guid = new Guid(value.ToString());
            if (elements.StoryElementGuids.ContainsKey(guid))
                return elements.StoryElementGuids[guid];
            return null;   // Not found
        }

        /// <summary>
        /// For two-way binding, The StoryElement's Guid is returned as a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return string.Empty;
            StoryElement element = (StoryElement)value;
            return element.Uuid.ToString();
        }

    }
}
