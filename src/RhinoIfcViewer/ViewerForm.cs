using System.Drawing.Drawing2D;
using Microsoft.Web.WebView2.WinForms;

namespace RhinoIfcViewer;

public sealed partial class ViewerForm : Form
{
    private readonly RoundedButton _refreshButton;
    private readonly Label _sourceLabel;
    private readonly Label _statusLabel;
    private readonly Label _summaryLabel;
    private readonly Label _staleLabel;
    private readonly TextBox _pathBox;
    private readonly Panel _messagePanel;
    private readonly Label _messageTitle;
    private readonly Label _messageDetail;
    private readonly ToolTip _toolTip = new();
    private readonly Font _bodyFont = new("Segoe UI", 10F);
    private readonly Font _headingFont = new("Segoe UI", 15F, FontStyle.Bold);
    private readonly Font _subtitleFont = new("Segoe UI", 9F);
    private readonly Font _buttonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private readonly Font _messageTitleFont = new("Segoe UI", 12F, FontStyle.Bold);

    public event EventHandler? RefreshRequested;
    public WebView2 ViewerBrowser { get; private set; }

    public ViewerForm()
    {
        Text = "Rhino IFC Viewer";
        Font = _bodyFont;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(960, 700);
        MinimumSize = new Size(680, 500);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = ColorTranslator.FromHtml("#F5F6F8");
        ForeColor = ColorTranslator.FromHtml("#202124");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        // Header: Title + Subtitle on the left, refined Export & Refresh action on the right
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Color.White
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titlePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var heading = new Label
        {
            Text = "IFC Preview",
            AutoSize = true,
            Font = _headingFont,
            ForeColor = ColorTranslator.FromHtml("#202124"),
            Margin = new Padding(0, 0, 0, 2)
        };
        var subtitle = new Label
        {
            Text = "Review the exported snapshot of your Rhino model.",
            AutoSize = true,
            Font = _subtitleFont,
            ForeColor = ColorTranslator.FromHtml("#616975"),
            Margin = Padding.Empty
        };
        titlePanel.Controls.Add(heading);
        titlePanel.Controls.Add(subtitle);

        _refreshButton = new RoundedButton
        {
            Text = "Export & Refresh",
            UseMnemonic = false,
            AccessibleName = "Export and refresh",
            AutoSize = true,
            MinimumSize = new Size(160, 38),
            BackColor = ColorTranslator.FromHtml("#2563EB"),
            ForeColor = Color.White,
            Font = _buttonFont,
            Cursor = Cursors.Hand,
            Padding = new Padding(14, 6, 14, 6),
            Anchor = AnchorStyles.Right,
            Radius = 8,
            TabIndex = 0
        };
        _refreshButton.Click += OnRefreshClicked;

        header.Controls.Add(titlePanel, 0, 0);
        header.Controls.Add(_refreshButton, 1, 0);
        layout.Controls.Add(header, 0, 0);

        _sourceLabel = new Label
        {
            Text = "Source: No snapshot",
            AutoSize = false,
            AutoEllipsis = true,
            Height = 36,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 8),
            ForeColor = ColorTranslator.FromHtml("#202124"),
            AccessibleName = "Snapshot source"
        };
        layout.Controls.Add(_sourceLabel, 0, 1);

