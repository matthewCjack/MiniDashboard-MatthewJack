using System.Collections;
using System.ComponentModel;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MiniDashboard.App.Helpers
{
    public static class DataGridColumnsHelper
    {
        public static readonly DependencyProperty AutoGenerateColumnsFromDynamicItemsProperty =
            DependencyProperty.RegisterAttached(
                "AutoGenerateColumnsFromDynamicItems",
                typeof(bool),
                typeof(DataGridColumnsHelper),
                new PropertyMetadata(false, OnAutoGenerateColumnsFromDynamicItemsChanged));

        public static bool GetAutoGenerateColumnsFromDynamicItems(DependencyObject obj)
            => (bool)obj.GetValue(AutoGenerateColumnsFromDynamicItemsProperty);

        public static void SetAutoGenerateColumnsFromDynamicItems(DependencyObject obj, bool value)
            => obj.SetValue(AutoGenerateColumnsFromDynamicItemsProperty, value);

        private static void OnAutoGenerateColumnsFromDynamicItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dataGrid) return;
            if (!(e.NewValue is bool newVal) || !newVal) return;

            // Listen for ItemsSource changes
            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(DataGrid.ItemsSourceProperty, typeof(DataGrid));
            descriptor.AddValueChanged(dataGrid, (s, ev) => GenerateColumns(dataGrid));

            // Initial generation if ItemsSource is already set
            GenerateColumns(dataGrid);
        }

        private static void GenerateColumns(DataGrid dataGrid)
        {
            dataGrid.Columns.Clear();
            if (dataGrid.ItemsSource is IEnumerable items)
            {
                foreach (object? firstItem in items)
                {
                    if (firstItem is IDictionary<string, object> dict)
                    {
                        foreach (string key in dict.Keys)
                        {
                            dataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = key,
                                Binding = new Binding($"[{key}]")
                            });
                        }
                        break; // only need the first item
                    }

                    if (firstItem is ExpandoObject expando)
                    {
                        foreach (KeyValuePair<string, object> kvp in (IDictionary<string, object>)expando)
                        {
                            dataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = kvp.Key,
                                Binding = new Binding($"[{kvp.Key}]")
                            });
                        }
                        break;
                    }
                }
            }
        }
    }
}
