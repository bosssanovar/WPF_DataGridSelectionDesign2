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

        Rect rect;
        public Rect Rect
        {
            get { return rect; }
            set
            {
                rect = value;

                Visibility = Visibility.Visible;

                InvalidateVisual();
            }
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
