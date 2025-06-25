using System.Drawing;
using System.Windows.Forms;

namespace DencryptGUI
{
    public static class Style
    {
        public static void ApplyDarkTheme(Control control)
        {
            control.BackColor = Color.FromArgb(30, 30, 30);
            control.ForeColor = Color.White;
            control.Font = new Font("Segoe UI", 15);

            foreach (Control child in control.Controls)
            {
                ApplyControlStyle(child);
            }
        }

        private static void ApplyControlStyle(Control control)
        {
            if (control is Button btn)
            {
                btn.BackColor = Color.FromArgb(40, 40, 40);
                btn.Height = 30;
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Cursor = Cursors.Hand;
                btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            }
            else if (control is TextBox txt)
            {
                txt.BackColor = Color.FromArgb(40, 40, 40);
                txt.ForeColor = Color.White;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ListBox list)
            {
                list.BackColor = Color.FromArgb(35, 35, 35);
                list.ForeColor = Color.White;
                list.BorderStyle = BorderStyle.None;
                list.Font = new Font("Segoe UI", 14);
            }
            else if (control is ListView view)
            {
                view.BackColor = Color.FromArgb(35, 35, 35);
                view.ForeColor = Color.White;
                view.BorderStyle = BorderStyle.None;
                view.Font = new Font("Segoe UI", 10);
            }
            else if (control is ProgressBar progressBar)
            {
                progressBar.BackColor = Color.FromArgb(200, 30, 30);
                progressBar.ForeColor = Color.FromArgb(30, 200, 30);
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Height = 40;
                progressBar.Width = 640;
                progressBar.Margin = new Padding(5);
            }

            // Recursively apply to nested controls
            foreach (Control child in control.Controls)
            {
                ApplyControlStyle(child);
            }
        }
        public static void ApplyListViewStyle(ListView listView)
        {
            listView.OwnerDraw = true;
            listView.FullRowSelect = true;
            listView.BorderStyle = BorderStyle.None;
            listView.Font = new Font("Segoe UI", 9);
            listView.ForeColor = Color.White;
            listView.BackColor = Color.FromArgb(20, 20, 20);

            listView.DrawColumnHeader += (s, e) =>
            {
                using Brush headerBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                using Pen borderPen = new Pen(Color.FromArgb(60, 60, 60));
                e.Graphics.FillRectangle(headerBrush, e.Bounds);
                e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.White, TextFormatFlags.Left);
            };

            listView.DrawSubItem += (s, e) =>
            {
                bool isSelected = e.Item.Selected;

                // Background color
                Color bgColor = isSelected
                    ? Color.FromArgb(60, 60, 100) // selection highlight
                    : listView.BackColor;

                using SolidBrush bgBrush = new SolidBrush(bgColor);
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                // Text
                TextRenderer.DrawText(
                    e.Graphics,
                    e.SubItem.Text,
                    listView.Font,
                    e.Bounds,
                    e.Item.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            };
        }
    }
}

public class CustomProgressBar : ProgressBar
{
    public Color BarColor { get; set; } = Color.LimeGreen;
    public Color BackgroundColor { get; set; } = Color.FromArgb(40, 40, 40);

    public CustomProgressBar()
    {
        SetStyle(ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Rectangle rect = this.ClientRectangle;
        Graphics g = e.Graphics;

        using Brush bgBrush = new SolidBrush(BackgroundColor);
        g.FillRectangle(bgBrush, rect);

        float percent = (float)Value / Maximum;
        Rectangle fill = new Rectangle(rect.X, rect.Y, (int)(rect.Width * percent), rect.Height);

        using Brush barBrush = new SolidBrush(BarColor);
        g.FillRectangle(barBrush, fill);
    }
}