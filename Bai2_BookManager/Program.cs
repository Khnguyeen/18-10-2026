
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Bai2_BookManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmBookManager());
        }
    }

    public class BookModel
    {
        public string BookId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string CoverPath { get; set; } = "";
        public decimal TotalValue => Price * Quantity;
    }

    public class frmBookManager : Form
    {
        readonly BindingList<BookModel> books = new();
        readonly BindingSource binding = new();

        readonly TextBox id = new();
        readonly TextBox title = new();
        readonly TextBox author = new();
        readonly TextBox price = new();
        readonly TextBox quantity = new();
        readonly TextBox coverPath = new();
        readonly TextBox search = new();

        readonly PictureBox picBookCover = new();
        readonly DataGridView dgvBooks = new();

        readonly ToolStripStatusLabel countStatus = new();
        readonly ToolStripStatusLabel valueStatus = new();
        readonly ToolStripStatusLabel timeStatus = new();

        BookModel? selected;
        BindingList<BookModel>? filteredBooks;
        int nextId = 1;

        public frmBookManager()
        {
            Text = "QUAN LY SACH | NHA SACH";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 780);
            MinimumSize = new Size(1000, 650);
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 250);

            BuildUI();

            binding.DataSource = books;
            dgvBooks.DataSource = binding;

            books.ListChanged += (_, _) => UpdateStatus();

            UpdateStatus();
            Reset();
        }

        void BuildUI()
        {
            var menu = new MenuStrip
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var file = new ToolStripMenuItem("Tep");
            file.DropDownItems.Add("Mo file CSV", null, (_, _) => ImportCsv());
            file.DropDownItems.Add("Luu file CSV", null, (_, _) => ExportCsv());
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add("Thoat", null, (_, _) => Close());

            var manage = new ToolStripMenuItem("Quan ly");
            manage.DropDownItems.Add("Them sach", null, (_, _) => AddBook());
            manage.DropDownItems.Add("Cap nhat sach", null, (_, _) => EditBook());
            manage.DropDownItems.Add("Xoa sach", null, (_, _) => DeleteBook());

            menu.Items.AddRange(new ToolStripItem[] { file, manage });
            MainMenuStrip = menu;

            var tools = new ToolStrip
            {
                Dock = DockStyle.Fill,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(235, 241, 248),
                Padding = new Padding(6, 3, 6, 3)
            };

            tools.Items.Add("Mo CSV", null, (_, _) => ImportCsv());
            tools.Items.Add("Xuat CSV", null, (_, _) => ExportCsv());
            tools.Items.Add(new ToolStripSeparator());
            tools.Items.Add("Them sach", null, (_, _) => AddBook());
            tools.Items.Add("Cap nhat", null, (_, _) => EditBook());
            tools.Items.Add("Xoa sach", null, (_, _) => DeleteBook());

            var status = new StatusStrip
            {
                Dock = DockStyle.Fill,
                SizingGrip = false,
                BackColor = Color.White
            };

            status.Items.Add(countStatus);
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(valueStatus);
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(timeStatus);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            Controls.Add(root);
            root.Controls.Add(menu, 0, 0);
            root.Controls.Add(tools, 0, 1);
            root.Controls.Add(status, 0, 3);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(220, 226, 234)
            };

            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;

            root.Controls.Add(split, 0, 2);

            split.HandleCreated += (_, _) =>
            {
                int maxDistance = split.ClientSize.Width - split.SplitterWidth;

                if (maxDistance > 0)
                    split.SplitterDistance = Math.Min(370, maxDistance);
            };

            split.SizeChanged += (_, _) =>
            {
                if (!split.IsHandleCreated || split.ClientSize.Width <= split.SplitterWidth)
                    return;

                int maxDistance = split.ClientSize.Width - split.SplitterWidth;
                int distance = Math.Min(370, maxDistance);

                if (split.SplitterDistance != distance)
                    split.SplitterDistance = distance;
            };

            root.Controls.Add(split, 0, 2);

            split.HandleCreated += (_, _) =>
            {
                SetSplitterDistance(split, 370);
            };

            split.SizeChanged += (_, _) =>
            {
                if (split.IsHandleCreated)
                    SetSplitterDistance(split, 370);
            };

            var leftCard = new GroupBox
            {
                Text = "Thong tin sach",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            split.Panel1.Controls.Add(leftCard);

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                Padding = new Padding(4),
                BackColor = Color.White
            };

            left.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 6; i++)
                left.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            leftCard.Controls.Add(left);

            AddField(left, "Ma sach", id, 0);
            AddField(left, "Ten sach", title, 1);
            AddField(left, "Tac gia", author, 2);
            AddField(left, "Don gia", price, 3);
            AddField(left, "So luong", quantity, 4);
            AddField(left, "Duong dan anh", coverPath, 5);

            id.ReadOnly = true;
            id.BackColor = Color.FromArgb(242, 245, 249);

            foreach (var box in new[]
            {
                id, title, author, price, quantity, coverPath
            })
            {
                box.Margin = new Padding(4, 5, 4, 5);
            }

            var coverButton = new Button
            {
                Text = "Chon anh bia...",
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Color.FromArgb(231, 239, 249),
                FlatStyle = FlatStyle.Flat
            };

            coverButton.Click += (_, _) => LoadCover();
            left.Controls.Add(coverButton, 1, 6);

            picBookCover.Dock = DockStyle.Fill;
            picBookCover.SizeMode = PictureBoxSizeMode.Zoom;
            picBookCover.BackColor = Color.FromArgb(248, 250, 252);
            picBookCover.Margin = new Padding(4, 2, 4, 6);
            picBookCover.BorderStyle = BorderStyle.FixedSingle;

            left.Controls.Add(picBookCover, 0, 7);
            left.SetColumnSpan(picBookCover, 2);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty
            };

            for (int i = 0; i < 4; i++)
                actions.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 25));

            string[] captions =
            {
                "Them", "Sua", "Xoa", "Lam moi"
            };

            EventHandler[] handlers =
            {
                (_, _) => AddBook(),
                (_, _) => EditBook(),
                (_, _) => DeleteBook(),
                (_, _) => Reset()
            };

            Color[] colors =
            {
                Color.SeaGreen,
                Color.RoyalBlue,
                Color.IndianRed,
                Color.DimGray
            };

            for (int i = 0; i < captions.Length; i++)
            {
                var button = new Button
                {
                    Text = captions[i],
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    BackColor = colors[i],
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };

                button.FlatAppearance.BorderSize = 0;
                button.Click += handlers[i];
                actions.Controls.Add(button, i, 0);
            }

            left.Controls.Add(actions, 0, 8);
            left.SetColumnSpan(actions, 2);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8, 0, 0, 0)
            };

            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            split.Panel2.Controls.Add(right);

            var searchBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 5, 0, 5)
            };

            searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            search.PlaceholderText = "Tim theo ten sach...";
            search.Dock = DockStyle.Fill;
            search.Margin = new Padding(0, 2, 8, 2);

            search.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SearchBook();
                    e.SuppressKeyPress = true;
                }
            };

            var btnSearch = CreateButton("Tim kiem", Color.RoyalBlue);
            var btnAll = CreateButton("Hien tat ca", Color.DimGray);

            btnSearch.Click += (_, _) => SearchBook();
            btnAll.Click += (_, _) =>
            {
                search.Clear();
                filteredBooks = null;
                binding.DataSource = books;
                dgvBooks.DataSource = binding;
            };

            searchBar.Controls.Add(search, 0, 0);
            searchBar.Controls.Add(btnSearch, 1, 0);
            searchBar.Controls.Add(btnAll, 2, 0);

            right.Controls.Add(searchBar, 0, 0);

            dgvBooks.Dock = DockStyle.Fill;
            dgvBooks.ReadOnly = true;
            dgvBooks.AllowUserToAddRows = false;
            dgvBooks.AllowUserToDeleteRows = false;
            dgvBooks.MultiSelect = false;
            dgvBooks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBooks.AutoGenerateColumns = false;
            dgvBooks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBooks.RowHeadersVisible = false;
            dgvBooks.BackgroundColor = Color.White;
            dgvBooks.BorderStyle = BorderStyle.FixedSingle;
            dgvBooks.RowTemplate.Height = 32;
            dgvBooks.ColumnHeadersHeight = 38;
            dgvBooks.EnableHeadersVisualStyles = false;
            dgvBooks.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(222, 233, 247);
            dgvBooks.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);
            dgvBooks.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(246, 249, 253);

            AddColumn("Ma sach", nameof(BookModel.BookId), 15);
            AddColumn("Ten sach", nameof(BookModel.Title), 25);
            AddColumn("Tac gia", nameof(BookModel.Author), 20);
            AddColumn("Don gia", nameof(BookModel.Price), 15, "N0");
            AddColumn("So luong", nameof(BookModel.Quantity), 10);
            AddColumn("Thanh tien", nameof(BookModel.TotalValue), 15, "N0");

            dgvBooks.SelectionChanged += (_, _) => LoadSelected();

            right.Controls.Add(dgvBooks, 0, 1);
        }

        static void SetSplitterDistance(SplitContainer split, int desired)
        {
            int max = split.ClientSize.Width
                - split.Panel2MinSize
                - split.SplitterWidth;

            int min = split.Panel1MinSize;

            if (max < min)
                return;

            int distance = Math.Max(min, Math.Min(desired, max));

            if (split.SplitterDistance != distance)
                split.SplitterDistance = distance;
        }

        Button CreateButton(string text, Color color)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        void AddColumn(
            string header,
            string property,
            float width,
            string format = "")
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = width,
                MinimumWidth = 60
            };

            if (format != "")
                column.DefaultCellStyle.Format = format;

            dgvBooks.Columns.Add(column);
        }

        static void AddField(
            TableLayoutPanel panel,
            string label,
            Control control,
            int row)
        {
            panel.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            }, 0, row);

            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control, 1, row);
        }

        bool ValidateBook(out decimal p, out int q)
        {
            bool valid =
                !string.IsNullOrWhiteSpace(title.Text) &&
                decimal.TryParse(price.Text, out p) &&
                p > 0 &&
                int.TryParse(quantity.Text, out q) &&
                q >= 0;

            if (!decimal.TryParse(price.Text, out p))
                p = 0;

            if (!int.TryParse(quantity.Text, out q))
                q = 0;

            if (!valid)
            {
                MessageBox.Show(
                    "Nhap ten sach, don gia > 0 va so luong la so nguyen khong am.",
                    "Du lieu chua hop le",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return valid;
        }

        void AddBook()
        {
            if (!ValidateBook(out decimal p, out int q))
                return;

            try
            {
                var book = new BookModel
                {
                    BookId = "S" + nextId.ToString("D3"),
                    Title = title.Text.Trim(),
                    Author = author.Text.Trim(),
                    Price = p,
                    Quantity = q,
                    CoverPath = coverPath.Text.Trim()
                };

                books.Add(book);
                nextId++;
                Reset();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi them sach: " + ex.Message);
            }
        }

        void EditBook()
        {
            if (selected == null)
            {
                MessageBox.Show("Chon sach can sua.");
                return;
            }

            if (!ValidateBook(out decimal p, out int q))
                return;

            selected.Title = title.Text.Trim();
            selected.Author = author.Text.Trim();
            selected.Price = p;
            selected.Quantity = q;
            selected.CoverPath = coverPath.Text.Trim();

            binding.ResetBindings(false);
            dgvBooks.Refresh();
            UpdateStatus();
        }

        void DeleteBook()
        {
            if (selected == null)
            {
                MessageBox.Show("Chon sach can xoa.");
                return;
            }

            if (MessageBox.Show(
                "Ban co chac muon xoa sach nay?",
                "Xac nhan xoa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            books.Remove(selected);
            Reset();
        }

        void LoadSelected()
        {
            if (dgvBooks.CurrentRow?.DataBoundItem is not BookModel book)
                return;

            selected = book;
            id.Text = book.BookId;
            title.Text = book.Title;
            author.Text = book.Author;
            price.Text = book.Price.ToString(CultureInfo.InvariantCulture);
            quantity.Text = book.Quantity.ToString();
            coverPath.Text = book.CoverPath;

            try
            {
                picBookCover.Image = null;

                if (File.Exists(book.CoverPath))
                {
                    using var image = Image.FromFile(book.CoverPath);
                    picBookCover.Image = new Bitmap(image);
                }
            }
            catch
            {
                picBookCover.Image = null;
            }
        }

        void Reset()
        {
            selected = null;
            id.Text = "S" + nextId.ToString("D3");
            title.Clear();
            author.Clear();
            price.Clear();
            quantity.Clear();
            coverPath.Clear();

            picBookCover.Image = null;
            dgvBooks.ClearSelection();
        }

        void LoadCover()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Chon anh bia sach"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            coverPath.Text = dialog.FileName;

            try
            {
                using var image = Image.FromFile(dialog.FileName);
                picBookCover.Image = new Bitmap(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khong doc duoc anh: " + ex.Message);
            }
        }

        void ExportCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "Books.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using var writer = new StreamWriter(
                    dialog.FileName, false, new UTF8Encoding(true));

                writer.WriteLine("BookId,Title,Author,Price,Quantity,CoverPath");

                foreach (var book in books)
                {
                    writer.WriteLine(string.Join(",",
                        Csv(book.BookId),
                        Csv(book.Title),
                        Csv(book.Author),
                        book.Price.ToString(CultureInfo.InvariantCulture),
                        book.Quantity.ToString(),
                        Csv(book.CoverPath)));
                }

                MessageBox.Show("Da xuat CSV thanh cong.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi xuat CSV: " + ex.Message);
            }
        }

        void ImportCsv()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Mo du lieu sach"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);

                var imported = new List<BookModel>();

                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var fields = ParseCsv(line);

                    if (fields.Count < 6)
                        continue;

                    if (!decimal.TryParse(
                        fields[3], NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal p))
                        continue;

                    if (!int.TryParse(fields[4], out int q))
                        continue;

                    imported.Add(new BookModel
                    {
                        BookId = fields[0],
                        Title = fields[1],
                        Author = fields[2],
                        Price = p,
                        Quantity = q,
                        CoverPath = fields[5]
                    });
                }

                books.Clear();

                foreach (var book in imported)
                    books.Add(book);

                nextId = books
                    .Select(b => int.TryParse(
                        b.BookId.TrimStart('S'), out int n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

                Reset();
                UpdateStatus();

                MessageBox.Show("Da nhap du lieu CSV.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi doc CSV: " + ex.Message);
            }
        }

        static string Csv(string text)
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        static List<string> ParseCsv(string line)
        {
            var result = new List<string>();
            var builder = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == ',' && !quoted)
                {
                    result.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                }
            }

            result.Add(builder.ToString());
            return result;
        }

        void SearchBook()
        {
            string term = search.Text.Trim();

            if (term == "")
            {
                filteredBooks = null;
                binding.DataSource = books;
                dgvBooks.DataSource = binding;
                return;
            }

            var results = books
                .Where(book => book.Title.Contains(
                    term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            filteredBooks = new BindingList<BookModel>(results);
            dgvBooks.DataSource = filteredBooks;
        }

        void UpdateStatus()
        {
            countStatus.Text = "Tong so sach: " + books.Count;
            valueStatus.Text =
                "Tong gia tri kho: " + books.Sum(b => b.TotalValue).ToString("N0") + " VND";
            timeStatus.Text = DateTime.Now.ToString("HH:mm:ss");
        }
    }
}
