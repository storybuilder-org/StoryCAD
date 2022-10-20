using System;
using Microsoft.UI.Xaml.Data;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Converters;

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
        StoryModel _Model = ShellViewModel.GetModel();
        StoryElementCollection _Elements = _Model.StoryElements;
        // legacy: locate the StoryElement from its Name
        foreach (StoryElement _Element in _Elements)  // Character or Setting??? Search both?
        {
            if (_Element.Type == StoryItemType.Character | _Element.Type == StoryItemType.Setting)
            {
                string _Source = (string)value;
                if (_Source.Trim().Equals(_Element.Name.Trim()))
                    return _Element;
            }
        }
        // Look for the StoryElement corresponding to the passed guid
        // (This is the normal approach)
        if (Guid.TryParse(value.ToString(), out Guid _Guid))
            if (_Elements.StoryElementGuids.ContainsKey(_Guid))
                return _Elements.StoryElementGuids[_Guid];
        return null;  // Not found
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
        StoryElement _Element = (StoryElement)value;
        return _Element.Uuid.ToString();
    }

}