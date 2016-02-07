using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ACCurrentSensing.View
{
    public class StateManager : DependencyObject
    {
        public static readonly DependencyProperty VisualStateProperty = DependencyProperty.RegisterAttached(
            "VisualState", 
            typeof (string), 
            typeof (StateManager), 
            new PropertyMetadata(
                default(string),
                (o, args) =>
                {
                    var newState = (string)args.NewValue;
                    var control = o as Control;
                    if (control != null)
                    {
                        VisualStateManager.GoToState(control, newState, true);
                    }
                }));

        public static void SetVisualState(DependencyObject element, string value)
        {
            element.SetValue(VisualStateProperty, value);
        }

        public static string GetVisualState(DependencyObject element)
        {
            return (string) element.GetValue(VisualStateProperty);
        }
    }
}
