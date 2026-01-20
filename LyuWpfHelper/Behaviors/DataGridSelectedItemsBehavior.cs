using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Behaviors;

/// <summary>
/// DataGrid 多选绑定辅助类
/// 用于将 DataGrid 的 SelectedItems 绑定到 ViewModel 中的集合
/// </summary>
public static class DataGridSelectedItemsBehavior
{
    private static readonly Dictionary<DataGrid, bool> _isUpdating = [];

    /// <summary>
    /// SelectedItems 依赖属性
    /// </summary>
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(DataGridSelectedItemsBehavior),
            new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

    /// <summary>
    /// 获取绑定的选中项集合
    /// </summary>
    public static IList GetSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(SelectedItemsProperty);
    }

    /// <summary>
    /// 设置绑定的选中项集合
    /// </summary>
    public static void SetSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(SelectedItemsProperty, value);
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;


        dataGrid.SelectionChanged -= OnDataGridSelectionChanged;

        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= (sender, args) => OnViewModelCollectionChanged(dataGrid, args);
        }


        if (e.NewValue is IList newList)
        {
            dataGrid.SelectionChanged += OnDataGridSelectionChanged;

            if (newList is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += (sender, args) => OnViewModelCollectionChanged(dataGrid, args);
            }

            // 初始化选中项
            SyncDataGridSelection(dataGrid, newList);
        }
    }

    private static void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
            return;

        var boundCollection = GetSelectedItems(dataGrid);
        if (boundCollection == null)
            return;

        if (_isUpdating.TryGetValue(dataGrid, out var isUpdating) && isUpdating)
            return;

        _isUpdating[dataGrid] = true;

        try
        {
            foreach (var item in e.RemovedItems)
            {
                if (boundCollection.Contains(item))
                {
                    boundCollection.Remove(item);
                }
            }

            foreach (var item in e.AddedItems)
            {
                if (!boundCollection.Contains(item))
                {
                    boundCollection.Add(item);
                }
            }
        }
        finally
        {
            _isUpdating[dataGrid] = false;
        }
    }

    private static void OnViewModelCollectionChanged(DataGrid dataGrid, NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdating.TryGetValue(dataGrid, out var isUpdating) && isUpdating)
            return;

        var boundCollection = GetSelectedItems(dataGrid);
        if (boundCollection == null)
            return;

        _isUpdating[dataGrid] = true;

        try
        {
            SyncDataGridSelection(dataGrid, boundCollection);
        }
        finally
        {
            _isUpdating[dataGrid] = false;
        }
    }

    private static void SyncDataGridSelection(DataGrid dataGrid, IList selectedItems)
    {
        dataGrid.SelectedItems.Clear();

        if (selectedItems == null)
            return;

        foreach (var item in selectedItems)
        {
            if (!dataGrid.SelectedItems.Contains(item))
            {
                dataGrid.SelectedItems.Add(item);
            }
        }
    }
}
