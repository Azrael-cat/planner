using System;
using System.Drawing;
using System.Windows.Forms;
namespace DeskTasks  {
    // A drag never counts as a click, so moving the shortcut does not open the card.
    public class FloatingTaskButton : ModernButton  {
        private Point pointerStart;
        private Point windowStart;
        private bool pressed;
        private bool dragged;
        public event Action<Point> PositionChanged;
        public static Point Clamp(Point position, Size size)  {
            Rectangle area = Screen.FromPoint(position).WorkingArea;
            return new Point( Math.Max(area.Left, Math.Min(position.X, area.Right - size.Width)), Math.Max(area.Top, Math.Min(position.Y, area.Bottom - size.Height)));
        }
        protected override void OnMouseDown(MouseEventArgs e)  {
            if (e.Button == MouseButtons.Left && FindForm() != null)  {
                pressed = true;
                dragged = false;
                pointerStart = PointToScreen(e.Location);
                windowStart = FindForm().Location;
                Capture = true;
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)  {
            if (pressed && e.Button == MouseButtons.Left)  {
                Point pointer = PointToScreen(e.Location);
                Size threshold = SystemInformation.DragSize;
                if (Math.Abs(pointer.X - pointerStart.X) >= threshold.Width / 2 || Math.Abs(pointer.Y - pointerStart.Y) >= threshold.Height / 2)  {
                    dragged = true;
                }
                if (dragged)  {
                    Form window = FindForm();
                    window.Location = Clamp(new Point(windowStart.X + pointer.X - pointerStart.X, windowStart.Y + pointer.Y - pointerStart.Y), window.Size);
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)  {
            bool save = pressed && dragged && e.Button == MouseButtons.Left;
            pressed = false;
            base.OnMouseUp(e);
            Capture = false;
            if (save && PositionChanged != null) PositionChanged(FindForm().Location);
        }
        protected override void OnClick(EventArgs e)  {
            if (!dragged) base.OnClick(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)  {
            dragged = false;
            base.OnKeyDown(e);
        }
    }
}
