using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;

namespace StoryBuilder.Services.Navigation;
// ****************************************************************************
// <copyright file="NavigationService.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>02.10.2014</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

////[ClassInfo(typeof(INavigationService))]
///
/// NOTE: This class is derived from the MVVMLight navigation service by
/// Laurent Bugnion mentioned above. The only change to the original code is
/// to add a pair of NavigateTo() methods which have an additional parameter
/// prefixed to them, which specifies the Frame on which to display the Page,
/// rather than than using Window.Current.Content. This was required in order
/// to display StoryElement pages in the right-hand-side Frame (subframe) of SplitView.
/// Note that if if a Frame is passed, GoBack isn't available; using it will
/// get you in trouble. 
/// <summary>
/// Windows 10 UWP implementation of <see ref="INavigationService"/>.
/// </summary>
public class NavigationService
{
    /// <summary>
    /// The key that is returned by the <see cref="CurrentPageKey"/> property
    /// when the current Page is the root page.
    /// </summary>
    public const string RootPageKey = "-- ROOT --";

    /// <summary>
    /// The key that is returned by the <see cref="CurrentPageKey"/> property
    /// when the current Page is not found.
    /// This can be the case when the navigation wasn't managed by this NavigationService,
    /// for example when it is directly triggered in the code behind, and the
    /// NavigationService was not configured for this page type.
    /// </summary>
    public const string UnknownPageKey = "-- UNKNOWN --";

    private readonly Dictionary<string, Type> _pagesByKey = new();
    private Frame _currentFrame;

    /// <summary>
    /// Gets or sets the Frame that should be use for the navigation.
    /// If this is not set explicitly, then (Frame)Window.Current.Content is used.
    /// </summary>
    public Frame CurrentFrame
    {
        get => _currentFrame ??= (Frame)Window.Current.Content;

        set => _currentFrame = value;
    }

    /// <summary>
    /// The key corresponding to the currently displayed page.
    /// </summary>
    public string CurrentPageKey
    {
        get
        {
            lock (_pagesByKey)
            {
                if (CurrentFrame.BackStackDepth == 0)
                {
                    return RootPageKey;
                }

                if (CurrentFrame.Content == null)
                {
                    return UnknownPageKey;
                }

                Type _CurrentType = CurrentFrame.Content.GetType();

                if (_pagesByKey.All(p => p.Value != _CurrentType))
                {
                    return UnknownPageKey;
                }

                KeyValuePair<string, Type> _Item = _pagesByKey.FirstOrDefault(
                    i => i.Value == _CurrentType);

                return _Item.Key;
            }
        }
    }

    /// <summary>
    /// Displays a new page corresponding to the given key. 
    /// Make sure to call the <see cref="Configure"/>
    /// method first.
    /// </summary>
    /// <param name="pageKey">The key corresponding to the page
    /// that should be displayed.</param>
    /// <exception cref="ArgumentException">When this method is called for 
    /// a key that has not been configured earlier.</exception>
    public void NavigateTo(string pageKey)
    {
        NavigateTo(pageKey, null);
    }

    /// <summary>
    /// Displays a new page corresponding to the given key,
    /// and passes a parameter to the new page.
    /// Make sure to call the <see cref="Configure"/>
    /// method first.
    /// </summary>
    /// <param name="pageKey">The key corresponding to the page
    /// that should be displayed.</param>
    /// <param name="parameter">The parameter that should be passed
    /// to the new page.</param>
    /// <exception cref="ArgumentException">When this method is called for 
    /// a key that has not been configured earlier.</exception>
    public virtual void NavigateTo(string pageKey, object parameter)
    {
        lock (_pagesByKey)
        {
            if (!_pagesByKey.ContainsKey(pageKey))
            {
                throw new ArgumentException($"No such page: {pageKey}. Did you forget to call NavigationService.Configure?", pageKey);
            }

            CurrentFrame.Navigate(_pagesByKey[pageKey], parameter);
        }
    }

    /// <summary>
    /// Displays a new page corresponding to the given key in the
    /// specified frame. 
    /// Make sure to call the <see cref="Configure"/>
    /// method first.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="pageKey">The key corresponding to the page
    /// that should be displayed.</param>
    /// <exception cref="ArgumentException">When this method is called for 
    /// a key that has not been configured earlier.</exception>
    public void NavigateTo(Frame frame, string pageKey)
    {
        NavigateTo(frame, pageKey, null);
    }

    /// <summary>
    /// Displays a new page corresponding to the given key,
    /// and passes a parameter to the new page.
    /// Make sure to call the <see cref="Configure"/>
    /// method first.
    /// </summary>
    /// <param name="frame"></param>
    /// 
    /// <param name="pageKey">The key corresponding to the page
    /// that should be displayed.</param>
    /// <param name="parameter">The parameter that should be passed
    /// to the new page.</param>
    /// <exception cref="ArgumentException">When this method is called for 
    /// a key that has not been configured earlier.</exception>
    public virtual void NavigateTo(Frame frame, string pageKey, object parameter)
    {
        lock (_pagesByKey)
        {
            if (!_pagesByKey.ContainsKey(pageKey))
            {
                throw new ArgumentException($"No such page: {pageKey}. Did you forget to call NavigationService.Configure?", pageKey);
            }

            frame.Navigate(_pagesByKey[pageKey], parameter);
            GlobalData.PageKey = pageKey;
        }
    }

    /// <summary>
    /// Adds a key/page pair to the navigation service.
    /// </summary>
    /// <param name="key">The key that will be used later
    /// in the <see cref="NavigateTo(string)"/> or <see cref="NavigateTo(string, object)"/> methods.</param>
    /// <param name="pageType">The type of the page corresponding to the key.</param>
    public void Configure(string key, Type pageType)
    {
        lock (_pagesByKey)
        {
            if (_pagesByKey.ContainsKey(key))
            {
                throw new ArgumentException("This key is already used: " + key);
            }

            if (_pagesByKey.Any(p => p.Value == pageType))
            {
                throw new ArgumentException(
                    "This type is already configured with key " + _pagesByKey.First(p => p.Value == pageType).Key);
            }

            _pagesByKey.Add(
                key,
                pageType);
        }
    }
}