        var preview = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        ViewerBrowser = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false,
            AccessibleName = "IFC model preview",
            TabIndex = 1
        };
        _messagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32),
            BackColor = ColorTranslator.FromHtml("#EBEEF2")
        };
        _messageTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = _messageTitleFont,
            ForeColor = ColorTranslator.FromHtml("#202124")
        };
        _messageDetail = new Label
        {
            Dock = DockStyle.Fill,
            Font = _bodyFont,
            ForeColor = ColorTranslator.FromHtml("#616975")
        };
        _messagePanel.Controls.Add(_messageDetail);
        _messagePanel.Controls.Add(_messageTitle);
        preview.Controls.Add(ViewerBrowser);
        preview.Controls.Add(_messagePanel);
        layout.Controls.Add(preview, 0, 2);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Color.White
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _statusLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            AccessibleName = "Preview status"
        };
        _summaryLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = ColorTranslator.FromHtml("#616975"),
            Margin = new Padding(0, 0, 0, 4),
            Visible = false,
            AccessibleName = "Export summary"
        };
        _staleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Visible = false,
            Margin = new Padding(0, 0, 0, 4),
            Text = "Previous snapshot (stale)",
            ForeColor = ColorTranslator.FromHtml("#8A5A00"),
            AccessibleName = "Stale snapshot indicator"
        };
        _pathBox = new TextBox
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            AccessibleName = "Exported IFC file path",
            PlaceholderText = "No IFC file exported",
            BackColor = ColorTranslator.FromHtml("#F5F6F8"),
            ForeColor = ColorTranslator.FromHtml("#202124"),
            BorderStyle = BorderStyle.FixedSingle,
            TabIndex = 2
        };
        footer.Controls.Add(_statusLabel);
        footer.Controls.Add(_summaryLabel);
        footer.Controls.Add(_staleLabel);
        footer.Controls.Add(_pathBox);
        footer.SizeChanged += (_, _) => UpdateFooterLabelConstraints(footer.ClientSize.Width - footer.Padding.Horizontal);
        layout.Controls.Add(footer, 0, 3);

        InitializeIntegration();
    }

    private void UpdateFooterLabelConstraints(int availableWidth)
    {
        int contentWidth = Math.Max(100, availableWidth);
        _statusLabel.MaximumSize = new Size(contentWidth, 0);
        _summaryLabel.MaximumSize = new Size(contentWidth, 0);
        _staleLabel.MaximumSize = new Size(contentWidth, 0);
    }

    private void OnRefreshClicked(object? sender, EventArgs e) =>
        RefreshRequested?.Invoke(this, EventArgs.Empty);

    // Presentation contracts are called on Rhino's UI thread.
    public void SetBusy(bool isBusy)
    {
        _refreshButton.Enabled = !isBusy;
        _refreshButton.Text = isBusy ? "Working..." : "Export & Refresh";
        _refreshButton.Cursor = isBusy ? Cursors.WaitCursor : Cursors.Hand;
    }

    public void SetSourceDocument(string documentName)
    {
        _sourceLabel.Text = $"Source: {documentName}";
        _toolTip.SetToolTip(_sourceLabel, documentName);
    }

    public void SetStatus(string message, UiStatusKind kind)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = ColorTranslator.FromHtml(kind switch
        {
            UiStatusKind.Success => "#237A45",
            UiStatusKind.Warning => "#8A5A00",
            UiStatusKind.Error => "#B42318",
            UiStatusKind.Busy => "#2563EB",
            _ => "#616975"
        });
    }

    public void SetExportSummary(int exportedCount, int skippedCount)
    {
        _summaryLabel.Text = $"Objects: {exportedCount} exported · {skippedCount} skipped";
        _summaryLabel.Visible = true;
    }

    public void ClearExportSummary()
    {
        _summaryLabel.Text = string.Empty;
        _summaryLabel.Visible = false;
    }

    public void SetExportPath(string? filePath)
    {
        _pathBox.Text = filePath ?? string.Empty;
        _pathBox.AccessibleDescription = string.IsNullOrEmpty(filePath)
            ? "No IFC file exported" : filePath;
    }

    public void SetSnapshotStale(bool isStale) => _staleLabel.Visible = isStale;

    public void SetPreviewMessage(string? title, string? detail)
    {
        bool showMessage = title is not null;
        _messageTitle.Text = title ?? string.Empty;
        _messageDetail.Text = detail ?? string.Empty;
        _messagePanel.Visible = showMessage;
        ViewerBrowser.Visible = !showMessage;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeIntegration();
            _refreshButton.Click -= OnRefreshClicked;
            _toolTip.Dispose();
        }
        // Disposes the single browser control along with all other child controls.
        base.Dispose(disposing);
        if (disposing)
        {
            _headingFont.Dispose();
            _bodyFont.Dispose();
            _subtitleFont.Dispose();
            _buttonFont.Dispose();
            _messageTitleFont.Dispose();
        }
    }

    private sealed class RoundedButton : Button
    {
        private int _radius = 8;
        private bool _isHovered;
        private bool _isPressed;

        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Invalidate();
            }
        }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2F;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            Color parentBg = Parent?.BackColor ?? Color.White;
            using (var bgBrush = new SolidBrush(parentBg))
            {
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);
            }

            // Inset by 1px so anti-aliasing fringe stays within the control bounds without clipping
            var rect = new RectangleF(1F, 1F, Width - 2F, Height - 2F);
            using var path = CreateRoundPath(rect, _radius);

            Color bg;
            if (!Enabled)
                bg = ColorTranslator.FromHtml("#93C5FD");
            else if (_isPressed)
                bg = ColorTranslator.FromHtml("#1E40AF");
            else if (_isHovered)
                bg = ColorTranslator.FromHtml("#1D4ED8");
            else
                bg = BackColor;

            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillPath(brush, path);
            }

            Color border = Enabled
                ? ColorTranslator.FromHtml("#1D4ED8")
                : ColorTranslator.FromHtml("#93C5FD");

            using (var pen = new Pen(border, 1.2F))
            {
                e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
