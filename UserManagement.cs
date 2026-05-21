using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class UserManagement : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private int currentLoggedInUserId;
        private string currentUserRole = "Staff";
        private List<int> hiddenUserIds = new List<int>();

        public UserManagement()
        {
            InitializeComponent();
            this.currentLoggedInUserId = 1;
            this.currentUserRole = "Admin";
            InitializeUserManagementForm();
        }

        public UserManagement(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            GetUserRoleFromDatabase();
            InitializeUserManagementForm();
        }

        private void GetUserRoleFromDatabase()
        {
            string query = "SELECT Role FROM Users WHERE UserID = @UserID;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", this.currentLoggedInUserId);
                try
                {
                    conn.Open();
                    object res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                    {
                        this.currentUserRole = res.ToString().Trim();
                    }
                }
                catch { this.currentUserRole = "Staff"; }
            }
        }

        private void InitializeUserManagementForm()
        {
            LoadUserProfilePicture();

            ComboBox targetFilter = FindControlRecursive<ComboBox>(this, "cmbFilters");
            if (targetFilter == null) targetFilter = FindControlRecursive<ComboBox>(this, "cmbFilter");
            if (targetFilter == null) targetFilter = FindControlRecursive<ComboBox>(this, "comboBox1");

            if (targetFilter != null)
            {
                targetFilter.Items.Clear();
                targetFilter.Items.AddRange(new string[] { "All", "Staff", "Admin" });
                targetFilter.SelectedIndex = 0;
                targetFilter.SelectedIndexChanged += (s, e) => RefreshUserGridData();
            }

            TextBox targetSearch = FindControlRecursive<TextBox>(this, "txtSearch");
            if (targetSearch == null) targetSearch = FindControlRecursive<TextBox>(this, "textBox1");

            if (targetSearch != null)
            {
                targetSearch.TextChanged -= txtSearch_TextChanged;
            }

            Button targetSearchBtn = FindControlRecursive<Button>(this, "btnSearch");
            if (targetSearchBtn == null) targetSearchBtn = FindControlRecursive<Button>(this, "button1");

            if (targetSearchBtn != null)
            {
                targetSearchBtn.Click -= btnSearch_Click;
                targetSearchBtn.Click += btnSearch_Click;
            }

            if (!this.currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Access Denied.\n\nOnly administrative accounts are authorized to modify user settings.", "Security Clearance Exception", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                this.Load += (s, e) => { BeginInvoke(new Action(() => SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)))); };
                return;
            }

            ConfigureUserGridColumns();
            RefreshUserGridData();
        }

        private T FindControlRecursive<T>(Control container, string name) where T : Control
        {
            if (container.Name == name && container is T) return (T)container;
            foreach (Control ctrl in container.Controls)
            {
                T found = FindControlRecursive<T>(ctrl, name);
                if (found != null) return found;
            }
            return null;
        }

        private void ConfigureUserGridColumns()
        {
            if (dataGridView1 == null) return;

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AllowUserToResizeColumns = true;

            dataGridView1.CellContentClick -= DataGridView1_CellContentClick;
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
        }

        private void RefreshUserGridData()
        {
            if (dataGridView1 == null) return;

            ComboBox targetFilter = FindControlRecursive<ComboBox>(this, "cmbFilters");
            if (targetFilter == null) targetFilter = FindControlRecursive<ComboBox>(this, "cmbFilter");
            if (targetFilter == null) targetFilter = FindControlRecursive<ComboBox>(this, "comboBox1");
            string filterValue = targetFilter != null ? targetFilter.SelectedItem?.ToString() : "All";

            TextBox targetSearch = FindControlRecursive<TextBox>(this, "txtSearch");
            if (targetSearch == null) targetSearch = FindControlRecursive<TextBox>(this, "textBox1");
            string searchKeyword = targetSearch != null ? targetSearch.Text.Trim() : "";

            string query = "SELECT UserID, Email, FullName, Role, Status FROM Users WHERE 1=1";

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query += " AND (Email LIKE '%' + @Search + '%' OR FullName LIKE '%' + @Search + '%')";
            }

            if (filterValue == "Admin" || filterValue == "Staff")
            {
                query += " AND Role = @Filter";
            }

            query += " ORDER BY FullName ASC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrEmpty(searchKeyword)) cmd.Parameters.AddWithValue("@Search", searchKeyword);
                if (filterValue != "All") cmd.Parameters.AddWithValue("@Filter", filterValue);

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        dataGridView1.Rows.Clear();

                        while (reader.Read())
                        {
                            int dbUserId = Convert.ToInt32(reader["UserID"]);

                            if (hiddenUserIds.Contains(dbUserId)) continue;

                            string dbEmail = reader["Email"].ToString();
                            string dbFullName = reader["FullName"].ToString();
                            string dbRole = reader["Role"].ToString();
                            string dbStatus = reader["Status"].ToString();

                            int rowIndex = dataGridView1.Rows.Add();

                            if (dataGridView1.Columns.Contains("username")) dataGridView1.Rows[rowIndex].Cells["username"].Value = dbEmail;
                            else dataGridView1.Rows[rowIndex].Cells[0].Value = dbEmail;

                            if (dataGridView1.Columns.Contains("fullName")) dataGridView1.Rows[rowIndex].Cells["fullName"].Value = dbFullName;
                            else if (dataGridView1.Columns.Contains("FullName")) dataGridView1.Rows[rowIndex].Cells["FullName"].Value = dbFullName;
                            else dataGridView1.Rows[rowIndex].Cells[1].Value = dbFullName;

                            if (dataGridView1.Columns.Contains("role")) dataGridView1.Rows[rowIndex].Cells["role"].Value = dbRole;
                            else if (dataGridView1.Columns.Contains("Role")) dataGridView1.Rows[rowIndex].Cells["Role"].Value = dbRole;
                            else dataGridView1.Rows[rowIndex].Cells[2].Value = dbRole;

                            if (dataGridView1.Columns.Contains("status")) dataGridView1.Rows[rowIndex].Cells["status"].Value = dbStatus;
                            else if (dataGridView1.Columns.Contains("Status")) dataGridView1.Rows[rowIndex].Cells["Status"].Value = dbStatus;
                            else dataGridView1.Rows[rowIndex].Cells[3].Value = dbStatus;

                            if (dataGridView1.Columns.Contains("action")) dataGridView1.Rows[rowIndex].Cells["action"].Value = "Edit Profile";
                            else if (dataGridView1.Columns.Contains("Action")) dataGridView1.Rows[rowIndex].Cells["Action"].Value = "Edit Profile";
                            else dataGridView1.Rows[rowIndex].Cells[4].Value = "Edit Profile";

                            dataGridView1.Rows[rowIndex].Tag = dbUserId;
                        }
                    }
                }
                catch { }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            RefreshUserGridData();
        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridView dgv = (DataGridView)sender;
            if (e.ColumnIndex == 4 || dgv.Columns[e.ColumnIndex].Name.Equals("action", StringComparison.OrdinalIgnoreCase))
            {
                if (dgv.Rows[e.RowIndex].Tag == null) return;
                int targetUserId = Convert.ToInt32(dgv.Rows[e.RowIndex].Tag);

                string oldEmail = dgv.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? "";
                string oldName = dgv.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
                string oldRole = dgv.Rows[e.RowIndex].Cells[2].Value?.ToString()?.Trim() ?? "Staff";
                string oldStatus = dgv.Rows[e.RowIndex].Cells[3].Value?.ToString()?.Trim() ?? "Active";

                string oldPassword = "";
                string pwdQuery = "SELECT PasswordHash FROM Users WHERE UserID = @TargetID;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(pwdQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TargetID", targetUserId);
                    try
                    {
                        conn.Open();
                        object res = cmd.ExecuteScalar();
                        if (res != null) oldPassword = res.ToString();
                    }
                    catch { }
                }

                Form editModal = new Form()
                {
                    Width = 420,
                    Height = 420,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = "Edit User Profile",
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };

                Label lblUser = new Label { Text = "Email Address:", Location = new Point(20, 20), Size = new Size(100, 20) };
                TextBox txtUser = new TextBox { Text = oldEmail, Location = new Point(140, 20), Size = new Size(230, 20) };

                Label lblPass = new Label { Text = "Password:", Location = new Point(20, 60), Size = new Size(100, 20) };
                TextBox txtPass = new TextBox { Text = oldPassword, Location = new Point(140, 60), Size = new Size(230, 20), PasswordChar = '*' };

                CheckBox chkShowPass = new CheckBox { Text = "Show Password", Location = new Point(140, 90), Size = new Size(150, 20) };
                chkShowPass.CheckedChanged += (s, ev) => { txtPass.PasswordChar = chkShowPass.Checked ? '\0' : '*'; };

                Label lblName = new Label { Text = "Full Name:", Location = new Point(20, 130), Size = new Size(100, 20) };
                TextBox txtName = new TextBox { Text = oldName, Location = new Point(140, 130), Size = new Size(230, 20) };

                Label lblRole = new Label { Text = "System Role:", Location = new Point(20, 170), Size = new Size(100, 20) };
                ComboBox cmbRole = new ComboBox { Location = new Point(140, 170), Size = new Size(230, 20), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbRole.Items.AddRange(new string[] { "Admin", "Staff" });
                cmbRole.SelectedItem = cmbRole.Items.Contains(oldRole) ? oldRole : "Staff";

                Label lblStatus = new Label { Text = "Account Status:", Location = new Point(20, 210), Size = new Size(100, 20) };
                ComboBox cmbUserStatus = new ComboBox { Location = new Point(140, 210), Size = new Size(230, 20), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbUserStatus.Items.AddRange(new string[] { "Active", "Inactive" });
                cmbUserStatus.SelectedItem = cmbUserStatus.Items.Contains(oldStatus) ? oldStatus : "Active";

                Button btnDeleteUser = new Button { Text = "Remove User", Location = new Point(20, 280), Size = new Size(100, 30), BackColor = Color.MistyRose, ForeColor = Color.DarkRed };
                Button btnSave = new Button { Text = "Save Changes", Location = new Point(140, 280), Size = new Size(110, 30), DialogResult = DialogResult.OK };
                Button btnCancel = new Button { Text = "Cancel", Location = new Point(270, 280), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };

                btnDeleteUser.Click += (s, ev) =>
                {
                    if (targetUserId == this.currentLoggedInUserId)
                    {
                        MessageBox.Show("Self-termination error.\n\nYou cannot delete your own active administrative profile while logged in.", "Constraint Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DialogResult doubleCheck = MessageBox.Show($"Are you sure you want to remove user '{oldEmail}' from the view grid?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (doubleCheck == DialogResult.Yes)
                    {
                        hiddenUserIds.Add(targetUserId);
                        MessageBox.Show("User profile has been successfully hidden from the interface grid display.", "Removed from View", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        editModal.DialogResult = DialogResult.Cancel;
                        RefreshUserGridData();
                    }
                };

                editModal.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, chkShowPass, lblName, txtName, lblRole, cmbRole, lblStatus, cmbUserStatus, btnDeleteUser, btnSave, btnCancel });
                editModal.AcceptButton = btnSave;

                if (editModal.ShowDialog() == DialogResult.OK)
                {
                    string newUser = txtUser.Text.Trim();
                    string newPass = txtPass.Text.Trim();
                    string newName = txtName.Text.Trim();
                    string newRole = cmbRole.SelectedItem.ToString();
                    string newStatus = cmbUserStatus.SelectedItem.ToString();

                    if (string.IsNullOrWhiteSpace(newUser) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Input verification error. Text parameters cannot be left blank.", "Validation Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                string updateSql = @"
                                    UPDATE Users 
                                    SET Email = @User, PasswordHash = @Pass, FullName = @Name, Role = @Role, Status = @Status 
                                    WHERE UserID = @TargetID;

                                    INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                                    VALUES (@UserID, 'UPDATE', 'Users', @TargetID, @Desc, GETDATE());";

                                using (SqlCommand cmd = new SqlCommand(updateSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@User", newUser);
                                    cmd.Parameters.AddWithValue("@Pass", newPass);
                                    cmd.Parameters.AddWithValue("@Name", newName);
                                    cmd.Parameters.AddWithValue("@Role", newRole);
                                    cmd.Parameters.AddWithValue("@Status", newStatus);
                                    cmd.Parameters.AddWithValue("@TargetID", targetUserId);
                                    cmd.Parameters.AddWithValue("@UserID", this.currentLoggedInUserId);
                                    cmd.Parameters.AddWithValue("@Desc", $"Modified access details for user ID: {targetUserId}");
                                    cmd.ExecuteNonQuery();
                                }
                                trans.Commit();
                                MessageBox.Show("User profile modified cleanly!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                RefreshUserGridData();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Database submission failure: " + ex.Message, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            Form addModal = new Form()
            {
                Width = 400,
                Height = 440,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Register New System User",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblUser = new Label { Text = "Email Address:", Location = new Point(20, 20), Size = new Size(100, 20) };
            TextBox txtUser = new TextBox { Location = new Point(140, 20), Size = new Size(210, 20) };

            Label lblPass = new Label { Text = "Password:", Location = new Point(20, 60), Size = new Size(100, 20) };
            TextBox txtPass = new TextBox { PasswordChar = '*', Location = new Point(140, 60), Size = new Size(210, 20) };

            CheckBox chkShowPass = new CheckBox { Text = "Show Password", Location = new Point(140, 90), Size = new Size(150, 20) };
            chkShowPass.CheckedChanged += (s, ev) => { txtPass.PasswordChar = chkShowPass.Checked ? '\0' : '*'; };

            Label lblName = new Label { Text = "Full Name:", Location = new Point(20, 130), Size = new Size(100, 20) };
            TextBox txtName = new TextBox { Location = new Point(140, 130), Size = new Size(210, 20) };

            Label lblRole = new Label { Text = "System Role:", Location = new Point(20, 170), Size = new Size(100, 20) };
            ComboBox cmbRole = new ComboBox { Location = new Point(140, 170), Size = new Size(210, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new string[] { "Admin", "Staff" });
            cmbRole.SelectedIndex = 1;

            Label lblImage = new Label { Text = "Profile Picture:", Location = new Point(20, 210), Size = new Size(100, 20) };
            Button btnBrowse = new Button { Text = "Browse Image...", Location = new Point(140, 210), Size = new Size(120, 25) };
            Label lblImagePathDisplay = new Label { Text = "No image selected", Location = new Point(140, 240), Size = new Size(210, 35), ForeColor = Color.Gray };

            string selectedImagePath = "";
            btnBrowse.Click += (s, ev) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Image Files(*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        selectedImagePath = ofd.FileName;
                        lblImagePathDisplay.Text = Path.GetFileName(selectedImagePath);
                        lblImagePathDisplay.ForeColor = Color.DarkGreen;
                    }
                }
            };

            Button btnSave = new Button { Text = "Register User", Location = new Point(140, 290), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(250, 290), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };

            addModal.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, chkShowPass, lblName, txtName, lblRole, cmbRole, lblImage, btnBrowse, lblImagePathDisplay, btnSave, btnCancel });
            addModal.AcceptButton = btnSave;

            if (addModal.ShowDialog() == DialogResult.OK)
            {
                string email = txtUser.Text.Trim();
                string password = txtPass.Text.Trim();
                string fullname = txtName.Text.Trim();
                string role = cmbRole.SelectedItem.ToString();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullname))
                {
                    MessageBox.Show("Registration error. Parameters cannot be empty blanks.", "Validation Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            string insertSql = @"
                                INSERT INTO Users (Email, PasswordHash, FullName, Role, Status, CreatedAt)
                                VALUES (@User, @Pass, @Name, @Role, 'Active', GETDATE());
                                
                                INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                                VALUES (@UserID, 'INSERT', 'Users', @@IDENTITY, 'Registered new system user access profile cleanly.', GETDATE());";

                            using (SqlCommand cmd = new SqlCommand(insertSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@User", email);
                                cmd.Parameters.AddWithValue("@Pass", password);
                                cmd.Parameters.AddWithValue("@Name", fullname);
                                cmd.Parameters.AddWithValue("@Role", role);
                                cmd.Parameters.AddWithValue("@UserID", this.currentLoggedInUserId);
                                cmd.ExecuteNonQuery();
                            }
                            trans.Commit();
                            MessageBox.Show("New account profile appended cleanly onto database framework registries!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RefreshUserGridData();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database registry insertion failure: " + ex.Message, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnFormRequiredExit(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void SafelyNavigateToForm(Form targetForm)
        {
            PictureBox pbUser = FindControlRecursive<PictureBox>(this, "pbUserProfilePic");
            if (pbUser == null) pbUser = FindControlRecursive<PictureBox>(this, "pictureBox1");

            if (pbUser != null && pbUser.Image != null)
            {
                pbUser.Image.Dispose();
                pbUser.Image = null;
            }
            this.FormClosed -= OnFormRequiredExit;
            targetForm.FormClosed += OnFormRequiredExit;
            this.Hide();
            targetForm.Show();
            this.Dispose();
        }

        private void LoadUserProfilePicture()
        {
            Label lblHeader = FindControlRecursive<Label>(this, "UserNameHeader");
            if (lblHeader == null) lblHeader = FindControlRecursive<Label>(this, "label1");

            PictureBox pbUser = FindControlRecursive<PictureBox>(this, "pbUserProfilePic");
            if (pbUser == null) pbUser = FindControlRecursive<PictureBox>(this, "pictureBox1");

            string query = "SELECT ImagePath, FullName FROM Users WHERE UserID = @UserID;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (lblHeader != null) lblHeader.Text = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "Staff Member";
                            if (pbUser != null && reader["ImagePath"] != DBNull.Value)
                            {
                                string path = reader["ImagePath"].ToString();
                                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                                {
                                    pbUser.Image?.Dispose();
                                    byte[] bytes = File.ReadAllBytes(path);
                                    using (MemoryStream ms = new MemoryStream(bytes)) { pbUser.Image = Image.FromStream(ms); }
                                    pbUser.SizeMode = PictureBoxSizeMode.Zoom;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void btnHome_Click(object sender, EventArgs e) { SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)); }
        private void btnNewRentalTransaction_Click(object sender, EventArgs e) { SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId)); }
        private void btnCalendar_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId)); }
        private void btnInventoryManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId)); }
        private void btnRecords_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId)); }
        private void btnBookingManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId)); }
        private void btnReturns_Click(object sender, EventArgs e) { SafelyNavigateToForm(new ReturnsCheckIn(this.currentLoggedInUserId)); }
        private void btnReports_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Reports(this.currentLoggedInUserId)); }

        private void btnHome_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)); }
        private void btnNewRentalTransaction_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId)); }
        private void btnCalendar_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId)); }
        private void btnInventoryManagement_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId)); }
        private void btnRecords_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId)); }
        private void btnBookingManagement_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId)); }
        private void btnReturns_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new ReturnsCheckIn(this.currentLoggedInUserId)); }
        private void btnReports_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Reports(this.currentLoggedInUserId)); }
        private void btnReports_Click_2(object sender, EventArgs e) { SafelyNavigateToForm(new Reports(this.currentLoggedInUserId)); }
    }
}
