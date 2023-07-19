using System.Windows;

namespace EyeshotDesignTestApp;

/// <summary>
/// Defines an attached property for loading tab items lazily
/// <see cref="http://stackoverflow.com/questions/3274629/lazy-loading-wpf-tab-content"/>
/// </summary>
public class Deferred
{
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.RegisterAttached(
                                            "Content",
                                            typeof(object),
                                            typeof(Deferred),
                                            new PropertyMetadata());

    public static object GetContent(DependencyObject obj)
    {
        return obj.GetValue(ContentProperty);
    }

    public static void SetContent(DependencyObject obj, object value)
    {
        obj.SetValue(ContentProperty, value);
    }
}