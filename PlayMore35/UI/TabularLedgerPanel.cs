using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlayMore35.UI {
    public class TabularLedgerPanel : UserControl {
        private readonly Panel headerPanel;
        private readonly Panel bodyPanel;
        private readonly Label lblHeaderGameTime;
        private readonly Label lblHeaderRealDate;
        private readonly Label lblHeaderDesc;
        private readonly Label lblHeaderAmount;
        private readonly List<LedgerRowControl> rows = new List<LedgerRowControl>();
        private LedgerRowControl selectedRow;

        public const int COL_GAMETIME_LEFT = 0;
        public const int COL_GAMETIME_WIDTH = 115;
        public const int COL_REALDATE_LEFT = COL_GAMETIME_LEFT + COL_GAMETIME_WIDTH + 5;
        public const int COL_REALDATE_WIDTH = 110;
        public const int COL_DESC_LEFT = COL_REALDATE_LEFT + COL_REALDATE_WIDTH + 5;
        public const int COL_DESC_WIDTH = 300;
        public const int COL_AMOUNT_WIDTH = 78;
        public const int BASELINE_AMOUNT_LEFT = COL_DESC_LEFT + COL_DESC_WIDTH + 6;
        public const int BASELINE_WIDTH = BASELINE_AMOUNT_LEFT + COL_AMOUNT_WIDTH + 31;
        public const int HEADER_HEIGHT = 22;
        public const int ROW_HEIGHT = 20;

        public int HeaderTop => headerPanel.Top;
        public int BodyTop => bodyPanel.Top;
        public bool IsOverflow => (HEADER_HEIGHT + rows.Count * ROW_HEIGHT) > ClientSize.Height;
        public int VerticalScrollValue => bodyPanel.VerticalScroll.Value;

        public string AmountColumnHeader {
            get => lblHeaderAmount.Text;
            set => lblHeaderAmount.Text = value;
        }

        public int RowCount => rows.Count;

        public LedgerRowControl GetRow(int index) {
            if (index >= 0 && index < rows.Count) {
                return rows[index];
            }
            return null;
        }

        public void AlignInputControls(Control datePicker, Control descInput, Control amountInput, Control addButton) {
            Width = BASELINE_WIDTH;
            Left = addButton.Right - BASELINE_WIDTH;

            datePicker.Left = Left + COL_REALDATE_LEFT;

            descInput.Left = Left + COL_DESC_LEFT;
            descInput.Width = COL_DESC_WIDTH;

            amountInput.Left = Left + BASELINE_AMOUNT_LEFT;
            amountInput.Width = COL_AMOUNT_WIDTH;
        }

        public TabularLedgerPanel() {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.Transparent;
            BorderStyle = BorderStyle.None;

            headerPanel = new Panel {
                Height = HEADER_HEIGHT,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };

            lblHeaderGameTime = new Label {
                Text = "In-Game Time",
                Location = new Point(COL_GAMETIME_LEFT, 2),
                Size = new Size(COL_GAMETIME_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };

            lblHeaderRealDate = new Label {
                Text = "Real Date",
                Location = new Point(COL_REALDATE_LEFT, 2),
                Size = new Size(COL_REALDATE_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };

            lblHeaderDesc = new Label {
                Text = "Description",
                Location = new Point(COL_DESC_LEFT, 2),
                Size = new Size(COL_DESC_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };

            lblHeaderAmount = new Label {
                Text = "Amount",
                Location = new Point(BASELINE_AMOUNT_LEFT, 2),
                Size = new Size(COL_AMOUNT_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font, FontStyle.Bold)
            };

            headerPanel.Controls.Add(lblHeaderGameTime);
            headerPanel.Controls.Add(lblHeaderRealDate);
            headerPanel.Controls.Add(lblHeaderDesc);
            headerPanel.Controls.Add(lblHeaderAmount);

            bodyPanel = new Panel {
                AutoScroll = false,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };

            Controls.Add(bodyPanel);
            Controls.Add(headerPanel);

            UpdateLayout();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            UpdateLayout();
            if (IsOverflow) {
                ScrollToBottom();
            }
        }

        public void UpdateLayout() {
            int contentHeight = HEADER_HEIGHT + rows.Count * ROW_HEIGHT;
            int totalHeight = Math.Max(0, ClientSize.Height);
            int totalWidth = Math.Max(0, ClientSize.Width);
            bool isOverflow = contentHeight > totalHeight;

            if (!isOverflow && bodyPanel.AutoScroll) {
                bodyPanel.AutoScrollPosition = new Point(0, 0);
                bodyPanel.AutoScroll = false;
            }

            headerPanel.SuspendLayout();
            bodyPanel.SuspendLayout();
            SuspendLayout();

            if (isOverflow) {
                headerPanel.Location = new Point(0, 0);
                headerPanel.Size = new Size(totalWidth, HEADER_HEIGHT);

                bodyPanel.Location = new Point(0, HEADER_HEIGHT);
                bodyPanel.Size = new Size(totalWidth, Math.Max(0, totalHeight - HEADER_HEIGHT));
                bodyPanel.AutoScroll = true;
            } else {
                int headerTop = Math.Max(0, totalHeight - contentHeight);
                headerPanel.Location = new Point(0, headerTop);
                headerPanel.Size = new Size(totalWidth, HEADER_HEIGHT);

                int bodyTop = headerTop + HEADER_HEIGHT;
                bodyPanel.Location = new Point(0, bodyTop);
                bodyPanel.Size = new Size(totalWidth, Math.Max(0, rows.Count * ROW_HEIGHT));
                bodyPanel.AutoScroll = false;
            }

            AdjustColumns(isOverflow);

            headerPanel.ResumeLayout(true);
            bodyPanel.ResumeLayout(true);

            int scrollY = isOverflow ? bodyPanel.DisplayRectangle.Y : 0;
            for (int i = 0; i < rows.Count; i++) {
                rows[i].Location = new Point(0, i * ROW_HEIGHT + scrollY);
            }

            ResumeLayout(true);

            headerPanel.Invalidate(true);
            bodyPanel.Invalidate(true);
            Invalidate(true);
        }

        private void AdjustColumns(bool isOverflow) {
            int totalWidth = ClientSize.Width;
            int extraWidth = Math.Max(0, totalWidth - BASELINE_WIDTH);

            int amountLeft = BASELINE_AMOUNT_LEFT + extraWidth;
            if (isOverflow) {
                int scrollBarReserved = SystemInformation.VerticalScrollBarWidth + 10;
                int maxAmountLeft = totalWidth - scrollBarReserved - COL_AMOUNT_WIDTH;
                if (amountLeft > maxAmountLeft) {
                    amountLeft = Math.Max(COL_DESC_LEFT + COL_DESC_WIDTH + 6, maxAmountLeft);
                }
            }

            int descWidth = Math.Max(COL_DESC_WIDTH, amountLeft - COL_DESC_LEFT - 6);

            lblHeaderDesc.Width = descWidth;
            lblHeaderAmount.Left = amountLeft;
            lblHeaderAmount.Width = COL_AMOUNT_WIDTH;

            int rowWidth = isOverflow
                ? Math.Max(0, totalWidth - SystemInformation.VerticalScrollBarWidth)
                : totalWidth;
            foreach (var row in rows) {
                row.Width = rowWidth;
                row.AdjustLayout(descWidth, amountLeft);
            }
        }

        public void SetEntries<T>(
            IEnumerable<T> items,
            Func<T, DateTime> getGameTime,
            Func<T, DateTime> getRealDate,
            Func<T, string> getDescription,
            Func<T, string> getAmountText,
            Action<T> onDelete
        ) {
            if (bodyPanel.AutoScroll) {
                bodyPanel.AutoScrollPosition = new Point(0, 0);
                bodyPanel.AutoScroll = false;
            }

            bodyPanel.SuspendLayout();
            bodyPanel.Controls.Clear();
            rows.Clear();
            selectedRow = null;

            int currentY = 0;
            foreach (var item in items) {
                var row = new LedgerRowControl(
                    LedgerFormatters.FormatGameTime(getGameTime(item)),
                    LedgerFormatters.FormatRealDate(getRealDate(item)),
                    getDescription(item),
                    getAmountText(item)
                ) {
                    Location = new Point(0, currentY),
                    Height = ROW_HEIGHT
                };

                string desc = getDescription(item);
                var contextMenu = new ContextMenuStrip();
                var deleteItem = new ToolStripMenuItem($"Delete {desc}");
                deleteItem.Click += (s, ev) => onDelete?.Invoke(item);
                contextMenu.Items.Add(deleteItem);
                row.ContextMenuStrip = contextMenu;

                row.RowSelected += (s, ev) => SelectRowInternal(row);

                rows.Add(row);
                bodyPanel.Controls.Add(row);
                currentY += ROW_HEIGHT;
            }

            bodyPanel.ResumeLayout(true);
            UpdateLayout();
            ScrollToBottom();
        }

        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            if (Visible) {
                UpdateLayout();
                ScrollToBottom();
            }
        }

        public void SelectRow(int index) {
            if (index >= 0 && index < rows.Count) {
                SelectRowInternal(rows[index]);
            }
        }

        private void SelectRowInternal(LedgerRowControl row) {
            if (selectedRow != null && selectedRow != row) {
                selectedRow.IsSelected = false;
            }
            selectedRow = row;
            if (selectedRow != null) {
                selectedRow.IsSelected = true;
            }
        }

        public void ScrollToBottom() {
            if (!IsOverflow || !bodyPanel.AutoScroll || bodyPanel.Controls.Count == 0) {
                return;
            }
            var lastControl = bodyPanel.Controls[bodyPanel.Controls.Count - 1];
            bodyPanel.ScrollControlIntoView(lastControl);
            try {
                bodyPanel.AutoScrollPosition = new Point(0, Math.Max(0, bodyPanel.DisplayRectangle.Height));
            } catch { }
            int scrollY = bodyPanel.DisplayRectangle.Y;
            for (int i = 0; i < rows.Count; i++) {
                rows[i].Location = new Point(0, i * ROW_HEIGHT + scrollY);
            }
        }
    }

    public class LedgerRowControl : Panel {
        private readonly Label lblGameTime;
        private readonly Label lblRealDate;
        private readonly Label lblDescription;
        private readonly Label lblAmount;
        private bool isSelected = false;
        private bool isHovered = false;

        private static readonly Color HoverColor = Color.FromArgb(240, 244, 250);
        private static readonly Color SelectedColor = Color.FromArgb(228, 238, 248);
        private static readonly Color NormalColor = Color.Transparent;

        public event EventHandler RowSelected;

        public string GameTimeText => lblGameTime.Text;
        public string RealDateText => lblRealDate.Text;
        public string DescriptionText => lblDescription.Text;
        public string AmountText => lblAmount.Text;

        public bool IsSelected {
            get => isSelected;
            set {
                isSelected = value;
                UpdateAppearance();
            }
        }

        public LedgerRowControl(string gameTime, string realDate, string description, string amount) {
            DoubleBuffered = true;
            BackColor = NormalColor;
            BorderStyle = BorderStyle.None;

            lblGameTime = new Label {
                Text = gameTime,
                Location = new Point(TabularLedgerPanel.COL_GAMETIME_LEFT, 1),
                Size = new Size(TabularLedgerPanel.COL_GAMETIME_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            lblRealDate = new Label {
                Text = realDate,
                Location = new Point(TabularLedgerPanel.COL_REALDATE_LEFT, 1),
                Size = new Size(TabularLedgerPanel.COL_REALDATE_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            lblDescription = new Label {
                Text = description,
                Location = new Point(TabularLedgerPanel.COL_DESC_LEFT, 1),
                Size = new Size(TabularLedgerPanel.COL_DESC_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            lblAmount = new Label {
                Text = amount,
                Location = new Point(TabularLedgerPanel.BASELINE_AMOUNT_LEFT, 1),
                Size = new Size(TabularLedgerPanel.COL_AMOUNT_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            Controls.Add(lblGameTime);
            Controls.Add(lblRealDate);
            Controls.Add(lblDescription);
            Controls.Add(lblAmount);

            WireEvents(this);
            WireEvents(lblGameTime);
            WireEvents(lblRealDate);
            WireEvents(lblDescription);
            WireEvents(lblAmount);
        }

        public void AdjustLayout(int descWidth, int amountLeft) {
            lblDescription.Width = descWidth;
            lblAmount.Left = amountLeft;
        }

        private void WireEvents(Control ctrl) {
            ctrl.MouseEnter += (s, e) => {
                isHovered = true;
                UpdateAppearance();
            };
            ctrl.MouseLeave += (s, e) => {
                Point p = PointToClient(Cursor.Position);
                if (!ClientRectangle.Contains(p)) {
                    isHovered = false;
                    UpdateAppearance();
                }
            };
            ctrl.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    RowSelected?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        protected override void OnContextMenuStripChanged(EventArgs e) {
            base.OnContextMenuStripChanged(e);
            lblGameTime.ContextMenuStrip = ContextMenuStrip;
            lblRealDate.ContextMenuStrip = ContextMenuStrip;
            lblDescription.ContextMenuStrip = ContextMenuStrip;
            lblAmount.ContextMenuStrip = ContextMenuStrip;
        }

        private void UpdateAppearance() {
            if (isSelected) {
                BackColor = SelectedColor;
            } else if (isHovered) {
                BackColor = HoverColor;
            } else {
                BackColor = NormalColor;
            }
        }
    }
}
