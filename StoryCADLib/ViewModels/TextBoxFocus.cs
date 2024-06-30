using Microsoft.UI.Xaml;

namespace StoryCAD.ViewModels
    {
        public static class TextBoxFocus
        {
            public static bool GetIsFocused(DependencyObject obj)
            {
                return (bool)obj.GetValue(IsFocusedProperty);
            }

            public static void SetIsFocused(DependencyObject obj, bool value)
            {
                obj.SetValue(IsFocusedProperty, value);
            }

            public static readonly DependencyProperty IsFocusedProperty =
                DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(TextBoxFocus), new PropertyMetadata(false, OnIsFocusedPropertyChanged));

            private static void OnIsFocusedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    ((TextBox)d).Loaded += TextBox_Loaded;
                }
                else
                {
                    ((TextBox)d).Loaded -= TextBox_Loaded;
                }
            }

            private static void TextBox_Loaded(object sender, RoutedEventArgs e)
            {
                ((TextBox)sender).Focus(FocusState.Programmatic);
                ((TextBox)sender).SelectAll();
            }
        }
    }
