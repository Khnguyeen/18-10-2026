
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Bai1_GymMemberManager
{
    public class MemberModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public int Fee { get; set; }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new FormGymMember());
        }
    }

    public class FormGymMember : Form
    {
        private readonly BindingList<MemberModel> members = new();
        private readonly BindingSource bindingSource = new();
        private readonly ErrorProvider errorProvider = new();

        private readonly TextBox txtId = new();
        private readonly TextBox txtName = new();
        private readonly TextBox txtPhone = new();
        private readonly TextBox txtFee = new();
        private readonly DataGridView dgvMembers = new();

        private int nextId = 1;

        public FormGymMember()
        {
            Text = "QUAN LY HOI VIEN PHONG GYM";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 700);
            MinimumSize = new Size(900, 600);
            Font = new Font("Segoe UI", 10);
            BackColor = Color.White;

            errorProvider.ContainerControl = this;

            CreateInterface();
            ConfigureGrid();

            bindingSource.DataSource = members;
            dgvMembers.DataSource = bindingSource;

            dgvMembers.SelectionChanged += DgvMembers_SelectionChanged;
        }

        private void CreateInterface()
        {
            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.Padding = new Padding(15);
            main.ColumnCount = 1;
            main.RowCount = 4;

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 175));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(main);

            Label lblTitle = new Label();
            lblTitle.Text = "QUAN LY HOI VIEN PHONG GYM";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            lblTitle.ForeColor = Color.DarkBlue;

            main.Controls.Add(lblTitle, 0, 0);

            GroupBox groupInput = new GroupBox();
            groupInput.Text = "Thong tin hoi vien";
            groupInput.Dock = DockStyle.Fill;
            groupInput.Padding = new Padding(12);

            TableLayoutPanel inputLayout = new TableLayoutPanel();
            inputLayout.Dock = DockStyle.Fill;
            inputLayout.ColumnCount = 4;
            inputLayout.RowCount = 2;
            inputLayout.Padding = new Padding(5);

            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 15));
            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 35));
            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 15));
            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 35));

            inputLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));
            inputLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));

            AddInputRow(inputLayout, 0, "Ho ten:", txtName,
                "So dien thoai:", txtPhone);

            AddInputRow(inputLayout, 1, "Ma hoi vien:", txtId,
                "Phi tap (VND):", txtFee);

            txtId.ReadOnly = true;
            txtId.BackColor = Color.WhiteSmoke;

            groupInput.Controls.Add(inputLayout);
            main.Controls.Add(groupInput, 0, 1);

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.WrapContents = false;
            buttonPanel.Padding = new Padding(0, 7, 0, 0);

            Button btnAdd = CreateButton("Them", Color.SeaGreen);
            Button btnEdit = CreateButton("Cap nhat", Color.RoyalBlue);
            Button btnDelete = CreateButton("Xoa", Color.IndianRed);
            Button btnReset = CreateButton("Lam moi", Color.DimGray);

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnReset.Click += BtnReset_Click;

            buttonPanel.Controls.Add(btnAdd);
            buttonPanel.Controls.Add(btnEdit);
            buttonPanel.Controls.Add(btnDelete);
            buttonPanel.Controls.Add(btnReset);

            main.Controls.Add(buttonPanel, 0, 2);

            GroupBox groupList = new GroupBox();
            groupList.Text = "Danh sach hoi vien";
            groupList.Dock = DockStyle.Fill;
            groupList.Padding = new Padding(8);

            dgvMembers.Dock = DockStyle.Fill;
            dgvMembers.BackgroundColor = Color.White;
            dgvMembers.BorderStyle = BorderStyle.FixedSingle;
            dgvMembers.AllowUserToAddRows = false;
            dgvMembers.AllowUserToDeleteRows = false;
            dgvMembers.ReadOnly = true;
            dgvMembers.MultiSelect = false;
            dgvMembers.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvMembers.AutoGenerateColumns = false;
            dgvMembers.RowHeadersVisible = false;
            dgvMembers.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
            dgvMembers.RowTemplate.Height = 32;
            dgvMembers.ColumnHeadersHeight = 38;
            dgvMembers.EnableHeadersVisualStyles = false;
            dgvMembers.ColumnHeadersDefaultCellStyle.BackColor =
                Color.LightSteelBlue;
            dgvMembers.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);
            dgvMembers.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 248, 252);

            groupList.Controls.Add(dgvMembers);
            main.Controls.Add(groupList, 0, 3);
        }

        private void AddInputRow(
            TableLayoutPanel layout,
            int row,
            string label1,
            TextBox box1,
            string label2,
            TextBox box2)
        {
            Label firstLabel = CreateLabel(label1);
            Label secondLabel = CreateLabel(label2);

            box1.Dock = DockStyle.Fill;
            box2.Dock = DockStyle.Fill;
            box1.Margin = new Padding(5, 8, 15, 8);
            box2.Margin = new Padding(5, 8, 5, 8);

            layout.Controls.Add(firstLabel, 0, row);
            layout.Controls.Add(box1, 1, row);
            layout.Controls.Add(secondLabel, 2, row);
            layout.Controls.Add(box2, 3, row);
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Margin = new Padding(5, 5, 0, 5)
            };
        }

        private Button CreateButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(125, 35);
            button.Margin = new Padding(0, 0, 12, 0);
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            return button;
        }

        private void ConfigureGrid()
        {
            dgvMembers.Columns.Clear();

            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "Ma hoi vien",
                DataPropertyName = "Id",
                FillWeight = 20
            });

            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Ho ten",
                DataPropertyName = "Name",
                FillWeight = 35
            });

            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPhone",
                HeaderText = "So dien thoai",
                DataPropertyName = "Phone",
                FillWeight = 25
            });

            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFee",
                HeaderText = "Phi tap (VND)",
                DataPropertyName = "Fee",
                FillWeight = 25,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
        }

        private bool ValidateInput()
        {
            errorProvider.Clear();

            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                errorProvider.SetError(txtName, "Ho ten khong duoc de trong.");
                valid = false;
            }

            string phone = txtPhone.Text.Trim();

            if (phone.Length != 10 ||
                !phone.All(char.IsDigit))
            {
                errorProvider.SetError(
                    txtPhone,
                    "So dien thoai phai gom dung 10 chu so.");
                valid = false;
            }

            if (!int.TryParse(txtFee.Text.Trim(), out int fee) ||
                fee <= 0)
            {
                errorProvider.SetError(
                    txtFee,
                    "Phi tap phai la so nguyen duong.");
                valid = false;
            }

            return valid;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                MemberModel member = new MemberModel
                {
                    Id = "HV" + nextId.ToString("D3"),
                    Name = txtName.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Fee = int.Parse(txtFee.Text.Trim())
                };

                members.Add(member);
                nextId++;

                ResetInputs();
                MessageBox.Show(
                    "Them hoi vien thanh cong!",
                    "Thong bao",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Khong the them hoi vien: " + ex.Message,
                    "Loi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvMembers.CurrentRow == null ||
                dgvMembers.CurrentRow.DataBoundItem is not MemberModel member)
            {
                MessageBox.Show("Vui long chon hoi vien can cap nhat.");
                return;
            }

            if (!ValidateInput())
                return;

            try
            {
                member.Name = txtName.Text.Trim();
                member.Phone = txtPhone.Text.Trim();
                member.Fee = int.Parse(txtFee.Text.Trim());

                bindingSource.ResetCurrentItem();
                ResetInputs();

                MessageBox.Show("Cap nhat hoi vien thanh cong!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Khong the cap nhat: " + ex.Message,
                    "Loi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvMembers.CurrentRow == null ||
                dgvMembers.CurrentRow.DataBoundItem is not MemberModel member)
            {
                MessageBox.Show("Vui long chon hoi vien can xoa.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Ban co chac muon xoa hoi vien " + member.Name + "?",
                "Xac nhan xoa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            members.Remove(member);
            ResetInputs();
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            ResetInputs();
            dgvMembers.ClearSelection();
        }

        private void DgvMembers_SelectionChanged(
            object? sender, EventArgs e)
        {
            if (dgvMembers.CurrentRow == null ||
                dgvMembers.CurrentRow.DataBoundItem is not MemberModel member)
                return;

            txtId.Text = member.Id;
            txtName.Text = member.Name;
            txtPhone.Text = member.Phone;
            txtFee.Text = member.Fee.ToString();

            errorProvider.Clear();
        }

        private void ResetInputs()
        {
            txtId.Clear();
            txtName.Clear();
            txtPhone.Clear();
            txtFee.Clear();
            errorProvider.Clear();

            dgvMembers.ClearSelection();
            txtName.Focus();
        }
    }
}
