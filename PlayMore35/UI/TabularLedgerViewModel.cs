using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.UI {
    public class TabularLedgerRowViewModel {
        public string GameTimeText { get; }
        public string RealDateText { get; }
        public string DescriptionText { get; }
        public string AmountText { get; }
        public decimal NumericAmount { get; }
        public decimal RunningBalance { get; }
        public object? Item { get; }
        public Action? DeleteAction { get; }
        public bool IsSelected { get; internal set; }

        public TabularLedgerRowViewModel(
            string gameTimeText,
            string realDateText,
            string descriptionText,
            string amountText,
            decimal numericAmount = 0m,
            decimal runningBalance = 0m,
            object? item = null,
            Action? deleteAction = null
        ) {
            GameTimeText = gameTimeText ?? "";
            RealDateText = realDateText ?? "";
            DescriptionText = descriptionText ?? "";
            AmountText = amountText ?? "";
            NumericAmount = numericAmount;
            RunningBalance = runningBalance;
            Item = item;
            DeleteAction = deleteAction;
        }

        public void Delete() => DeleteAction?.Invoke();
    }

    public class TabularLedgerViewModel {
        private string _gameTimeHeader = "In-Game Time";
        private string _realDateHeader = "Real Date";
        private string _descriptionHeader = "Description";
        private string _amountHeader = "Amount";
        private readonly List<TabularLedgerRowViewModel> _rows = new List<TabularLedgerRowViewModel>();
        private int _selectedIndex = -1;

        public event EventHandler? EntriesChanged;
        public event EventHandler? SelectionChanged;
        public event EventHandler? ColumnHeadersChanged;

        public string GameTimeHeader {
            get => _gameTimeHeader;
            set {
                if (_gameTimeHeader != value) {
                    _gameTimeHeader = value ?? "";
                    ColumnHeadersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string RealDateHeader {
            get => _realDateHeader;
            set {
                if (_realDateHeader != value) {
                    _realDateHeader = value ?? "";
                    ColumnHeadersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string DescriptionHeader {
            get => _descriptionHeader;
            set {
                if (_descriptionHeader != value) {
                    _descriptionHeader = value ?? "";
                    ColumnHeadersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string AmountHeader {
            get => _amountHeader;
            set {
                if (_amountHeader != value) {
                    _amountHeader = value ?? "";
                    ColumnHeadersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public IReadOnlyList<TabularLedgerRowViewModel> Rows => _rows.AsReadOnly();
        public int RowCount => _rows.Count;

        public int SelectedIndex {
            get => _selectedIndex;
            set {
                if (_selectedIndex != value) {
                    if (_selectedIndex >= 0 && _selectedIndex < _rows.Count) {
                        _rows[_selectedIndex].IsSelected = false;
                    }
                    _selectedIndex = (value >= 0 && value < _rows.Count) ? value : -1;
                    if (_selectedIndex >= 0) {
                        _rows[_selectedIndex].IsSelected = true;
                    }
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public TabularLedgerRowViewModel? SelectedRow =>
            _selectedIndex >= 0 && _selectedIndex < _rows.Count ? _rows[_selectedIndex] : null;

        public decimal TotalAmount => _rows.Count > 0 ? _rows[_rows.Count - 1].RunningBalance : 0m;

        public decimal Total => TotalAmount;

        public string FormattedTotal => string.Equals(_amountHeader, "XP", StringComparison.OrdinalIgnoreCase)
            ? LedgerFormatters.FormatTotalXp((int)TotalAmount)
            : LedgerFormatters.FormatTotalGold(TotalAmount);

        public void SelectRow(int index) {
            SelectedIndex = index;
        }

        public void Clear() {
            _rows.Clear();
            _selectedIndex = -1;
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddRow(TabularLedgerRowViewModel row) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            _rows.Add(row);
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetRows(IEnumerable<TabularLedgerRowViewModel> rows) {
            _rows.Clear();
            _selectedIndex = -1;
            if (rows != null) {
                _rows.AddRange(rows);
            }
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetEntries<T>(
            IEnumerable<T> items,
            Func<T, DateTime> getGameTime,
            Func<T, DateTime> getRealDate,
            Func<T, string> getDescription,
            Func<T, string> getAmountText,
            Action<T>? onDelete = null,
            Func<T, decimal>? getNumericAmount = null
        ) {
            _rows.Clear();
            _selectedIndex = -1;

            decimal runningBalance = 0m;
            if (items != null) {
                foreach (var item in items) {
                    string gameTime = LedgerFormatters.FormatGameTime(getGameTime(item));
                    string realDate = LedgerFormatters.FormatRealDate(getRealDate(item));
                    string desc = getDescription(item);
                    string amountText = getAmountText(item);

                    decimal numericAmount = ResolveNumericAmount(item, amountText, getNumericAmount);
                    runningBalance += numericAmount;

                    var row = new TabularLedgerRowViewModel(
                        gameTime,
                        realDate,
                        desc,
                        amountText,
                        numericAmount,
                        runningBalance,
                        item,
                        () => onDelete?.Invoke(item)
                    );
                    _rows.Add(row);
                }
            }

            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        private static decimal ResolveNumericAmount<T>(T item, string amountText, Func<T, decimal>? customNumericExtractor) {
            if (customNumericExtractor != null) {
                return customNumericExtractor(item);
            }
            if (item is GpTransaction gp) {
                return gp.Amount;
            }
            if (item is XpSessionEntry xp) {
                return xp.Xp;
            }
            if (!string.IsNullOrEmpty(amountText)) {
                string cleaned = amountText.Replace("+", "").Replace(",", "").Trim();
                if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val)) {
                    return val;
                }
            }
            return 0m;
        }
    }
}
