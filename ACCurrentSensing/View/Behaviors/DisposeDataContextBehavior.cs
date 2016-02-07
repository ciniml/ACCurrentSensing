using System;
using Windows.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace ACCurrentSensing.View.Behaviors
{
    public class DisposeDataContextBehavior : DependencyObject, IBehavior
    {
        public DependencyObject AssociatedObject { get; private set; }

        private RoutedEventHandler unloadedEventHandler;

        public void Attach(DependencyObject associatedObject)
        {
            var page = associatedObject as Windows.UI.Xaml.Controls.Page;
            this.unloadedEventHandler = (o, e) => { (page.DataContext as IDisposable)?.Dispose(); };
            if (page != null)
            {
                page.Unloaded += this.unloadedEventHandler;
            }
            this.AssociatedObject = associatedObject;
        }
        
        public void Detach()
        {
            var page = this.AssociatedObject as Windows.UI.Xaml.Controls.Page;
            if (page != null)
            {
                page.Unloaded -= this.unloadedEventHandler;
            }
        }

        public object Execute(object sender, object parameter)
        {
            return null;
        }
    }
}
