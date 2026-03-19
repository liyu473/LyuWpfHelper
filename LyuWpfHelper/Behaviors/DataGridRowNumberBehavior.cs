using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LyuWpfHelper.Behaviors;

/// <summary>
/// DataGrid 行序号辅助类
/// 自动为 DataGrid 添加序号列，并在数据变化时自动刷新
/// </summary>
public static class DataGridRowNumberBehavior
{
    private static readonly Dictionary<DataGrid, DataGridTemplateColumn> _columnCache = [];

    /// <summary>
    /// 是否启用行序号
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridRowNumberBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>
    /// 序号列标题
    /// </summary>
    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.RegisterAttached(
            "HeaderText",
            typeof(string),
            typeof(DataGridRowNumberBehavior),
            new PropertyMetadata("序号", OnHeaderTextChanged));

    /// <summary>
    /// 序号起始值（默认从1开始）
    /// </summary>
    public static readonly DependencyProperty StartIndexProperty =
        DependencyProperty.RegisterAttached(
            "StartIndex",
            typeof(int),
            typeof(DataGridRowNumberBehavior),
            new PropertyMetadata(1, OnStartIndexChanged));

    /// <summary>
    /// 序号列宽度
    /// </summary>
    public static readonly DependencyProperty ColumnWidthProperty =
        DependencyProperty.RegisterAttached(
            "ColumnWidth",
            typeof(DataGridLength),
            typeof(DataGridRowNumberBehavior),
            new PropertyMetadata(new DataGridLength(50), OnColumnWidthChanged));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    public static string GetHeaderText(DependencyObject obj)
    {
        return (string)obj.GetValue(HeaderTextProperty);
    }

    public static void SetHeaderText(DependencyObject obj, string value)
    {
        obj.SetValue(HeaderTextProperty, value);
    }

    public static int GetStartIndex(DependencyObject obj)
    {
        return (int)obj.GetValue(StartIndexProperty);
    }

    public static void SetStartIndex(DependencyObject obj, int value)
    {
        obj.SetValue(StartIndexProperty, value);
    }

    public static DataGridLength GetColumnWidth(DependencyObject obj)
    {
        return (DataGridLength)obj.GetValue(ColumnWidthProperty);
    }

    public static void SetColumnWidth(DependencyObject obj, DataGridLength value)
    {
        obj.SetValue(ColumnWidthProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;

        if ((bool)e.NewValue)
        {
            AddRowNumberColumn(dataGrid);

            dataGrid.LoadingRow += OnLoadingRow;
            dataGrid.Sorting += OnSorting;

            // 监听数据源变化
            var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                ItemsControl.ItemsSourceProperty, typeof(DataGrid));
            descriptor?.AddValueChanged(dataGrid, (s, args) => OnItemsSourceChanged(dataGrid));

            OnItemsSourceChanged(dataGrid);
        }
        else
        {
            RemoveRowNumberColumn(dataGrid);
            dataGrid.LoadingRow -= OnLoadingRow;
            dataGrid.Sorting -= OnSorting;
        }
    }

    private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid && _columnCache.TryGetValue(dataGrid, out var column))
        {
            column.Header = e.NewValue;
        }
    }

    private static void OnStartIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid)
        {
            RefreshRowNumbers(dataGrid);
        }
    }

    private static void OnColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid && _columnCache.TryGetValue(dataGrid, out var column))
        {
            column.Width = (DataGridLength)e.NewValue;
        }
    }

    private static void AddRowNumberColumn(DataGrid dataGrid)
    {
        if (_columnCache.ContainsKey(dataGrid))
            return;

        // 使用 DataGridTemplateColumn 而不是 DataGridTextColumn
        // 这样可以完全控制内容显示，避免 Binding 警告和闪烁问题
        var column = new DataGridTemplateColumn
        {
            Header = GetHeaderText(dataGrid),
            Width = GetColumnWidth(dataGrid),
            IsReadOnly = true,
            CanUserSort = false,
            CanUserResize = true
        };

        // 创建单元格模板
        var cellTemplate = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.SetValue(TextBlock.TextProperty, string.Empty); // 默认为空，通过 LoadingRow 更新
        cellTemplate.VisualTree = factory;
        column.CellTemplate = cellTemplate;

        dataGrid.Columns.Insert(0, column);
        _columnCache[dataGrid] = column;
    }

    private static void RemoveRowNumberColumn(DataGrid dataGrid)
    {
        if (_columnCache.TryGetValue(dataGrid, out var column))
        {
            dataGrid.Columns.Remove(column);
            _columnCache.Remove(dataGrid);
        }
    }

    private static void OnItemsSourceChanged(DataGrid dataGrid)
    {
        if (dataGrid.ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += (s, args) => RefreshRowNumbers(dataGrid);
        }

        RefreshRowNumbers(dataGrid);
    }

    private static void OnSorting(object? sender, DataGridSortingEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            // 排序完成后刷新行号
            dataGrid.Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshRowNumbers(dataGrid);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private static void OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            UpdateRowNumber(dataGrid, e.Row);
        }
    }

    private static void UpdateRowNumber(DataGrid dataGrid, DataGridRow row)
    {
        if (!_columnCache.ContainsKey(dataGrid))
            return;

        int startIndex = GetStartIndex(dataGrid);
        int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);

        if (index >= 0 && row.Item is not null)
        {
            // 更新单元格内容
            dataGrid.Dispatcher.BeginInvoke(new Action(() =>
            {
                var cell = GetCell(dataGrid, row, 0);
                if (cell is not null)
                {
                    var textBlock = new TextBlock
                    {
                        Text = (startIndex + index).ToString(),
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    cell.Content = textBlock;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private static void RefreshRowNumbers(DataGrid dataGrid)
    {
        if (!_columnCache.ContainsKey(dataGrid))
            return;

        dataGrid.Dispatcher.BeginInvoke(new Action(() =>
        {
            int startIndex = GetStartIndex(dataGrid);

            for (int i = 0; i < dataGrid.Items.Count; i++)
            {
                var item = dataGrid.Items[i];
                if (dataGrid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                {
                    var cell = GetCell(dataGrid, row, 0);
                    if (cell is not null)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = (startIndex + i).ToString(),
                            TextAlignment = TextAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        cell.Content = textBlock;
                    }
                }
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private static DataGridCell? GetCell(DataGrid dataGrid, DataGridRow row, int columnIndex)
    {
        if (row == null || columnIndex < 0 || columnIndex >= dataGrid.Columns.Count)
            return null;

        var presenter = GetVisualChild<DataGridCellsPresenter>(row);
        if (presenter == null)
            return null;

        var cell = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        if (cell == null)
        {
            dataGrid.ScrollIntoView(row, dataGrid.Columns[columnIndex]);
            cell = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        }

        return cell;
    }

    private static T? GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        T? child = null;
        int numVisuals = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < numVisuals; i++)
        {
            var v = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            child = v as T ?? GetVisualChild<T>(v);
            if (child != null)
                break;
        }

        return child;
    }
}
