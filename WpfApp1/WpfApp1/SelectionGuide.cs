using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace WpfApp1
{
    class SelectionGuide : Control
    {
        public ScrollViewer? ScrollViewer { get; set; }
        public List<Square> Objects { get; } = new List<Square>();

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (ScrollViewer == null || Objects == null)
            {
                return;
            }

            // ScrollViewerで表示されている領域
            var viewRect = new Rect(ScrollViewer.HorizontalOffset, ScrollViewer.VerticalOffset, ScrollViewer.ViewportWidth, ScrollViewer.ViewportHeight);

            foreach (Square s in Objects)
            {
                var rect = new Rect(Canvas.GetLeft(s), Canvas.GetTop(s), s.Width, s.Height);
                // 四角形が表示領域内に含まれる場合のみ描画する
                if (viewRect.IntersectsWith(rect))
                {
                    drawingContext.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), rect);
                }
            }
        }
    }
}
