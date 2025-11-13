using MiniDashboard.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MiniDashboard.App.Behaviors
{
    public static class DataGridAutoColumnsBehavior
    {
        public static readonly DependencyProperty AutoColumnsProperty =
            DependencyProperty.RegisterAttached(
                "AutoColumns",
                typeof(bool),
                typeof(DataGridAutoColumnsBehavior),
                new PropertyMetadata(false, OnAutoColumnsChanged));

        public static bool GetAutoColumns(DependencyObject obj) =>
            (bool)obj.GetValue(AutoColumnsProperty);

        public static void SetAutoColumns(DependencyObject obj, bool value) =>
            obj.SetValue(AutoColumnsProperty, value);

        private static void OnAutoColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid) return;

            if ((bool)e.NewValue)
            {
                grid.AutoGeneratingColumn += (s, ev) => ev.Cancel = true; // prevent default auto-generation
                grid.Loaded += (s, ev) => SetupColumns(grid);
            }
        }

        private static void SetupColumns(DataGrid grid)
        {
            if (grid.DataContext is not ItemsViewModel vm) return;

            // Clear existing columns
            grid.Columns.Clear();

            // Initial column creation
            GenerateColumns(grid, vm);

            // Listen for changes in Columns collection
            vm.Columns.CollectionChanged += (s, ev) => GenerateColumns(grid, vm);
        }

        private static void GenerateColumns(DataGrid grid, ItemsViewModel vm)
        {
            grid.Columns.Clear();
            foreach (ViewModels.ColumnDefinition col in vm.Columns)
            {
                DataGridTextColumn column = new DataGridTextColumn
                {
                    Header = col.Header,
                    Binding = new Binding($"[{col.Header}]")
                };
                grid.Columns.Add(column);
            }
        }
    }
    public static class DataGridCommitBehavior
    {
        public static readonly DependencyProperty CommitEditsOnClickProperty =
            DependencyProperty.RegisterAttached(
                "CommitEditsOnClick",
                typeof(bool),
                typeof(DataGridCommitBehavior),
                new PropertyMetadata(false, OnChanged));

        public static bool GetCommitEditsOnClick(Button button)
            => (bool)button.GetValue(CommitEditsOnClickProperty);

        public static void SetCommitEditsOnClick(Button button, bool value)
            => button.SetValue(CommitEditsOnClickProperty, value);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    button.PreviewMouseDown += (s, ev) =>
                    {
                        Window window = Window.GetWindow(button);
                        if (window != null)
                        {
                            DataGrid? dg = FindDataGrid(window);
                            dg?.CommitEdit(DataGridEditingUnit.Row, true);
                            dg?.CommitEdit(DataGridEditingUnit.Cell, true);
                        }
                    };
                }
            }
        }

        private static DataGrid FindDataGrid(DependencyObject parent)
        {
            if (parent == null) return null;
            if (parent is DataGrid dg) return dg;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DataGrid result = FindDataGrid(VisualTreeHelper.GetChild(parent, i));
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
