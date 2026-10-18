
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai4_RevenueAnalytics
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new FormRevenue());
        }
    }

    public class RevenueItem
    {
        public string Ngay { get; set; } = "";
        public string MaDon { get; set; } = "";
        public string SanPham { get; set; } = "";
        public int SoLuong { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class FormRevenue : Form
    {
        private readonly DataGridView dgvRevenue = new();
        private readonly ProgressBar progressBar = new();
        private readonly Label lblProgress = new();
        private readonly Label lblTotal = new();
        private readonly Label lblCount = new();
        private readonly Label lblStatus = new();

        private readonly Button btnStart = new();
        private readonly Button btnCancel = new();
        private readonly Button btnReset = new();

        private readonly BindingList<RevenueItem> revenueItems = new();

        private CancellationTokenSource? cancellationTokenSource;
        private bool isRunning;

        public FormRevenue()
        {
            Text = "Phan tich doanh thu";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1150, 760);
            MinimumSize = new Size(950, 650);
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 250);

            BuildUI();

            dgvRevenue.DataSource = revenueItems;
            UpdateSummary();
        }

        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(14),
                Margin = Padding.Empty
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 155));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(root);

            var title = new Label
            {
                Text = "PHAN TICH DOANH THU",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 75, 130)
            };

            root.Controls.Add(title, 0, 0);

            var summary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 10)
            };

            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            summary.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            summary.Controls.Add(
                CreateSummaryCard("TONG DOANH THU", lblTotal), 0, 0);

            summary.Controls.Add(
                CreateSummaryCard("SO GIAO DICH", lblCount), 1, 0);

            root.Controls.Add(summary, 0, 1);

            var processGroup = new GroupBox
            {
                Text = "Xu ly du lieu",
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.White
            };

            root.Controls.Add(processGroup, 0, 2);

            var processLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(3)
            };

            processLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 110));

            processLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            processLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 65));

            processLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 35));

            processLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 28));

            processLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            processGroup.Controls.Add(processLayout);

            var lblProcess = new Label
            {
                Text = "Tien trinh:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            progressBar.Dock = DockStyle.Fill;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            progressBar.Margin = new Padding(4, 7, 4, 7);

            lblProgress.Text = "0%";
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.TextAlign = ContentAlignment.MiddleCenter;

            processLayout.Controls.Add(lblProcess, 0, 0);
            processLayout.Controls.Add(progressBar, 1, 0);
            processLayout.Controls.Add(lblProgress, 2, 0);

            lblStatus.Text = "San sang xu ly";
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.ForeColor = Color.DimGray;

            processLayout.Controls.Add(lblStatus, 0, 1);
            processLayout.SetColumnSpan(lblStatus, 3);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnStart.Text = "Bat dau thong ke";
            btnCancel.Text = "Huy xu ly";
            btnReset.Text = "Lam moi";

            StyleButton(btnStart, Color.FromArgb(38, 125, 80));
            StyleButton(btnCancel, Color.FromArgb(190, 65, 65));
            StyleButton(btnReset, Color.FromArgb(85, 100, 120));

            btnStart.Click += async (_, _) => await StartAnalysisAsync();
            btnCancel.Click += (_, _) => CancelAnalysis();
            btnReset.Click += (_, _) => ResetData();

            btnCancel.Enabled = false;

            buttonPanel.Controls.Add(btnStart);
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnReset);

            processLayout.Controls.Add(buttonPanel, 0, 2);
            processLayout.SetColumnSpan(buttonPanel, 3);

            var gridGroup = new GroupBox
            {
                Text = "Chi tiet doanh thu",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                BackColor = Color.White
            };

            ConfigureGrid();
            gridGroup.Controls.Add(dgvRevenue);
            root.Controls.Add(gridGroup, 0, 3);
        }

        private Panel CreateSummaryCard(string heading, Label value)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5),
                Padding = new Padding(8)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

            var label = new Label
            {
                Text = heading,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            value.Text = "0";
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.MiddleCenter;
            value.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            value.ForeColor = Color.FromArgb(35, 105, 180);

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(value, 0, 1);
            card.Controls.Add(layout);

            return card;
        }

        private void StyleButton(Button button, Color color)
        {
            button.Size = new Size(155, 32);
            button.Margin = new Padding(0, 0, 10, 0);
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        private void ConfigureGrid()
        {
            dgvRevenue.Dock = DockStyle.Fill;
            dgvRevenue.ReadOnly = true;
            dgvRevenue.AllowUserToAddRows = false;
            dgvRevenue.AllowUserToDeleteRows = false;
            dgvRevenue.MultiSelect = false;
            dgvRevenue.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvRevenue.AutoGenerateColumns = false;
            dgvRevenue.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
            dgvRevenue.RowHeadersVisible = false;
            dgvRevenue.BackgroundColor = Color.White;
            dgvRevenue.BorderStyle = BorderStyle.FixedSingle;
            dgvRevenue.RowTemplate.Height = 32;
            dgvRevenue.ColumnHeadersHeight = 38;
            dgvRevenue.EnableHeadersVisualStyles = false;

            dgvRevenue.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(220, 233, 248);

            dgvRevenue.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            dgvRevenue.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(246, 249, 253);

            AddColumn("Ngay", nameof(RevenueItem.Ngay), 18);
            AddColumn("Ma don", nameof(RevenueItem.MaDon), 18);
            AddColumn("San pham", nameof(RevenueItem.SanPham), 28);
            AddColumn("So luong", nameof(RevenueItem.SoLuong), 14);
            AddColumn("Doanh thu", nameof(RevenueItem.DoanhThu), 22, "N0");
        }

        private void AddColumn(
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
                MinimumWidth = 70
            };

            if (format != "")
            {
                column.DefaultCellStyle.Format = format;
                column.DefaultCellStyle.Alignment =
                    DataGridViewContentAlignment.MiddleRight;
            }

            dgvRevenue.Columns.Add(column);
        }

        private async Task StartAnalysisAsync()
        {
            if (isRunning)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            isRunning = true;
            btnStart.Enabled = false;
            btnCancel.Enabled = true;
            btnReset.Enabled = false;

            progressBar.Value = 0;
            lblProgress.Text = "0%";
            lblStatus.Text = "Dang tong hop du lieu...";

            try
            {
                IProgress<int> progress = new Progress<int>(value =>
                {
                    int safeValue = Math.Max(0, Math.Min(100, value));
                    progressBar.Value = safeValue;
                    lblProgress.Text = safeValue + "%";
                });

                List<RevenueItem> results = await Task.Run(
                    () => CalculateRevenue(progress, token), token);

                revenueItems.Clear();

                foreach (var item in results)
                    revenueItems.Add(item);

                UpdateSummary();
                lblStatus.Text = "Thong ke hoan tat!";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Da huy thao tac!";
                MessageBox.Show(
                    "Da huy thao tac!",
                    "Thong bao",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Xu ly that bai";
                MessageBox.Show(
                    "Loi khi thong ke: " + ex.Message,
                    "Loi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                isRunning = false;
                btnStart.Enabled = true;
                btnCancel.Enabled = false;
                btnReset.Enabled = true;

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        private List<RevenueItem> CalculateRevenue(
            IProgress<int> progress,
            CancellationToken token)
        {
            var results = new List<RevenueItem>();
            var random = new Random();

            string[] products =
            {
                "Laptop", "Ban phim", "Chuot",
                "Man hinh", "Tai nghe", "USB"
            };

            for (int i = 1; i <= 100; i++)
            {
                token.ThrowIfCancellationRequested();

                Thread.Sleep(50);

                if (i % 10 == 0)
                {
                    results.Add(new RevenueItem
                    {
                        Ngay = DateTime.Today
                            .AddDays(-random.Next(0, 30))
                            .ToString("dd/MM/yyyy"),
                        MaDon = "DH" + (1000 + i),
                        SanPham = products[random.Next(products.Length)],
                        SoLuong = random.Next(1, 10),
                        DoanhThu = random.Next(100, 5000) * 1000m
                    });
                }

                progress.Report(i);
            }

            token.ThrowIfCancellationRequested();
            return results;
        }

        private void CancelAnalysis()
        {
            if (isRunning)
            {
                cancellationTokenSource?.Cancel();
                lblStatus.Text = "Dang huy thao tac...";
            }
        }

        private void ResetData()
        {
            if (isRunning)
                return;

            revenueItems.Clear();
            progressBar.Value = 0;
            lblProgress.Text = "0%";
            lblStatus.Text = "San sang xu ly";

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            decimal total = revenueItems.Sum(item => item.DoanhThu);

            lblTotal.Text = total.ToString("N0") + " VND";
            lblCount.Text = revenueItems.Count.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isRunning)
                cancellationTokenSource?.Cancel();

            base.OnFormClosing(e);
        }
    }
}
