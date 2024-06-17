using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow2 : Window
    {
        private const int InitCulumnCount = 640;

        public ObservableCollection<Detail> Items { get; private set; } = new ObservableCollection<Detail>();

        private ScrollSynchronizer? _scrollSynchronizer;

        public ReactivePropertySlim<bool> IsDataGridEnabled { get; } = new(true);

        public MainWindow2()
        {
            InitializeComponent();

            InitData(InitCulumnCount);

            IsDataGridEnabled.Where(x => !x).Subscribe(
                x =>
                {
                    grid.UnselectAllCells();
                });
        }

        #region 画面の初期化

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            Cursor = Cursors.Wait;

            InitColumns(InitCulumnCount);

            squares.ScrollViewer = previewScroll;

            grid.Visibility = Visibility.Visible;

            Dispatcher.InvokeAsync(() =>
            {
                Cursor = null;

                InitScrollSynchronizer();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void InitScrollSynchronizer()
        {
            var scrollList = new List<ScrollViewer>();
            scrollList.Add(previewScroll);
            var gridScroll = DataGridHelper.GetScrollViewer(grid);
            if (gridScroll is not null)
            {
                scrollList.Add(gridScroll);
            }
            _scrollSynchronizer = new ScrollSynchronizer(scrollList);
        }
        #endregion

        #region 行列の初期化

        private void InitColumns(int count)
        {
            grid.Columns.Clear();

            var converter = new BooleanToVisibilityConverter();
            for (int columnIndex = 0; columnIndex < count; ++columnIndex)
            {
                var binding = new Binding($"Values[{columnIndex}].Value");
                binding.Converter = converter;

                var factory = new FrameworkElementFactory(typeof(Ellipse));
                factory.SetValue(Ellipse.HeightProperty, 15.0);
                factory.SetValue(Ellipse.WidthProperty, 15.0);
                factory.SetValue(Ellipse.FillProperty, new SolidColorBrush(Color.FromRgb(0xe2, 0xaf, 0x42)));
                factory.SetBinding(Ellipse.VisibilityProperty, binding);

                var dataTemplate = new DataTemplate();
                dataTemplate.VisualTree = factory;

                var column = new DataGridTemplateColumn();
                column.CellTemplate = dataTemplate;

                grid.Columns.Add(column);
            }
        }

        private void InitData(int count)
        {
            // バインドを切断
            Binding b = new Binding("Items")
            {
                Source = null
            };
            grid.SetBinding(DataGrid.ItemsSourceProperty, b);

            var list = new List<Detail>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new Detail(InitCulumnCount));
            }

            Items = new ObservableCollection<Detail>(list);

            b = new Binding("Items")
            {
                Source = this
            };
            grid.SetBinding(DataGrid.ItemsSourceProperty, b);
        }
        #endregion

        #region 設定値変更

        private void DataGridCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsDataGridEnabled.Value)
            {
                return;
            }

            if (grid.SelectedCells.Count == 1)
            {
                var columnIndex = DataGridHelper.GetSelectedColumnIndex(grid);
                var rowIndex = DataGridHelper.GetSelectedRowIndex(grid);

                Items[rowIndex].Invert(columnIndex);

                UpdatePreview();
            }
        }

        private void DataGridCell_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsDataGridEnabled.Value)
            {
                return;
            }

            if (grid.SelectedCells.Count <= 1)
            {
                grid.Focus();
                grid.SelectedCells.Clear();

                DataGridCell? targetCell = DataGridHelper.GetCellAtMousePosition(sender, e);

                if (targetCell is null) return;
                grid.CurrentCell = new DataGridCellInfo(targetCell);
                grid.SelectedCells.Add(grid.CurrentCell);

                ShowContextMenu(false);
            }
            else
            {
                ShowContextMenu(true);
            }
        }

        private void ShowContextMenu(bool isSelectArea)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Header = "行全部設定";
            menuItem.Click += new RoutedEventHandler(AllOn);
            menuItem.IsEnabled = !isSelectArea;
            contextMenu.Items.Add(menuItem);

            Separator separator = new Separator();
            contextMenu.Items.Add(separator);

            menuItem = new MenuItem();
            menuItem.Header = "選択エリア設定";
            menuItem.Click += new RoutedEventHandler(AreaOn);
            menuItem.IsEnabled = isSelectArea;
            contextMenu.Items.Add(menuItem);

            contextMenu.IsOpen = true;
        }

        private void AllOn(object sender, RoutedEventArgs e)
        {
            var rowIndex = DataGridHelper.GetSelectedRowIndex(grid);
            Items[rowIndex].SetAll(true);

            UpdatePreview();
        }

        private void AreaOn(object sender, RoutedEventArgs e)
        {
            var indexes = DataGridHelper.GetSelectedCellsIndex(grid);
            foreach (var index in indexes)
            {
                Items[index.RowIndex].SetOn(index.ColumnIndex);
            }

            UpdatePreview();
        }

        #endregion

        #region ミニマップ

        #region 表示位置

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            var thumb = sender as Thumb;
            if (null != thumb)
            {
                var border = thumb.Template.FindName("Thumb_Border", thumb) as Border;
                if (null != border)
                {
                    border.BorderThickness = new Thickness(1);

                    e.Handled = true;
                }
            }
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var thumb = sender as Thumb;
            if (null != thumb)
            {
                var border = thumb.Template.FindName("Thumb_Border", thumb) as Border;
                if (null != border)
                {
                    border.BorderThickness = new Thickness(0);

                    e.Handled = true;
                }
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            if (null != thumb)
            {
                var x = Canvas.GetRight(thumb) - e.HorizontalChange;
                var y = Canvas.GetBottom(thumb) - e.VerticalChange;

                var canvas = thumb.Parent as Canvas;
                if (null != canvas)
                {
                    x = Math.Max(x, 0);
                    y = Math.Max(y, 0);
                    x = Math.Min(x, canvas.ActualWidth - thumb.ActualWidth);
                    y = Math.Min(y, canvas.ActualHeight - thumb.ActualHeight);
                }

                Canvas.SetRight(thumb, x);
                Canvas.SetBottom(thumb, y);

                e.Handled = true;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetRight(map, 0);
            Canvas.SetBottom(map, 0);

            squares.InvalidateVisual();
            MoveScroll();
        }

        #endregion

        #region スクロール

        private void Thumb_DragStarted2(object sender, DragStartedEventArgs e)
        {
            var thumb = sender as Thumb;
            if (null != thumb)
            {
                var border = thumb.Template.FindName("Area_Thumb_Border", thumb) as Border;
                if (null != border)
                {
                    border.BorderThickness = new Thickness(1);
                }

                gridPanel.Children.Remove(grid);

                previewScroll.Visibility = Visibility.Visible;

                squares.InvalidateVisual();

                e.Handled = true;
            }
        }

        private void Thumb_DragCompleted2(object sender, DragCompletedEventArgs e)
        {
            Cursor = Cursors.Wait;

            var thumb = sender as Thumb;
            if (null != thumb)
            {
                var border = thumb.Template.FindName("Area_Thumb_Border", thumb) as Border;
                if (null != border)
                {
                    border.BorderThickness = new Thickness(0);
                }

                gridPanel.Children.Insert(2, grid);

                Dispatcher.InvokeAsync(() =>
                {
                    if (gridPanel.Children.Contains(grid))  // ドラッグ開始終了の連続動作時にプレビュー表示されなくなる問題回避のため
                    {
                        previewScroll.Visibility = Visibility.Collapsed;
                    }

                    Cursor = null;
                }, System.Windows.Threading.DispatcherPriority.Background);

                e.Handled = true;
            }
        }

        private void Thumb_DragDelta2(object sender, DragDeltaEventArgs e)
        {
            VerticalScrollDelta(sender, e);
            HorizontalScrollDelta(sender, e);

            e.Handled = true;
        }

        private void MoveMiniMapThumb((double HorizontalRatio, double VerticalRatio) ratios)
        {
            var mapCanvas = map.Template.FindName("Area_Canvas", map) as Canvas;
            if (null == mapCanvas) return;
            var mapThumb = map.Template.FindName("Area_Thumb", map) as Thumb;
            if (null == mapThumb) return;

            var x = mapCanvas.ActualWidth * ratios.HorizontalRatio;
            var y = mapCanvas.ActualHeight * ratios.VerticalRatio;

            Canvas.SetLeft(mapThumb, x);
            Canvas.SetTop(mapThumb, y);
        }

        #endregion

        #endregion

        #region ダミー表示

        private void UpdatePreview()
        {
            squares.Objects.Clear();

            var rowMax = Items.Count;
            var colmunMax = Items[0].Values.Count;

            for (int rowIndex = 0; rowIndex < rowMax; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < colmunMax; columnIndex++)
                {
                    if (Items[rowIndex].Values[columnIndex].Value)
                    {
                        Square square = CreateSquare(rowIndex, columnIndex);

                        squares.Objects.Add(square);
                    }
                }
            }
        }

        private static Square CreateSquare(int rowIndex, int columnIndex)
        {
            var square = new Square();
            square.Width = 16;
            square.Height = 16;
            Canvas.SetTop(square, 28 * rowIndex + 6);
            Canvas.SetLeft(square, 28 * columnIndex + 6);
            return square;
        }

        private void previewScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            squares.InvalidateVisual();

            MoveScroll();
        }

        private void MoveScroll()
        {
            var ratios = GetScrollRatio(previewScroll);
            MoveMiniMapThumb(ratios);
            MoveHorizontalScrollThumb(ratios);
            MoveVerticalScrollThumb(ratios);
        }

        private static (double HorizontalRatio, double VerticalRatio) GetScrollRatio(ScrollViewer scroll)
        {
            var gridWidth = scroll.ScrollableWidth;
            var gridHeight = scroll.ScrollableHeight;

            return ((scroll.HorizontalOffset / gridWidth), (scroll.VerticalOffset / gridHeight));
        }

        #endregion

        #region スクロールバー　垂直

        private void MoveVerticalScrollThumb((double HorizontalRatio, double VerticalRatio) ratios)
        {
            var y = verticalScrollCanvas.ActualHeight * ratios.VerticalRatio;
            Canvas.SetTop(verticalScrollThumb, y);
        }

        private void verticalScrollThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            VerticalScrollDelta(sender, e);

            e.Handled = true;
        }

        private void VerticalScrollDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            if (null != thumb)
            {

                var y = Canvas.GetTop(thumb) + e.VerticalChange;

                var canvas = thumb.Parent as Canvas;
                if (null != canvas)
                {

                    y = Math.Max(y, 0);
                    y = Math.Min(y, canvas.ActualHeight - thumb.ActualHeight);

                    Canvas.SetTop(thumb, y);

                    DataGridHelper.MoveVerticalScrollTo(grid, Canvas.GetTop(thumb) / canvas.ActualHeight);

                    previewScroll.ScrollToVerticalOffset(previewScroll.ScrollableHeight * (Canvas.GetTop(thumb) / canvas.ActualHeight));
                }
            }
        }

        #endregion

        #region スクロールバー　水平

        private void MoveHorizontalScrollThumb((double HorizontalRatio, double VerticalRatio) ratios)
        {
            var x = horizontalScrollCanvas.ActualWidth * ratios.HorizontalRatio;
            Canvas.SetLeft(horizontalScrollThumb, x);
        }

        private void horizontalScrollThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            HorizontalScrollDelta(sender, e);

            e.Handled = true;
        }

        private void HorizontalScrollDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            if (null != thumb)
            {
                var x = Canvas.GetLeft(thumb) + e.HorizontalChange;

                var canvas = thumb.Parent as Canvas;
                if (null != canvas)
                {

                    x = Math.Max(x, 0);
                    x = Math.Min(x, canvas.ActualWidth - thumb.ActualWidth);

                    Canvas.SetLeft(thumb, x);

                    DataGridHelper.MoveHorizontalScrollTo(grid, Canvas.GetLeft(thumb) / canvas.ActualWidth);

                    previewScroll.ScrollToHorizontalOffset(previewScroll.ScrollableWidth * (Canvas.GetLeft(thumb) / canvas.ActualWidth));
                }
            }
        }

        #endregion

        private FillHandleAdorner? fillHandleAdorner = null;

        private void grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var datagrid = (System.Windows.Controls.DataGrid)sender;
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(datagrid);
            if (datagrid.SelectedCells.Any())
            {
                DataGridCellInfo firstCellInfo = datagrid.SelectedCells.First();
                FrameworkElement firstElt = firstCellInfo.Column.GetCellContent(firstCellInfo.Item);

                DataGridCellInfo lastCellInfo = datagrid.SelectedCells.Last();
                FrameworkElement lastElt = lastCellInfo.Column.GetCellContent(lastCellInfo.Item);

                if (firstElt != null && lastElt != null)
                {
                    var firstcell = (DataGridCell)firstElt.Parent;
                    var lastCell = (DataGridCell)lastElt.Parent;
                    Point topLeft = datagrid.PointFromScreen(firstcell.PointToScreen(new Point(0, 0)));
                    Point bottomRight = datagrid.PointFromScreen(lastCell.PointToScreen(new Point(lastCell.ActualWidth, lastCell.ActualHeight)));
                    var rect = new Rect(topLeft, bottomRight);
                    if (fillHandleAdorner == null)
                    {
                        fillHandleAdorner = new FillHandleAdorner(datagrid, rect);
                        adornerLayer.Add(fillHandleAdorner);
                    }
                    else
                        fillHandleAdorner.Rect = rect;
                }
            }
            else
            {
                adornerLayer.Remove(fillHandleAdorner);
                fillHandleAdorner = null;
            }
        }
    }

    internal class FillHandleAdorner : Adorner
    {
        Rect rect;

        readonly VisualCollection visualChildren;

        System.Windows.Controls.DataGrid dataGrid;

        public Rect Rect
        {
            get { return rect; }
            set
            {
                rect = value;
                InvalidateVisual();
            }
        }

        public FillHandleAdorner(UIElement adornedElement, Rect rect)
            : base(adornedElement)
        {
            dataGrid = (System.Windows.Controls.DataGrid)adornedElement;
            Rect = rect;

            visualChildren = new VisualCollection(this);
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xec, 0xa1, 0x2a)));
        }

        protected override int VisualChildrenCount
        {
            get { return visualChildren.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visualChildren[index];
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var blackSolidBrush = new SolidColorBrush(Color.FromRgb(0xec, 0xa1, 0x2a));
            var pen = new Pen(blackSolidBrush, 3);
            pen.Freeze();
            double halfPenWidth = pen.Thickness / 2;

            Rect rangeBorderRect = rect;
            rangeBorderRect.Offset(-1, -1);

            GuidelineSet guidelines = new GuidelineSet();
            guidelines.GuidelinesX.Add(rangeBorderRect.Left + halfPenWidth);
            guidelines.GuidelinesX.Add(rangeBorderRect.Right + halfPenWidth);
            guidelines.GuidelinesY.Add(rangeBorderRect.Top + halfPenWidth);
            guidelines.GuidelinesY.Add(rangeBorderRect.Bottom + halfPenWidth);

            Point p1 = rangeBorderRect.BottomRight;
            p1.Offset(0, +1);
            guidelines.GuidelinesY.Add(p1.Y + halfPenWidth);

            Point p2 = rangeBorderRect.BottomRight;
            p2.Offset(+1, 0);
            guidelines.GuidelinesX.Add(p2.X + halfPenWidth);

            drawingContext.PushGuidelineSet(guidelines);

            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, true, false);
                ctx.LineTo(rangeBorderRect.TopRight, true, false);
                ctx.LineTo(rangeBorderRect.TopLeft, true, false);
                ctx.LineTo(rangeBorderRect.BottomLeft, true, false);
                ctx.LineTo(p2, true, false);
            }
            geometry.Freeze();
            drawingContext.DrawGeometry(null, pen, geometry);

            drawingContext.Pop();
        }
    }
}