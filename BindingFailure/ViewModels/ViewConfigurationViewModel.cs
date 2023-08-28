using CommunityToolkit.Mvvm.ComponentModel;

namespace BindingFailure.ViewModels;


/// <summary>
/// 
/// </summary>
public class ViewConfiguration
{
    public double X
    {
        get; set;
    }
    public double Y
    {
        get; set;
    }

    public int Number
    {
        get; set;
    }

    // the base view
    private ViewConfiguration? baseView = null;
    public ViewConfiguration? BaseView
    {
        get => baseView;
        set
        {
            baseView = value;
        }
    }
}

public record SheetSize(string Name, double Height, double Width);

public class SheetSizes
{
    public static readonly SheetSize A0 = new("A0", 841, 1189);
    public static readonly SheetSize A1 = new("A1", 594, 841);
    public static readonly SheetSize A2 = new("A2", 420, 594);
    public static readonly SheetSize A3 = new("A3", 297, 420);
    public static readonly SheetSize A4 = new("A4", 210, 297);

    public static readonly SheetSize[] All = new[] { A0, A1, A2, A3, A4 };
}

public partial class ViewConfigurationViewModel : ObservableObject
{
    public readonly ViewConfiguration ViewConfiguration;
    private readonly SheetSize sheetSize;

    // TODO: move this
    private readonly int canvasHeight = 600;
    private readonly int canvasWidth = 900;

    public int Number => ViewConfiguration.Number;

    public ViewConfigurationViewModel(ViewConfiguration viewConfiguration, SheetSize sheetSize)
    {
        this.ViewConfiguration = viewConfiguration;
        this.sheetSize = sheetSize;

        Top = (canvasHeight * (sheetSize.Height - viewConfiguration.Y)) / sheetSize.Height;
        Left = (canvasWidth * viewConfiguration.X) / sheetSize.Width;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Y))]
    [NotifyPropertyChangedFor(nameof(WritableY))]
    private double left;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(X))]
    [NotifyPropertyChangedFor(nameof(WritableX))]
    private double top;

    partial void OnTopChanged(double value)
    {
        var actualValue = (int)((canvasHeight - value) * sheetSize.Height / canvasHeight);

        ViewConfiguration.Y = actualValue;
    }

    partial void OnLeftChanged(double value)
    {
        var actualValue = (int)((value * sheetSize.Width) / canvasWidth);

        ViewConfiguration.X = actualValue;
    }

    public double X => ViewConfiguration.X;
    public double Y => ViewConfiguration.Y;

    public double WritableX
    {
        get => ViewConfiguration.X;
        set
        {
            SetProperty(ViewConfiguration.X, value, ViewConfiguration, (u, n) => u.X = n);

            Left = (canvasWidth * value) / sheetSize.Width;

            OnPropertyChanged(nameof(X));
        }
    }

    public double WritableY
    {
        get => ViewConfiguration.Y;
        set
        {
            SetProperty(ViewConfiguration.Y, value, ViewConfiguration, (u, n) => u.Y = n);

            Top = (canvasHeight * (sheetSize.Height - value)) / sheetSize.Height;

            OnPropertyChanged(nameof(Y));
        }
    }

    //[ObservableProperty]
    //private ViewConfigurationViewModel? baseView;

    //partial void OnBaseViewChanged(ViewConfigurationViewModel? value)
    //{
    //    if (value == null)
    //    {
    //        return;
    //    }
    //    if (ViewConfiguration.BaseView != value?.ViewConfiguration) 
    //        ViewConfiguration.BaseView = value?.ViewConfiguration;
    //}

    private ViewConfigurationViewModel baseView;
    public ViewConfigurationViewModel BaseView 
    { 
        get => baseView;
        set
        {
            if (value == null)
            {
                return;
            }

            baseView = value;

            if (ViewConfiguration.BaseView != value?.ViewConfiguration)
                ViewConfiguration.BaseView = value?.ViewConfiguration;

            OnPropertyChanged();
        }
    }

}