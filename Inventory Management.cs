using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class Inventory_Management : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private int currentLoggedInUserId;

        public Inventory_Management(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;

            ConfigureInventoryGrid();
            SetupFilterDropdown();
            RefreshInventoryData();
            LoadUserProfilePicture();
        }

        private void Inventory_Management_Load(object sender, EventArgs e)
        {
            ConfigureInventoryGrid();
            SetupFilterDropdown();
            RefreshInventoryData();
            LoadUserProfilePicture();
        }

        private void SetupFilterDropdown()
        {
            if (cmbFilters == null) return;

            cmbFilters.SelectedIndexChanged -= cmbFilters_SelectedIndexChanged;
            cmbFilters.Items.Clear();
            cmbFilters.Items.AddRange(new string[] { "All Statuses", "Available", "Fully Booked", "Maintenance" });
            cmbFilters.SelectedIndex = 0;
            cmbFilters.SelectedIndexChanged += cmbFilters_SelectedIndexChanged;
        }

        private void ConfigureInventoryGrid()
        {
            if (dataGridView1 == null) return;

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Clear();

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ScrollBars = ScrollBars.Both;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ItemID",
                HeaderText = "ID",
                DataPropertyName = "ItemID",
                ReadOnly = true,
                Visible = false
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "Item Name", DataPropertyName = "ItemName", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", DataPropertyName = "Category", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalQuantity", HeaderText = "Total Qty", DataPropertyName = "TotalQuantity", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailableQuantity", HeaderText = "Available for Rent", DataPropertyName = "AvailableQuantity", ReadOnly = true });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaintenanceQuantity", HeaderText = "In Maintenance", DataPropertyName = "MaintenanceQuantity", ReadOnly = true });

            var rateCol = new DataGridViewTextBoxColumn { Name = "DailyRate", HeaderText = "Daily Rate", DataPropertyName = "DailyRate", ReadOnly = true };
            rateCol.DefaultCellStyle.Format = "₱#,##0.00";
            dataGridView1.Columns.Add(rateCol);

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", ReadOnly = true });

            var viewButtonCol = new DataGridViewButtonColumn
            {
                Name = "ViewColumn",
                HeaderText = "Description",
                Text = "View Info",
                UseColumnTextForButtonValue = true
            };
            dataGridView1.Columns.Add(viewButtonCol);

            var actionButtonCol = new DataGridViewButtonColumn
            {
                Name = "ActionColumn",
                HeaderText = "Action",
                Text = "Edit Details",
                UseColumnTextForButtonValue = true
            };
            dataGridView1.Columns.Add(actionButtonCol);

            dataGridView1.CellContentClick -= DataGridView1_CellContentClick;
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
        }


        private void RefreshInventoryData()
        {
            if (dataGridView1 == null) return;

            string filterStatus = cmbFilters != null ? cmbFilters.SelectedItem?.ToString() : "All Statuses";
            string searchKeyword = txtSearch != null ? txtSearch.Text.Trim() : "";

            string query = @"
        SELECT i.ItemID, i.ItemName, i.Category, i.TotalQuantity, i.AvailableQuantity, i.DailyRate, i.Status, i.Description,
               (SELECT ISNULL(SUM(ml.Quantity), 0) 
                FROM MaintenanceLog ml 
                WHERE ml.ItemID = i.ItemID AND ml.Status = 'In Repair') AS MaintenanceQuantity
        FROM Items i 
        WHERE i.Status <> 'Discontinued'";

            if (filterStatus != "All Statuses" && !string.IsNullOrEmpty(filterStatus))
            {
                query += " AND i.Status = @Status";
            }
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query += " AND (i.ItemName LIKE '%' + @Search + '%' OR i.Category LIKE '%' + @Search + '%')";
            }
            query += " ORDER BY i.ItemName ASC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (filterStatus != "All Statuses" && !string.IsNullOrEmpty(filterStatus))
                    {
                        cmd.Parameters.AddWithValue("@Status", filterStatus);
                    }
                    if (!string.IsNullOrEmpty(searchKeyword))
                    {
                        cmd.Parameters.AddWithValue("@Search", searchKeyword);
                    }

                    try
                    {
                        conn.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dataGridView1.DataSource = null;
                        dataGridView1.DataSource = dt;

                        ApplyRowColoringRules();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Failed to load inventory data.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void ApplyRowColoringRules()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Status"].Value == null) continue;
                string status = row.Cells["Status"].Value.ToString();

                if (status == "Maintenance")
                {
                    row.DefaultCellStyle.BackColor = Color.Bisque;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
                else if (status == "Fully Booked")
                {
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }
        }

        private void btnSearchItem_Click(object sender, EventArgs e)
        {
            RefreshInventoryData();
        }

        private void cmbFilters_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshInventoryData();
        }

        private bool IsItemNameDuplicate(string itemName, int excludeItemId = -1)
        {
            string query = "SELECT COUNT(*) FROM Items WHERE ItemName = @ItemName AND ItemID <> @ExcludeItemID AND Status <> 'Discontinued';";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemName", itemName);
                    cmd.Parameters.AddWithValue("@ExcludeItemID", excludeItemId);
                    try
                    {
                        conn.Open();
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        private void LoadUserProfilePicture()
        {
            string query = "SELECT ImagePath, FullName FROM Users WHERE UserID = @UserID;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
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
                                if (UserNameHeader != null)
                                {
                                    UserNameHeader.Text = reader["FullName"] != DBNull.Value
                                        ? reader["FullName"].ToString()
                                        : "Staff Member";
                                }

                                if (pbUserProfilePic != null)
                                {
                                    if (reader["ImagePath"] != DBNull.Value)
                                    {
                                        string path = reader["ImagePath"].ToString();
                                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                                        {
                                            pbUserProfilePic.Image?.Dispose();
                                            byte[] bytes = File.ReadAllBytes(path);
                                            using (MemoryStream ms = new MemoryStream(bytes))
                                            {
                                                pbUserProfilePic.Image = Image.FromStream(ms);
                                            }
                                            pbUserProfilePic.SizeMode = PictureBoxSizeMode.Zoom;
                                        }
                                        else { pbUserProfilePic.Image = null; }
                                    }
                                    else { pbUserProfilePic.Image = null; }
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (pbUserProfilePic != null) pbUserProfilePic.Image = null;
                    }
                }
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId));
        }

        private void btnNewRentalTransaction_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId));
        }

        private void btnCalendar_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId));
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (txtSearch != null) txtSearch.Text = string.Empty;
            if (cmbFilters != null) cmbFilters.SelectedIndex = 0;
            RefreshInventoryData();
        }
        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dataGridView1.Columns["ViewColumn"].Index)
            {
                string itemName = dataGridView1.Rows[e.RowIndex].Cells["ItemName"].Value.ToString();
                string descriptionText = "No description provided for this item.";
                int itemId = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["ItemID"].Value);

                string descQuery = "SELECT Description FROM Items WHERE ItemID = @ItemID;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(descQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemID", itemId);
                    try
                    {
                        conn.Open();
                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value && !string.IsNullOrWhiteSpace(res.ToString()))
                        {
                            descriptionText = res.ToString();
                        }
                    }
                    catch { }
                }

                MessageBox.Show(descriptionText, $"{itemName} - Description Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (e.ColumnIndex == dataGridView1.Columns["ActionColumn"].Index)
            {
                int itemId = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["ItemID"].Value);
                string oldName = dataGridView1.Rows[e.RowIndex].Cells["ItemName"].Value.ToString();
                string oldCategory = dataGridView1.Rows[e.RowIndex].Cells["Category"].Value != DBNull.Value ? dataGridView1.Rows[e.RowIndex].Cells["Category"].Value.ToString() : "";
                int oldTotalQty = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["TotalQuantity"].Value);
                int oldAvailQty = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["AvailableQuantity"].Value);
                string oldStatus = dataGridView1.Rows[e.RowIndex].Cells["Status"].Value.ToString();
                decimal oldRate = Convert.ToDecimal(dataGridView1.Rows[e.RowIndex].Cells["DailyRate"].Value);
                int currentlyInMaintenance = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["MaintenanceQuantity"].Value);

                string oldDescription = "";
                string queryDesc = "SELECT Description FROM Items WHERE ItemID = @ItemID;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(queryDesc, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemID", itemId);
                    try
                    {
                        conn.Open();
                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value) oldDescription = res.ToString();
                    }
                    catch { }
                }

                Form editForm = new Form()
                {
                    Width = 440,
                    Height = 480,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = "Edit Item Details",
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };

                Label lblName = new Label { Text = "Item Name:", Location = new Point(20, 20), Size = new Size(120, 20) };
                TextBox txtName = new TextBox { Text = oldName, Location = new Point(150, 20), Size = new Size(240, 20) };

                Label lblCategory = new Label { Text = "Category:", Location = new Point(20, 60), Size = new Size(120, 20) };
                TextBox txtCategory = new TextBox { Text = oldCategory, Location = new Point(150, 60), Size = new Size(240, 20) };

                Label lblTotal = new Label { Text = "Total Quantity:", Location = new Point(20, 100), Size = new Size(120, 20) };
                NumericUpDown numTotal = new NumericUpDown { Minimum = 0, Maximum = 99999, Value = oldTotalQty, Location = new Point(150, 100), Size = new Size(240, 20) };

                Label lblAvailable = new Label { Text = "Available Items:", Location = new Point(20, 140), Size = new Size(120, 20) };
                NumericUpDown numAvailable = new NumericUpDown { Minimum = 0, Maximum = 99999, Value = oldAvailQty, Location = new Point(150, 140), Size = new Size(240, 20) };

                Label lblRate = new Label { Text = "Daily Rate (₱):", Location = new Point(20, 180), Size = new Size(120, 20) };
                NumericUpDown numRate = new NumericUpDown { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Value = oldRate, Location = new Point(150, 180), Size = new Size(240, 20) };

                Label lblStatus = new Label { Text = "Status:", Location = new Point(20, 220), Size = new Size(120, 20) };
                ComboBox cmbStatus = new ComboBox { Location = new Point(150, 220), Size = new Size(240, 20), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new string[] { "Available", "Fully Booked", "Maintenance" });
                cmbStatus.SelectedItem = cmbStatus.Items.Contains(oldStatus) ? oldStatus : "Available";

                Label lblDescription = new Label { Text = "Description:", Location = new Point(20, 260), Size = new Size(120, 20) };
                TextBox txtDescription = new TextBox { Text = oldDescription, Location = new Point(150, 260), Size = new Size(240, 60), Multiline = true, ScrollBars = ScrollBars.Vertical };

                Button btnSendToMaintenance = new Button
                {
                    Text = "Send to Maintenance",
                    Location = new Point(20, 340),
                    Size = new Size(180, 30),
                    BackColor = Color.Bisque,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    Visible = (oldAvailQty > 0)
                };

                Button btnReturnFromMaintenance = new Button
                {
                    Text = "Return from Maintenance",
                    Location = new Point(210, 340),
                    Size = new Size(180, 30),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    Visible = (currentlyInMaintenance > 0)
                };

                if (oldAvailQty <= 0 && currentlyInMaintenance > 0)
                {
                    btnReturnFromMaintenance.Location = new Point(20, 340);
                    btnReturnFromMaintenance.Size = new Size(370, 30);
                }

                Button btnDelete = new Button { Text = "Delete Item", Location = new Point(20, 390), Size = new Size(100, 30), BackColor = Color.MistyRose, ForeColor = Color.DarkRed };
                Button btnSave = new Button { Text = "Save Changes", Location = new Point(150, 390), Size = new Size(110, 30), DialogResult = DialogResult.OK };
                Button btnCancel = new Button { Text = "Cancel", Location = new Point(280, 390), Size = new Size(110, 30), DialogResult = DialogResult.Cancel };

                btnSendToMaintenance.Click += (s, ev) =>
                {
                    if (oldAvailQty <= 0)
                    {
                        MessageBox.Show("No available items on the floor to send to maintenance.", "Stock Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    Form damagePrompt = new Form()
                    {
                        Width = 350,
                        Height = 180,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        Text = "Send Showroom Items to Repair",
                        StartPosition = FormStartPosition.CenterParent,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.White
                    };

                    Label lblCountPrompt = new Label { Text = "Quantity to pull:", Left = 20, Top = 20, Width = 120 };
                    NumericUpDown numPullQty = new NumericUpDown { Left = 150, Top = 18, Width = 160, Minimum = 1, Maximum = oldAvailQty };

                    Label lblReasonPrompt = new Label { Text = "Damage Notes:", Left = 20, Top = 60, Width = 120 };
                    TextBox txtReason = new TextBox { Left = 150, Top = 58, Width = 160, Text = string.Empty };

                    Button btnConfirmPull = new Button { Text = "Confirm", Left = 130, Top = 100, Width = 90, DialogResult = DialogResult.OK };
                    Button btnAbortPull = new Button { Text = "Cancel", Left = 230, Top = 100, Width = 80, DialogResult = DialogResult.Cancel };

                    damagePrompt.Controls.AddRange(new Control[] { lblCountPrompt, numPullQty, lblReasonPrompt, txtReason, btnConfirmPull, btnAbortPull });
                    damagePrompt.AcceptButton = btnConfirmPull;

                    if (damagePrompt.ShowDialog() == DialogResult.OK)
                    {
                        string noteText = txtReason.Text.Trim();
                        if (string.IsNullOrWhiteSpace(noteText))
                        {
                            MessageBox.Show("Please enter a short description for the damage notes.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int pullAmount = (int)numPullQty.Value;

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();
                                using (SqlTransaction sqlTrans = conn.BeginTransaction())
                                {
                                    string insertLog = @"
                                INSERT INTO MaintenanceLog (ItemID, Quantity, PullDate, DamageNotes, Status)
                                VALUES (@ItemID, @Qty, GETDATE(), @Notes, 'In Repair');";

                                    using (SqlCommand cmdLog = new SqlCommand(insertLog, conn, sqlTrans))
                                    {
                                        cmdLog.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdLog.Parameters.AddWithValue("@Qty", pullAmount);
                                        cmdLog.Parameters.AddWithValue("@Notes", noteText);
                                        cmdLog.ExecuteNonQuery();
                                    }

                                    int newAvail = oldAvailQty - pullAmount;
                                    string computedStatus = newAvail <= 0 ? "Maintenance" : oldStatus;

                                    string updateItemSql = "UPDATE Items SET AvailableQuantity = @NewAvail, Status = @Status WHERE ItemID = @ItemID;";
                                    using (SqlCommand cmdItem = new SqlCommand(updateItemSql, conn, sqlTrans))
                                    {
                                        cmdItem.Parameters.AddWithValue("@NewAvail", newAvail);
                                        cmdItem.Parameters.AddWithValue("@Status", computedStatus);
                                        cmdItem.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdItem.ExecuteNonQuery();
                                    }

                                    sqlTrans.Commit();
                                    MessageBox.Show("Items successfully moved to maintenance.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    damagePrompt.Dispose();
                                    editForm.DialogResult = DialogResult.Cancel;
                                    RefreshInventoryData();
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Failed to pull items into repair: " + ex.Message);
                            }
                        }
                    }
                };
                btnReturnFromMaintenance.Click += (s, ev) =>
                {
                    if (currentlyInMaintenance <= 0)
                    {
                        MessageBox.Show("There are no units recorded in repairs for this item.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    Form returnPrompt = new Form()
                    {
                        Width = 350,
                        Height = 180,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        Text = "Restock Repaired Items",
                        StartPosition = FormStartPosition.CenterParent,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.White
                    };

                    Label lblCountPrompt = new Label { Text = "Quantity to restock:", Left = 20, Top = 20, Width = 120 };
                    NumericUpDown numReturnQty = new NumericUpDown { Left = 150, Top = 18, Width = 160, Minimum = 1, Maximum = currentlyInMaintenance, Value = currentlyInMaintenance };

                    Button btnConfirmReturn = new Button { Text = "Confirm Restock", Left = 110, Top = 90, Width = 110, DialogResult = DialogResult.OK };
                    Button btnAbortReturn = new Button { Text = "Cancel", Left = 230, Top = 90, Width = 80, DialogResult = DialogResult.Cancel };

                    returnPrompt.Controls.AddRange(new Control[] { lblCountPrompt, numReturnQty, btnConfirmReturn, btnAbortReturn });
                    returnPrompt.AcceptButton = btnConfirmReturn;

                    if (returnPrompt.ShowDialog() == DialogResult.OK)
                    {
                        int restockAmount = (int)numReturnQty.Value;

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();
                                using (SqlTransaction sqlTrans = conn.BeginTransaction())
                                {
                                    string restockQuery = @"
                                UPDATE Items 
                                SET AvailableQuantity = AvailableQuantity + @RestockQty,
                                    Status = CASE WHEN Status = 'Maintenance' THEN 'Available' ELSE Status END
                                WHERE ItemID = @ItemID;";

                                    using (SqlCommand cmdItem = new SqlCommand(restockQuery, conn, sqlTrans))
                                    {
                                        cmdItem.Parameters.AddWithValue("@RestockQty", restockAmount);
                                        cmdItem.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdItem.ExecuteNonQuery();
                                    }
                                    string resolveQuery = @"
                                UPDATE TOP (@RestockQty) MaintenanceLog
                                SET Status = 'Repaired', ReturnDate = GETDATE(), ResolutionNotes = 'Fixed and returned onto floor'
                                WHERE ItemID = @ItemID AND Status = 'In Repair';";

                                    using (SqlCommand cmdDetail = new SqlCommand(resolveQuery, conn, sqlTrans))
                                    {
                                        cmdDetail.Parameters.AddWithValue("@RestockQty", restockAmount);
                                        cmdDetail.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdDetail.ExecuteNonQuery();
                                    }

                                    sqlTrans.Commit();
                                    MessageBox.Show("Repaired units restocked to available inventory successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    returnPrompt.Dispose();
                                    editForm.DialogResult = DialogResult.Cancel;
                                    RefreshInventoryData();
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Failed to restock items: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };

                btnDelete.Click += (s, ev) =>
                {
                    int referenceCount = 0;
                    string checkTransactionQuery = "SELECT COUNT(*) FROM RentalDetails WHERE ItemID = @ItemID;";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();
                            using (SqlCommand checkCmd = new SqlCommand(checkTransactionQuery, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@ItemID", itemId);
                                referenceCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not check data records: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    string warningMessage = referenceCount > 0
                        ? $"The item '{oldName}' is currently used in {referenceCount} rental order(s).\n\nYou cannot delete it completely because it will break your system records.\n\nDo you still want to proceed with the deletion process?"
                        : $"Are you sure you want to completely remove '{oldName}' from the system?\n\nThis item is not used in any orders, but this action cannot be undone.\n\nDo you still want to proceed with the deletion?";

                    DialogResult firstCheck = MessageBox.Show(
                        warningMessage,
                        referenceCount > 0 ? "Item cannot be deleted" : "Item can be deleted",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2
                    );

                    if (firstCheck != DialogResult.Yes) return;

                    Form verificationPrompt = new Form()
                    {
                        Width = 400,
                        Height = 160,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        Text = "Final Verification: Step 2 of 2",
                        StartPosition = FormStartPosition.CenterParent,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.White
                    };

                    Label lblText = new Label() { Left = 20, Top = 15, Width = 350, Height = 35, Text = "To confirm deletion, please type out the word 'DELETE' below:" };
                    TextBox txtConfirm = new TextBox() { Left = 20, Top = 55, Width = 340 };
                    Button btnConfirm = new Button() { Text = "Permanently Delete", Left = 210, Width = 150, Top = 95, DialogResult = DialogResult.OK, BackColor = Color.MistyRose };
                    Button btnAbort = new Button() { Text = "Cancel", Left = 120, Width = 80, Top = 95, DialogResult = DialogResult.Cancel };

                    verificationPrompt.Controls.AddRange(new Control[] { lblText, txtConfirm, btnConfirm, btnAbort });
                    verificationPrompt.AcceptButton = btnConfirm;

                    if (verificationPrompt.ShowDialog() == DialogResult.OK)
                    {
                        if (txtConfirm.Text.Trim() != "DELETE")
                        {
                            MessageBox.Show("The typed text did not match 'DELETE'. Action cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();

                                if (referenceCount > 0)
                                {
                                    string softDeletePrompt = "To protect your data records, we will not delete this item.\n\nInstead, would you like to mark it as 'Discontinued'?\n\n• It will change your available stock to 0.\n• It will hide the item so it cannot be rented again.\n• All your old rental records will remain completely safe.\n\nDo you still want to proceed with this change?";

                                    DialogResult softDelete = MessageBox.Show(softDeletePrompt, "Save Records Safe", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    if (softDelete == DialogResult.Yes)
                                    {
                                        string softDeleteQuery = "UPDATE Items SET Status = 'Discontinued', AvailableQuantity = 0 WHERE ItemID = @ItemID;";
                                        using (SqlCommand updateCmd = new SqlCommand(softDeleteQuery, conn))
                                        {
                                            updateCmd.Parameters.AddWithValue("@ItemID", itemId);
                                            updateCmd.ExecuteNonQuery();
                                        }
                                        MessageBox.Show("The item has been changed to Discontinued.", "Status Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        editForm.DialogResult = DialogResult.Cancel;
                                        RefreshInventoryData();
                                    }
                                    return;
                                }

                                string deleteQuery = "DELETE FROM Items WHERE ItemID = @ItemID;";
                                using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
                                {
                                    deleteCmd.Parameters.AddWithValue("@ItemID", itemId);
                                    deleteCmd.ExecuteNonQuery();
                                }

                                MessageBox.Show("Item deleted successfully from records.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                editForm.DialogResult = DialogResult.Cancel;
                                RefreshInventoryData();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Could not complete delete action: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };

                editForm.Controls.AddRange(new Control[] { lblName, txtName, lblCategory, txtCategory, lblTotal, numTotal, lblAvailable, numAvailable, lblRate, numRate, lblStatus, cmbStatus, lblDescription, txtDescription, btnSendToMaintenance, btnReturnFromMaintenance, btnDelete, btnSave, btnCancel });
                editForm.AcceptButton = btnSave;

                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    string newName = txtName.Text.Trim();
                    string newCategory = txtCategory.Text.Trim();
                    int finalTotalQty = (int)numTotal.Value;
                    int finalAvailQty = (int)numAvailable.Value;
                    decimal newRate = numRate.Value;
                    string finalStatus = cmbStatus.SelectedItem.ToString();
                    string newDescription = txtDescription.Text.Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Item name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (IsItemNameDuplicate(newName, itemId))
                    {
                        MessageBox.Show("This item name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (finalAvailQty > finalTotalQty)
                    {
                        MessageBox.Show("Available quantity cannot exceed total quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int currentlyRentedOutOrReserved = 0;
                    string checkActiveRentalsQuery = @"
                SELECT ISNULL(SUM(rd.Quantity), 0) 
                FROM RentalDetails rd
                INNER JOIN RentalTransactions t ON rd.TransactionID = t.TransactionID
                WHERE rd.ItemID = @ItemID AND t.Status IN ('Pending', 'Ongoing', 'Overdue');";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();
                            using (SqlCommand activeCmd = new SqlCommand(checkActiveRentalsQuery, conn))
                            {
                                activeCmd.Parameters.AddWithValue("@ItemID", itemId);
                                currentlyRentedOutOrReserved = Convert.ToInt32(activeCmd.ExecuteScalar());
                            }
                        }
                        catch
                        {
                            return;
                        }
                    }

                    if ((finalTotalQty - finalAvailQty) < currentlyRentedOutOrReserved)
                    {
                        MessageBox.Show($"Cannot modify counts. There are currently {currentlyRentedOutOrReserved} units of this item out on active rentals or bookings.", "Inventory Conflict", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                    if (finalStatus != "Maintenance")
                    {
                        finalStatus = finalAvailQty <= 0 ? "Fully Booked" : "Available";
                    }

                    string updateQuery = @"
                UPDATE Items 
                SET ItemName = @ItemName, 
                    Category = @Category,
                    DailyRate = @DailyRate, 
                    TotalQuantity = @TotalQty, 
                    AvailableQuantity = @AvailQty,
                    Status = @Status,
                    Description = @Description
                WHERE ItemID = @ItemID;";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@ItemName", newName);
                            cmd.Parameters.AddWithValue("@Category", string.IsNullOrEmpty(newCategory) ? DBNull.Value : (object)newCategory);
                            cmd.Parameters.AddWithValue("@DailyRate", newRate);
                            cmd.Parameters.AddWithValue("@TotalQty", finalTotalQty);
                            cmd.Parameters.AddWithValue("@AvailQty", finalAvailQty);
                            cmd.Parameters.AddWithValue("@Status", finalStatus);
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(newDescription) ? DBNull.Value : (object)newDescription);
                            cmd.Parameters.AddWithValue("@ItemID", itemId);

                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                MessageBox.Show("Item updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                RefreshInventoryData();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Failed to execute update sequence: " + ex.Message, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }




        private void btnAddNewItem_Click(object sender, EventArgs e)
        {
            Form addForm = new Form()
            {
                Width = 440,
                Height = 380,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Add New Inventory Item",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblName = new Label { Text = "Item Name:", Location = new Point(20, 20), Size = new Size(120, 20) };
            TextBox txtName = new TextBox { Location = new Point(150, 20), Size = new Size(240, 20) };

            Label lblCategory = new Label { Text = "Category:", Location = new Point(20, 60), Size = new Size(120, 20) };
            TextBox txtCategory = new TextBox { Location = new Point(150, 60), Size = new Size(240, 20) };

            Label lblTotal = new Label { Text = "Total Quantity:", Location = new Point(20, 100), Size = new Size(120, 20) };
            NumericUpDown numTotal = new NumericUpDown { Minimum = 1, Maximum = 99999, Value = 1, Location = new Point(150, 100), Size = new Size(240, 20) };

            Label lblRate = new Label { Text = "Daily Rate (₱):", Location = new Point(20, 140), Size = new Size(120, 20) };
            NumericUpDown numRate = new NumericUpDown { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Value = 0, Location = new Point(150, 140), Size = new Size(240, 20) };

            Label lblStatus = new Label { Text = "Initial Status:", Location = new Point(20, 180), Size = new Size(120, 20) };
            ComboBox cmbStatus = new ComboBox { Location = new Point(150, 180), Size = new Size(240, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new string[] { "Available", "Maintenance" });
            cmbStatus.SelectedIndex = 0;

            Label lblDescription = new Label { Text = "Description:", Location = new Point(20, 220), Size = new Size(120, 20) };
            TextBox txtDescription = new TextBox { Location = new Point(150, 220), Size = new Size(240, 60), Multiline = true, ScrollBars = ScrollBars.Vertical };

            Button btnSave = new Button { Text = "Add Item", Location = new Point(150, 300), Size = new Size(110, 30), DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(280, 300), Size = new Size(110, 30), DialogResult = DialogResult.Cancel };

            addForm.Controls.AddRange(new Control[] { lblName, txtName, lblCategory, txtCategory, lblTotal, numTotal, lblRate, numRate, lblStatus, cmbStatus, lblDescription, txtDescription, btnSave, btnCancel });
            addForm.AcceptButton = btnSave;

            if (addForm.ShowDialog() == DialogResult.OK)
            {
                string itemName = txtName.Text.Trim();
                string categoryName = txtCategory.Text.Trim();
                int quantity = (int)numTotal.Value;
                decimal dailyRate = numRate.Value;
                string initialStatus = cmbStatus.SelectedItem.ToString();
                string descriptionName = txtDescription.Text.Trim();

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    MessageBox.Show("Please enter an item name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (itemName.Length > 100)
                {
                    MessageBox.Show("Item name is too long (max 100 characters).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (IsItemNameDuplicate(itemName))
                {
                    MessageBox.Show("This item name already exists in inventory.", "Duplicate Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string insertQuery = @"
                    INSERT INTO Items (ItemName, Category, TotalQuantity, AvailableQuantity, DailyRate, Status, Description, LastUpdated)
                    VALUES (@ItemName, @Category, @Qty, @Qty, @Rate, @Status, @Description, GETDATE());";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ItemName", itemName);
                        cmd.Parameters.AddWithValue("@Category", string.IsNullOrEmpty(categoryName) ? DBNull.Value : (object)categoryName);
                        cmd.Parameters.AddWithValue("@Qty", quantity);
                        cmd.Parameters.AddWithValue("@Rate", dailyRate);
                        cmd.Parameters.AddWithValue("@Status", initialStatus);
                        cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(descriptionName) ? DBNull.Value : (object)descriptionName);

                        try
                        {
                            conn.Open();
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Item added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RefreshInventoryData();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed to save item line: " + ex.Message, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void btnRecords_Click(object sender, EventArgs e)
        {
            if (pbUserProfilePic != null && pbUserProfilePic.Image != null)
            {
                pbUserProfilePic.Image.Dispose();
                pbUserProfilePic.Image = null;
            }
            this.FormClosed -= (s, a) => Application.Exit();
            Customer_Records recordsForm = new Customer_Records(this.currentLoggedInUserId);
            recordsForm.FormClosed += (s, a) => Application.Exit();
            this.Hide();
            recordsForm.Show();
            this.Dispose();
        }

        private void btnBookingManagement_Click(object sender, EventArgs e)
        {
            if (pbUserProfilePic != null && pbUserProfilePic.Image != null)
            {
                pbUserProfilePic.Image.Dispose();
                pbUserProfilePic.Image = null;
            }
            this.FormClosed -= (s, a) => Application.Exit();
            Booking_Management bookingForm = new Booking_Management(this.currentLoggedInUserId);
            bookingForm.FormClosed += (s, a) => Application.Exit();
            this.Hide();
            bookingForm.Show();
            this.Dispose();
        }

        private void SafelyNavigateToForm(Form targetForm)
        {
            if (pbUserProfilePic != null && pbUserProfilePic.Image != null)
            {
                pbUserProfilePic.Image.Dispose();
                pbUserProfilePic.Image = null;
            }
            this.FormClosed -= (s, a) => Application.Exit();
            targetForm.FormClosed += (s, a) => Application.Exit();
            this.Hide();
            targetForm.Show();
            this.Dispose();
        }

        private void btnReturnsCheckIn_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new ReturnsCheckIn(this.currentLoggedInUserId));
        }

        private void RestrictAdminControlsByRole()
        {
            if (btnUserManagement == null) return;

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
                        string role = res.ToString().Trim();
                        btnUserManagement.Visible = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        btnUserManagement.Visible = false;
                    }
                }
                catch
                {
                    btnUserManagement.Visible = false;
                }
            }
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new UserManagement(this.currentLoggedInUserId));
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new Reports(this.currentLoggedInUserId));
        }
    }
}
