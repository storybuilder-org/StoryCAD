namespace StoryCAD.Services.Navigation;

// ****************************************************************************
// <copyright file="INavigationService.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>30.09.2014</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

////[ClassInfo(typeof(INavigationService),
////    VersionString = "5.3.5",
////    DateString = "201604212130",
////    UrlContacts = "http://www.galasoft.ch/contact_en.html",
////    Email = "laurent@galasoft.ch")]
/// NOTE: This interface is derived from the MVVMLight navigation service by
/// Laurent Bugnion mentioned bove. The only change to the original code is
/// to add a pair of NavigateTo() methods which have an additional parameter
/// prefixed to them, which specifies the Frame on which to display the Page,
/// rather than than using Window.Current.Content. This was required in order
/// to dplay to pages in the right-hand-side Frame (subframe) of SplitView.
/// <summary>
///     An interface defining how navigation between pages should
///     be performed in various frameworks such as Windows,
///     Windows Phone, Android, iOS etc.
/// </summary>
public interface INavigationService
{
    /// <summary>
    ///     The key corresponding to the currently displayed page.
    /// </summary>
    string CurrentPageKey { get; }

    /// <summary>
    ///     If possible, instructs the navigation service
    ///     to discard the current page and display the previous page
    ///     on the navigation stack.
    /// </summary>
    void GoBack();

    /// <summary>
    ///     Instructs the navigation service to display a new page
    ///     corresponding to the given key. Depending on the platforms,
    ///     the navigation service might have to be configured with a
    ///     key/page list.
    /// </summary>
    /// <param name="pageKey">
    ///     The key corresponding to the page
    ///     that should be displayed.
    /// </param>
    void NavigateTo(string pageKey);

    /// <summary>
    ///     Instructs the navigation service to display a new page
    ///     corresponding to the given key, and passes a parameter
    ///     to the new page.
    ///     Depending on the platforms, the navigation service might
    ///     have to be Configure with a key/page list.
    /// </summary>
    /// <param name="pageKey">
    ///     The key corresponding to the page
    ///     that should be displayed.
    /// </param>
    /// <param name="parameter">
    ///     The parameter that should be passed
    ///     to the new page.
    /// </param>
    void NavigateTo(string pageKey, object parameter);

    /// <summary>
    ///     Instructs the navigation service to display a new page
    ///     corresponding to the given key. The page is displayed on
    ///     a specified frame.
    ///     Depending on the platforms, the navigation service might
    ///     have to be configured with a key/page list.
    ///     <param name="frame"> The frame or subframe on which to display the page </param>
    ///     <param name="pageKey">The key corresponding to the page that should be displayed.</param>
    /// </summary>
    void NavigateTo(Frame frame, string pageKey);

    /// <summary>
    ///     Instructs the navigation service to display a new page
    ///     corresponding to the given key, and passes a parameter
    ///     to the new page. The page is displayed in a specified frame.
    ///     Depending on the platforms, the navigation service might
    ///     have to be Configure with a key/page list.
    /// </summary>
    /// <param name="frame"> The frame or subframe on which to display the page </Param>
    /// <param name="pageKey">
    ///     The key corresponding to the page
    ///     that should be displayed.
    /// </param>
    /// <param name="parameter">
    ///     The parameter that should be passed
    ///     to the new page.
    /// </param>
    void NavigateTo(Frame frame, string pageKey, object parameter);
}
