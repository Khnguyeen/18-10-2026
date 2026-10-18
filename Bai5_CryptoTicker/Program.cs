using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bai5_CryptoTicker;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new frmCryptoStockTicker());
    }
}

public class PriceLog
{
    public string Time { get; set; } = "";
    public decimal Price { get; set; }
    public string Change { get; set; } = "";
}

public class frmCryptoStockTicker : Form
{
    readonly Button start = new() { Text = "▶  Bắt đầu giám sát", Width = 175, Height = 38 };
    readonly Button pause = new() { Text = "Ⅱ  Tạm dừng", Width = 145, Height = 38, Enabled = false };
    readonly Label lblBTCPrice = new() { Text = "$65,000.00", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 30, FontStyle.Bold), ForeColor = Color.FromArgb(35, 78, 125) };
    readonly Label priceCaption = new() { Text = "BTC / USD", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(75, 88, 105) };
    readonly Label trendLabel = new() { Text = "Đang chờ dữ liệu", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.DimGray };
    readonly DataGridView dgvHistoryLog = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, AllowUserToAddRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    readonly ToolStripStatusLabel lastUpdate = new("Lần cập nhật cuối: --:--:--");
    readonly ToolStripStatusLabel successCount = new("Số lượt đồng bộ: 0");
    readonly BindingList<PriceLog> logs = new();
    CancellationTokenSource? cts;
    decimal currentPrice = 65000m;
    int updates;

    public frmCryptoStockTicker()
    {
        Text = "Crypto Ticker | BTC/USD"; Width = 1050; Height = 720; MinimumSize = new Size(850, 600); StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(245, 247, 250);
        BuildInterface();
        start.Click += async (_, _) => await StartMonitoringAsync();
        pause.Click += (_, _) => cts?.Cancel();
        FormClosing += (_, _) => cts?.Cancel();
    }

    void BuildInterface()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(16) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); heading.RowStyles.Add(new RowStyle(SizeType.Percent, 68)); heading.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
        heading.Controls.Add(new Label { Text = "CRYPTOCURRENCY TICKER", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(35, 78, 125), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        heading.Controls.Add(new Label { Text = "Theo dõi biến động giá BTC/USD theo chu kỳ 2 giây", Dock = DockStyle.Fill, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        root.Controls.Add(heading, 0, 0);

        var quote = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), Margin = new Padding(0, 4, 0, 8) };
        quote.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(220, 228, 238)); e.Graphics.DrawRectangle(pen, 0, 0, quote.Width - 1, quote.Height - 1); };
        var quoteLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        quoteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); quoteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); quoteLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); quoteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        quoteLayout.Controls.Add(priceCaption, 0, 0); quoteLayout.Controls.Add(lblBTCPrice, 0, 1); quoteLayout.Controls.Add(trendLabel, 0, 2); quote.Controls.Add(quoteLayout); root.Controls.Add(quote, 0, 1);

        var actionBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 5, 0, 5) };
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185)); actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155)); actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        StyleButton(start, Color.FromArgb(36, 145, 95)); StyleButton(pause, Color.FromArgb(190, 65, 65));
        actionBar.Controls.Add(start, 0, 0); actionBar.Controls.Add(pause, 1, 0);
        actionBar.Controls.Add(new Label { Text = "Giá mô phỏng • Không phải dữ liệu giao dịch thực", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9, FontStyle.Italic) }, 2, 0);
        root.Controls.Add(actionBar, 0, 2);

        var history = new GroupBox { Text = "  Lịch sử biến động giá  ", Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Color.White };
        ConfigureGrid(); dgvHistoryLog.DataSource = logs; history.Controls.Add(dgvHistoryLog); root.Controls.Add(history, 0, 3);

        var status = new StatusStrip { Dock = DockStyle.Bottom, SizingGrip = false, BackColor = Color.White };
        status.Items.Add(lastUpdate); status.Items.Add(new ToolStripStatusLabel { Spring = true }); status.Items.Add(successCount); Controls.Add(status);
    }

    static void StyleButton(Button button, Color color)
    {
        button.Dock = DockStyle.Fill; button.Margin = new Padding(0, 0, 10, 0); button.BackColor = color; button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.Font = new Font("Segoe UI", 9, FontStyle.Bold); button.Cursor = Cursors.Hand;
    }

    void ConfigureGrid()
    {
        dgvHistoryLog.BackgroundColor = Color.White; dgvHistoryLog.BorderStyle = BorderStyle.FixedSingle; dgvHistoryLog.RowHeadersVisible = false; dgvHistoryLog.RowTemplate.Height = 32; dgvHistoryLog.ColumnHeadersHeight = 38; dgvHistoryLog.EnableHeadersVisualStyles = false;
        dgvHistoryLog.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(222, 233, 247); dgvHistoryLog.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); dgvHistoryLog.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 253);
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thời gian", DataPropertyName = nameof(PriceLog.Time), FillWeight = 30 });
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giá BTC/USD", DataPropertyName = nameof(PriceLog.Price), FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Format = "$#,##0.00", Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Biến động", DataPropertyName = nameof(PriceLog.Change), FillWeight = 25 });
        dgvHistoryLog.CellFormatting += (_, e) =>
        {
            if (dgvHistoryLog.Columns[e.ColumnIndex].DataPropertyName == nameof(PriceLog.Change) && e.Value is string change)
            {
                e.CellStyle.ForeColor = change == "Tăng" ? Color.ForestGreen : change == "Giảm" ? Color.Firebrick : Color.DimGray;
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
        };
    }

    async Task StartMonitoringAsync()
    {
        if (cts != null) return;
        cts = new CancellationTokenSource(); var token = cts.Token;
        start.Enabled = false; pause.Enabled = true; trendLabel.Text = "Đang giám sát...";
        try
        {
            await Task.Run(async () =>
            {
                var random = new Random();
                while (true)
                {
                    await Task.Delay(2000, token);
                    token.ThrowIfCancellationRequested();
                    decimal previous = currentPrice;
                    decimal next = previous * (1 + (decimal)(random.NextDouble() - 0.5) * 0.012m);
                    string time = DateTime.Now.ToString("HH:mm:ss");
                    string change = next >= previous ? "Tăng" : "Giảm";
                    currentPrice = next;
                    UpdateUI(() =>
                    {
                        lblBTCPrice.Text = $"${next:N2}";
                        lblBTCPrice.ForeColor = change == "Tăng" ? Color.ForestGreen : Color.Firebrick;
                        trendLabel.Text = $"{change} so với lần cập nhật trước  •  {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                        lastUpdate.Text = "Lần cập nhật cuối: " + time;
                        updates++; successCount.Text = "Số lượt đồng bộ: " + updates;
                        logs.Insert(0, new PriceLog { Time = time, Price = next, Change = change });
                        if (logs.Count > 100) logs.RemoveAt(logs.Count - 1);
                    });
                }
            }, token);
        }
        catch (OperationCanceledException) { /* Tạm dừng theo yêu cầu người dùng */ }
        catch (Exception ex) { if (!IsDisposed) MessageBox.Show("Lỗi giám sát tỷ giá: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            cts?.Dispose(); cts = null;
            if (!IsDisposed) { start.Enabled = true; pause.Enabled = false; trendLabel.Text = "Đã tạm dừng giám sát"; }
        }
    }

    void UpdateUI(Action action)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired) Invoke(new Action(() => { if (!IsDisposed && !Disposing) action(); }));
        else action();
    }
}
