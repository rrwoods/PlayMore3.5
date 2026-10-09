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

        private const int COL_GAMETIME_WIDTH = 120;
        private const int COL_REALDATE_WIDTH = 85;
        private const int COL_AMOUNT_WIDTH = 75;
        private const int ROW_HEIGHT = 20;

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

        public TabularLedgerPanel() {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            BorderStyle = BorderStyle.None;

            headerPanel = new Panel {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };

            lblHeaderGameTime = new Label {
                Text = "In-Game Time",
                Location = new Point(0, 2),
                Size = new Size(COL_GAMETIME_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };

            lblHeaderRealDate = new Label {
                Text = "Real Date",
                Location = new Point(COL_GAMETIME_WIDTH + 5, 2),
                Size = new Size(COL_REALDATE_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };

            lblHeaderDesc = new Label {
                Text = "Description",
                Location = new Point(COL_GAMETIME_WIDTH + COL_REALDATE_WIDTH + 10, 2),
                Size = new Size(190, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };

            lblHeaderAmount = new Label {
                Text = "Amount",
                Location = new Point(COL_GAMETIME_WIDTH + COL_REALDATE_WIDTH + 205, 2),
                Size = new Size(COL_AMOUNT_WIDTH, 18),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(Font, FontStyle.Bold)
            };

            headerPanel.Controls.Add(lblHeaderGameTime);
            headerPanel.Controls.Add(lblHeaderRealDate);
            headerPanel.Controls.Add(lblHeaderDesc);
            headerPanel.Controls.Add(lblHeaderAmount);

            bodyPanel = new Panel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };

            Controls.Add(bodyPanel);
            Controls.Add(headerPanel);
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            AdjustColumns();
        }

        private void AdjustColumns() {
            int availableWidth = Width - (SystemInformation.VerticalScrollBarWidth + 10);
            int descWidth = availableWidth - (COL_GAMETIME_WIDTH + COL_REALDATE_WIDTH + COL_AMOUNT_WIDTH + 15);
            if (descWidth < 100) descWidth = 100;

            lblHeaderDesc.Width = descWidth;
            lblHeaderAmount.Left = COL_GAMETIME_WIDTH + COL_REALDATE_WIDTH + 10 + descWidth + 5;

            bodyPanel.SuspendLayout();
            foreach (var row in rows) {
                row.AdjustLayout(descWidth, lblHeaderAmount.Left);
            }
            bodyPanel.ResumeLayout();
        }

        public void SetEntries<T>(
            IEnumerable<T> items,
            Func<T, DateTime> getGameTime,
            Func<T, DateTime> getRealDate,
            Func<T, string> getDescription,
            Func<T, string> getAmountText,
            Action<T> onDelete
        ) {
            bodyPanel.SuspendLayout();
            bodyPanel.Controls.Clear();
            rows.Clear();
            selectedRow = null;

            int currentY = 0;
            int availableWidth = Width - (SystemInformation.VerticalScrollBarWidth + 10);
            int descWidth = availableWidth - (COL_GAMETIME_WIDTH + COL_REALDATE_WIDTH + COL_AMOUNT_WIDTH + 15);
            if (descWidth < 100) descWidth = 100;
            int amountLeft = COL_GAMETIME_WIDTH + COL_REALDATE_WIDTH + 10 + descWidth + 5;

            foreach (var item in items) {
                var row = new LedgerRowControl(
                    LedgerFormatters.FormatGameTime(getGameTime(item)),
                    LedgerFormatters.FormatRealDate(getRealDate(item)),
                    getDescription(item),
                    getAmountText(item)
                ) {
                    Location = new Point(0, currentY),
                    Width = Math.Max(Width, amountLeft + COL_AMOUNT_WIDTH + 5),
                    Height = ROW_HEIGHT,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                row.AdjustLayout(descWidth, amountLeft);

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

            bodyPanel.ResumeLayout();
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
            if (bodyPanel.Controls.Count > 0) {
                var lastControl = bodyPanel.Controls[bodyPanel.Controls.Count - 1];
                bodyPanel.ScrollControlIntoView(lastControl);
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
                Location = new Point(0, 1),
                Size = new Size(120, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            lblRealDate = new Label {
                Text = realDate,
                Location = new Point(125, 1),
                Size = new Size(85, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            lblDescription = new Label {
                Text = description,
                Location = new Point(215, 1),
                Size = new Size(190, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            lblAmount = new Label {
                Text = amount,
                Location = new Point(410, 1),
                Size = new Size(75, 18),
                TextAlign = ContentAlignment.MiddleRight,
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
