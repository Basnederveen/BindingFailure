using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.UI.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BindingFailure.ViewModels;
public partial class CanvasView : ItemsControl
{
    private (DependencyProperty, string)[] LiftedProperties = new (DependencyProperty, string)[] {
        (Canvas.LeftProperty, "(Canvas.Left)"),
        (Canvas.TopProperty, "(Canvas.Top)"),
        (Canvas.ZIndexProperty, "(Canvas.ZIndex)"),
        (ManipulationModeProperty, "ManipulationMode")
    };

    private bool dragging;

    public CanvasView()
    {
        // TODO: Need to use XamlReader because of https://github.com/microsoft/microsoft-ui-xaml/issues/2898
        ItemsPanel = XamlReader.Load("" +
                                     "<ItemsPanelTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                                     "<Canvas>" +
                                        "<Canvas.Background>" +
                                            "<ImageBrush ImageSource=\"../Assets/drawing.png\" />" +
                                        "</Canvas.Background>" +
                                     "</Canvas>" +
                                     "</ItemsPanelTemplate>") as ItemsPanelTemplate;
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        // ContentPresenter is the default container for Canvas.
        if (element is ContentPresenter cp)
        {
            _ = CompositionTargetHelper.ExecuteAfterCompositionRenderingAsync(() =>
            {
                SetupChildBinding(cp);
            });

            // Loaded is not firing when dynamically loading an element to the collection. Relay on CompositionTargetHelper above.
            // Seems like a bug in Loaded event?
            cp.Loaded += ContentPresenter_Loaded;
            cp.ManipulationDelta += ContentPresenter_ManipulationDelta;
            cp.PointerMoved += ContentPresenter_PointerMoved;
        }

        /// TODO: Do we want to support something else in a custom template??
        /// else if (item is FrameworkElement fe && fe.FindDescendant/GetContentControl?)
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        base.ClearContainerForItemOverride(element, item);

        if (element is ContentPresenter cp)
        {
            cp.Loaded -= ContentPresenter_Loaded;
            cp.ManipulationDelta -= ContentPresenter_ManipulationDelta;
            cp.PointerMoved -= ContentPresenter_PointerMoved;
        }
    }

    private void ContentPresenter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            var point = e.GetCurrentPoint(sender as UIElement);
            if (point.Properties.IsMiddleButtonPressed)
            {
                dragging = true;
            }
            else
            {
                dragging = false;
            }
        }
    }

    private void ContentPresenter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        // Move the rectangle.
        if (sender is ContentPresenter cp && dragging)
        {
            // TODO: Seeing some drift, not sure if due to DPI or just general drift
            // or probably we need to do the start/from delta approach we did with SizerBase to resolve.

            // We know that most likely these values have been bound to a data model object of some sort
            // Therefore, we need to use this helper to update the underlying model value of our bound property.
            var left = Canvas.GetLeft(cp) + e.Delta.Translation.X;
            var top = Canvas.GetTop(cp) + e.Delta.Translation.Y;

            cp.SetBindingExpressionValue(Canvas.TopProperty, top);
            cp.SetBindingExpressionValue(Canvas.LeftProperty, left);
        }
    }

    private void ContentPresenter_Loaded(object sender, RoutedEventArgs args)
    {
        if (sender is ContentPresenter cp)
        {
            cp.Loaded -= ContentPresenter_Loaded;

            SetupChildBinding(cp);
        }
    }

    private void SetupChildBinding(ContentPresenter cp)
    {
        // Get direct visual descendant for ContentPresenter to look for Canvas properties within Template.
        var child = VisualTreeHelper.GetChild(cp, 0);

        if (child != null)
        {
            // TODO: Should we avoid doing this twice?

            // Hook up any properties we care about from the templated children to it's parent ContentPresenter.
            foreach ((var prop, var path) in LiftedProperties)
            {
                var binding = new Binding();
                binding.Source = child;
                ////binding.Mode = BindingMode.TwoWay; // TODO: Should this be exposed as a general property?
                binding.Path = new PropertyPath(path);

                cp.SetBinding(prop, binding);
            }
        }
    }
}


public static partial class FrameworkElementExtensions
{
    /// <summary>
    /// Normally when trying to set a value of a <see cref="DependencyProperty"/> this will update the raw value of the property
    /// and break any <see cref="Binding"/> associated with that property. This method instead retrieves the underlying
    /// <see cref="BindingExpression"/> of the <see cref="DependencyProperty"/>, if one exists, and instead updates
    /// the underlying bound property value directly (using reflection). This is an advanced technique and has not been
    /// widely tested, use with caution.
    /// </summary>
    /// <param name="fe">The <see cref="FrameworkElement"/> with the property to update.</param>
    /// <param name="property">The <see cref="DependencyProperty"/> to update the underlying bound value of.</param>
    /// <param name="value">The new value to update the bound property to.</param>
    public static void SetBindingExpressionValue(this FrameworkElement fe, DependencyProperty property, object value)
    {
        var subBinding = fe.GetBindingExpression(property);
        if (subBinding == null)
        {
            fe.SetValue(property, value);
        }
        else if (subBinding.DataItem is FrameworkElement subfe)
        {
            subfe.SetBindingExpressionValue(property, value);
        }
        else if (subBinding.DataItem != null && subBinding.ParentBinding.Path != null)
        {
            var prop = subBinding.DataItem.GetType().GetProperty(subBinding.ParentBinding.Path.Path);

            prop?.SetValue(subBinding.DataItem, Convert.ChangeType(value, prop.PropertyType));
        }
    }
}