using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bai3_HospitalMDI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new frmHospitalMain());
    }
}

public class frmHospitalMain : Form
{
    readonly ToolStripStatusLabel doctorStatus = new("Sẵn sàng | Chưa mở cửa sổ nào");
    readonly ToolStripProgressBar progress = new() { Minimum = 0, Maximum = 100, Value = 0, Width = 180 };
    readonly ToolStripStatusLabel progressLabel = new("Nạp dữ liệu: 0%");
    readonly System.Windows.Forms.Timer reloadTimer = new() { Interval = 25 };
    public frmHospitalMain()
    {
        Text = "Hệ thống quản lý bệnh viện";
        IsMdiContainer = true;
        Width = 1250; Height = 800; MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(238, 243, 248);

        var menu = new MenuStrip { Dock = DockStyle.Fill, BackColor = Color.White };
        var system = new ToolStripMenuItem("Hệ thống");
        system.DropDownItems.Add("Đăng xuất", null, (_, _) => Logout());
        system.DropDownItems.Add("Nạp lại dữ liệu", null, (_, _) => ReloadData());
        system.DropDownItems.Add(new ToolStripSeparator());
        system.DropDownItems.Add("Thoát", null, (_, _) => Close());
        var manage = new ToolStripMenuItem("Quản lý danh mục");
        manage.DropDownItems.Add("Hồ sơ bệnh nhân", null, (_, _) => OpenChild<frmPatientProfiles>());
        manage.DropDownItems.Add("Lịch trực bác sĩ", null, (_, _) => OpenChild<frmDoctorSchedule>());
        manage.DropDownItems.Add("Khoa / phòng", null, (_, _) => OpenChild<frmDepartment>());
        var windows = new ToolStripMenuItem("Cửa sổ");
        windows.DropDownItems.Add("Xếp tầng", null, (_, _) => LayoutMdi(MdiLayout.Cascade));
        windows.DropDownItems.Add("Xếp ngang", null, (_, _) => LayoutMdi(MdiLayout.TileHorizontal));
        windows.DropDownItems.Add("Xếp dọc", null, (_, _) => LayoutMdi(MdiLayout.TileVertical));
        windows.DropDownItems.Add("Đóng tất cả", null, (_, _) => { foreach (Form f in MdiChildren.ToArray()) f.Close(); UpdateWindowStatus(); });
        menu.Items.AddRange(new ToolStripItem[] { system, manage, windows });
        MainMenuStrip = menu;

        var toolbar = new ToolStrip { Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.FromArgb(232, 240, 248), Padding = new Padding(8, 3, 8, 3) };
        toolbar.Items.Add("👤 Hồ sơ bệnh nhân", null, (_, _) => OpenChild<frmPatientProfiles>());
        toolbar.Items.Add("🩺 Lịch trực bác sĩ", null, (_, _) => OpenChild<frmDoctorSchedule>());
        toolbar.Items.Add("🏥 Khoa / phòng", null, (_, _) => OpenChild<frmDepartment>());
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add("Nạp lại", null, (_, _) => ReloadData());

        var status = new StatusStrip { Dock = DockStyle.Bottom, SizingGrip = false, BackColor = Color.White };
        status.Items.Add(doctorStatus);
        status.Items.Add(new ToolStripStatusLabel { Spring = true });
        status.Items.Add(progressLabel);
        status.Items.Add(progress);

        var shell = new TableLayoutPanel { Dock = DockStyle.Top, Height = 66, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        shell.Controls.Add(menu, 0, 0); shell.Controls.Add(toolbar, 0, 1);
        Controls.Add(shell); Controls.Add(status);
        MainMenuStrip = menu;
        MdiChildActivate += (_, _) => UpdateWindowStatus();
        reloadTimer.Tick += (_, _) =>
        {
            progress.Value = Math.Min(100, progress.Value + 5);
            progressLabel.Text = $"Nạp dữ liệu: {progress.Value}%";
            if (progress.Value >= 100) { reloadTimer.Stop(); doctorStatus.Text = "Dữ liệu đã được nạp lại"; }
        };
        Shown += (_, _) =>
        {
            foreach (Control c in Controls)
                if (c is MdiClient client) client.BackColor = Color.FromArgb(235, 240, 246);
        };
    }

    void OpenChild<T>() where T : Form, new()
    {
        foreach (Form child in MdiChildren)
            if (child is T) { child.Activate(); return; }
        var form = new T { MdiParent = this };
        form.FormClosed += (_, _) => UpdateWindowStatus();
        form.Show();
        doctorStatus.Text = $"Đang mở: {form.Text}";
    }

    void UpdateWindowStatus() => doctorStatus.Text = $"Số cửa sổ đang mở: {MdiChildren.Length}";

    void ReloadData()
    {
        reloadTimer.Stop(); progress.Value = 0; progressLabel.Text = "Nạp dữ liệu: 0%"; doctorStatus.Text = "Đang nạp dữ liệu..."; reloadTimer.Start();
    }

    void Logout()
    {
        foreach (Form f in MdiChildren.ToArray()) f.Close();
        doctorStatus.Text = "Đã đăng xuất";
    }
}

public class frmPatientProfiles : Form
{
    readonly BindingSource source = new();
    readonly BindingList<Patient> patients = new();
    readonly DataGridView grid = new();
    readonly TextBox name = new(), age = new(), diagnosis = new();

