using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfTest;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        for (int i = 0; i < 8; i++)
        {
            Datas.Add(new TestClass { Name = $"Test{i + 1}" });
        }

        SelectedData.CollectionChanged += SelectedData_CollectionChanged;
    }

    [ObservableProperty]
    private string testText = string.Empty;

    private void SelectedData_CollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e
    )
    {
        TestText = $"总数为{SelectedData.Count}";
    }

    public ObservableCollection<TestClass> Datas { get; } = [];

    public ObservableCollection<TestClass> SelectedData { get; } = [];
}

public class TestClass
{
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}
