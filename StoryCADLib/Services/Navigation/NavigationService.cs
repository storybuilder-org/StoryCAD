using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StoryCAD.Services.Navigation
{
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
    //
    // NOTE: This class is derived from the MVVMLight navigation service by
    // Laurent Bugnion mentioned above. The only change to the original code is
    // to add a pair of NavigateTo() methods which have an additional parameter
    // prefixed to them, which specifies the Frame on which to display the Page,
    // rather than using Window.Current.Content. This was required in order
    // to display StoryElement pages in the right-hand-side Frame (subframe) of
    // SplitView. Note that if a Frame is passed, GoBack isn't available; using it
    // will get you in trouble.

    /// <summary>
    /// WinUI 3 implementation of <see cref="INavigationService"/>.
    /// </summary>
    public class NavigationService : INavigationService
    {
        public const string RootPageKey = "-- ROOT --";
        public const string UnknownPageKey = "-- UNKNOWN --";

        private readonly Dictionary<string, Type> _pagesByKey = new();
        private Frame _currentFrame;

        /// <summary>Frame used for navigation; falls back to the app’s main window.</summary>
        public Frame CurrentFrame
        {
            get => _currentFrame ??= (Frame)Ioc.Default.GetRequiredService<Windowing>().MainWindow.Content;
            set => _currentFrame = value;
        }

        public bool CanGoBack => CurrentFrame.CanGoBack;
        public bool CanGoForward => CurrentFrame.CanGoForward;

        public void GoForward()
        {
            if (CurrentFrame.CanGoForward) CurrentFrame.GoForward();
        }

        public string CurrentPageKey
        {
            get
            {
                lock (_pagesByKey)
                {
                    if (CurrentFrame.BackStackDepth == 0) return RootPageKey;
                    if (CurrentFrame.Content is null) return UnknownPageKey;

                    Type currentType = CurrentFrame.Content.GetType();
                    if (_pagesByKey.All(p => p.Value != currentType)) return UnknownPageKey;

                    return _pagesByKey.First(p => p.Value == currentType).Key;
                }
            }
        }

        public void GoBack()
        {
            if (CurrentFrame.CanGoBack) CurrentFrame.GoBack();
        }

        #region Navigate API ----------------------------------------------------

        public void NavigateTo(string pageKey) => NavigateTo(pageKey, null);

        public virtual void NavigateTo(string pageKey, object parameter)
        {
            lock (_pagesByKey)
            {
                if (!_pagesByKey.TryGetValue(pageKey, out var pageType))
                    throw new ArgumentException(
                        $"No such page: {pageKey}. Did you forget to call NavigationService.Configure?",
                        pageKey);

                PerformNavigation(CurrentFrame, pageType, parameter);
            }
        }

        public void NavigateTo(Frame frame, string pageKey) => NavigateTo(frame, pageKey, null);

        public virtual void NavigateTo(Frame frame, string pageKey, object parameter)
        {
            lock (_pagesByKey)
            {
                if (!_pagesByKey.TryGetValue(pageKey, out var pageType))
                    throw new ArgumentException(
                        $"No such page: {pageKey}. Did you forget to call NavigationService.Configure?",
                        pageKey);

                PerformNavigation(frame, pageType, parameter);
                Ioc.Default.GetRequiredService<Windowing>().PageKey = pageKey;
            }
        }

        /// <summary>
        /// Deactivate current VM → navigate → activate new VM.
        /// </summary>
        private static void PerformNavigation(Frame frame, Type targetPage, object parameter)
        {
            if (frame.Content is FrameworkElement currentPage &&
                currentPage.DataContext is INavigable currentVm)
            {
                currentVm.Deactivate(parameter);
            }

            bool navigated = frame.Navigate(targetPage, parameter);

            if (navigated &&
                frame.Content is FrameworkElement newPage &&
                newPage.DataContext is INavigable newVm)
            {
                newVm.Activate(parameter);
            }
        }

        #endregion -------------------------------------------------------------

        #region Configuration helpers ------------------------------------------

        public void Configure(string key, Type pageType)
        {
            lock (_pagesByKey)
            {
                if (_pagesByKey.ContainsKey(key))
                    throw new ArgumentException($"This key is already used: {key}");

                if (_pagesByKey.Any(p => p.Value == pageType))
                    throw new ArgumentException(
                        $"This type is already configured with key {_pagesByKey.First(p => p.Value == pageType).Key}");

                _pagesByKey.Add(key, pageType);
            }
        }

        public string GetKeyForPage(Type page)
        {
            lock (_pagesByKey)
            {
                if (_pagesByKey.ContainsValue(page))
                    return _pagesByKey.First(p => p.Value == page).Key;

                throw new ArgumentException($"The page '{page.Name}' is unknown by the NavigationService");
            }
        }

        #endregion -------------------------------------------------------------
    }
}