    public frmPatientProfiles()
    {
        Text = "Hồ sơ bệnh nhân"; Width = 900; Height = 600; MinimumSize = new Size(700, 450);
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(246, 248, 251);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var heading = new Label { Text = "QUẢN LÝ HỒ SƠ BỆNH NHÂN", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(35, 78, 125), TextAlign = ContentAlignment.MiddleLeft };
        root.Controls.Add(heading, 0, 0);

        var entry = new GroupBox { Text = "Thông tin bệnh nhân", Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White };
        root.Controls.Add(entry, 0, 1);
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(4) };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        entry.Controls.Add(fields);
        AddField(fields, "Họ tên:", name, 0, 0); AddField(fields, "Tuổi:", age, 0, 2); AddField(fields, "Chẩn đoán:", diagnosis, 1, 0);
        fields.SetColumnSpan(diagnosis, 2);
        var add = new Button { Text = "+ Thêm bệnh nhân", Width = 155, Height = 32, BackColor = Color.FromArgb(35, 139, 95), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(4) };
        add.FlatAppearance.BorderSize = 0;
        add.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text) || !int.TryParse(age.Text, out int a) || a < 0)
            { MessageBox.Show("Vui lòng nhập họ tên và tuổi hợp lệ.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try { patients.Add(new Patient { Name = name.Text.Trim(), Age = a, Diagnosis = diagnosis.Text.Trim() }); name.Clear(); age.Clear(); diagnosis.Clear(); name.Focus(); }
            catch (Exception ex) { MessageBox.Show("Không thể thêm bệnh nhân: " + ex.Message); }
        };
        fields.Controls.Add(add, 3, 1);

        var listBox = new GroupBox { Text = "Danh sách bệnh nhân", Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Color.White };
        root.Controls.Add(listBox, 0, 2);
        grid.Dock = DockStyle.Fill; grid.AutoGenerateColumns = true; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.MultiSelect = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; StyleGrid(grid);
        listBox.Controls.Add(grid); source.DataSource = patients; grid.DataSource = source;
    }

    static void AddField(TableLayoutPanel panel, string label, Control input, int row, int col)
    {
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(4) }, col, row);
        input.Dock = DockStyle.Fill; input.Margin = new Padding(4, 5, 8, 5); panel.Controls.Add(input, col + 1, row);
    }

    internal static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.FixedSingle; grid.RowHeadersVisible = false; grid.RowTemplate.Height = 32; grid.ColumnHeadersHeight = 38; grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(222, 233, 247); grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 253);
    }
}

public class Patient { public string Name { get; set; } = ""; public int Age { get; set; } public string Diagnosis { get; set; } = ""; }

public class frmDoctorSchedule : Form
{
    public frmDoctorSchedule()
    {
        Text = "Lịch trực bác sĩ"; Width = 800; Height = 500; MinimumSize = new Size(650, 400); Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(246, 248, 251);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(14) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
        root.Controls.Add(new Label { Text = "LỊCH TRỰC BÁC SĨ", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(35, 78, 125), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true, AllowUserToAddRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = new BindingList<DoctorShift> {
            new() { Doctor = "BS. Nguyễn An", Department = "Nội", Shift = "07:00 - 15:00" },
            new() { Doctor = "BS. Trần Bình", Department = "Ngoại", Shift = "15:00 - 23:00" },
            new() { Doctor = "BS. Lê Chi", Department = "Cấp cứu", Shift = "23:00 - 07:00" }
        } };
        frmPatientProfiles.StyleGrid(grid); root.Controls.Add(grid, 0, 1);
    }
}
public class DoctorShift { public string Doctor { get; set; } = ""; public string Department { get; set; } = ""; public string Shift { get; set; } = ""; }

public class frmDepartment : Form
{
    public frmDepartment()
    {
        Text = "Danh mục khoa/phòng"; Width = 600; Height = 450; MinimumSize = new Size(450, 350); Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(246, 248, 251);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(18) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
        root.Controls.Add(new Label { Text = "DANH MỤC KHOA / PHÒNG", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(35, 78, 125), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var list = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12), IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        list.Items.AddRange(new object[] { "Khoa Nội", "Khoa Ngoại", "Khoa Cấp cứu", "Khoa Nhi", "Khoa Xét nghiệm" });
        root.Controls.Add(list, 0, 1);
    }
}
