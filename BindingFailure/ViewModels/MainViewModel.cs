using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BindingFailure.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private static ViewConfiguration baseView = new()
    {
        X = 150,
        Y = 150,
    };

    public static List<ViewConfiguration> Views = new()
    {
            baseView,
            new()
            {
                BaseView = baseView,
                X = 150,
                Y = 300,
            },
            new()
            {
                BaseView = baseView,
                X = 325,
                Y = 150,
            },
            new()
            {
                BaseView = baseView,
                X = 400,
                Y = 400,
            }
    };

    public MainViewModel()
    {
        ViewConfigurations = GetViews();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OtherViewConfigurations))]
    private ViewConfigurationViewModel? selectedViewConfiguration;

    [ObservableProperty]
    private ObservableCollection<ViewConfigurationViewModel>? viewConfigurations;
    public List<ViewConfigurationViewModel>? OtherViewConfigurations => ViewConfigurations?.Where(x => x != SelectedViewConfiguration).ToList();
    
    public ObservableCollection<ViewConfigurationViewModel> GetViews()
    {
        var vms = new List<ViewConfigurationViewModel>(Views
            .Select(x => new ViewConfigurationViewModel(x, SheetSizes.A2)));

        // go through the created viewmodels and assign the base view(model)
        int i = 1;
        foreach (var vm in vms)
        {
            // assign a view number
            vm.ViewConfiguration.Number = i;
            i++;

            if (vm.ViewConfiguration.BaseView == null) { continue; }

            var otherVms = vms.Where(x => x != vm).ToList();

            vm.BaseView = otherVms.FirstOrDefault(x => x.ViewConfiguration == vm.ViewConfiguration.BaseView)!;
        }

        return new ObservableCollection<ViewConfigurationViewModel>(vms);
    }
    
    [RelayCommand]
    private void ViewConfigurationClicked(ViewConfigurationViewModel item)
    {
        // item.BaseView is not null here
        SelectedViewConfiguration = item;

        var test = OtherViewConfigurations.Contains(item.BaseView);

        // Here you can see that item.BaseView is not null, and that it is contained in OtherViewConfigurations
    }
}

