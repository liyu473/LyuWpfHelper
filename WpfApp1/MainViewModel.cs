using CommunityToolkit.Mvvm.ComponentModel;
using LyuWpfHelper.ViewModels;
using System.Collections.ObjectModel;

namespace WpfApp1;

public partial class MainViewModel: ViewModelBase
{
    public MainViewModel()
    {
        for(int i = 0; i < 8; i++)
        {
            Datas.Add(new TestClass { Name = $"Test{i + 1}" });
        }
    }

    public ObservableCollection<TestClass> Datas { get; } = [];

    [ObservableProperty]
    private TestClass? selectedItem;
}

public class TestClass
{
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}
