using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class ReturnsCheckIn : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private int currentLoggedInUserId;

        private int activeTransactionId = -1;
        private int activeItemId = -1;
        private int activeRentedQuantity = 0;
        private decimal activeItemDailyRate = 0;
        private decimal activeOriginalDeposit = 0;
        private decimal activeTransactionTotalAmount = 0;
        private DateTime activeRentalStartDate = DateTime.Now;
        private DateTime activeExpectedReturnDate = DateTime.Now;

        public ReturnsCheckIn()
        {
            InitializeComponent();
            this.currentLoggedInUserId = 1;
            SetupInitialFormStates();
            LoadUserProfilePicture();
        }

        public ReturnsCheckIn(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            SetupInitialFormStates();
            LoadUserProfilePicture();
        }

        private void SetupInitialFormStates()
        {
            if (txtCustomer != null) txtCustomer.ReadOnly = true;
            if (txtItem != null) txtItem.ReadOnly = true;
            if (txtExpectedReturn != null) txtExpectedReturn.ReadOnly = true;
            if (txtDepositRefund != null) txtDepositRefund.ReadOnly = true;
            if (txtOverdueFine != null) txtOverdueFine.ReadOnly = true;

            if (numDamagedQuantity != null)
            {
                numDamagedQuantity.Minimum = 0;
                numDamagedQuantity.Maximum = 999;
                numDamagedQuantity.Value = 0;
                numDamagedQuantity.Enabled = true;
                numDamagedQuantity.ValueChanged += ItemQuantityCounters_ValueChanged;
            }

            if (numBrokenQuantity != null)
            {
                numBrokenQuantity.Minimum = 0;
                numBrokenQuantity.Maximum = 999;
                numBrokenQuantity.Value = 0;
                numBrokenQuantity.Enabled = true;
                numBrokenQuantity.ValueChanged += ItemQuantityCounters_ValueChanged;
            }

            if (txtDamageNotes != null) txtDamageNotes.Enabled = false;

            dtpActualReturn.Value = DateTime.Now;
            dtpActualReturn.ValueChanged += (s, e) => CalculateRefundAndFines();
        }

        private void ItemQuantityCounters_ValueChanged(object sender, EventArgs e)
        {
            int damagedQty = numDamagedQuantity != null ? (int)numDamagedQuantity.Value : 0;
            int brokenQty = numBrokenQuantity != null ? (int)numBrokenQuantity.Value : 0;

            if (this.activeTransactionId > 0 && (damagedQty + brokenQty) > this.activeRentedQuantity)
            {
                NumericUpDown currentControl = (NumericUpDown)sender;
                MessageBox.Show("Total damaged and broken units cannot exceed the rented quantity.", "Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                currentControl.Value -= 1;
                return;
            }

            if (txtDamageNotes != null)
            {
                bool hasDamageOrBreakage = (damagedQty > 0 || brokenQty > 0);
                txtDamageNotes.Enabled = hasDamageOrBreakage;
                if (!hasDamageOrBreakage) txtDamageNotes.Text = string.Empty;
            }

            CalculateRefundAndFines();
        }
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearchCriteria.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show("Please enter a customer name or transaction ID to search.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form searchModal = new Form();
            ListBox lstResults = new ListBox();
            Button btnSelect = new Button();
            Button btnCancelClose = new Button();
            DataTable resultsTable = new DataTable();

            searchModal.Text = "Select Active Rental Transaction";
            searchModal.Size = new Size(500, 350);
            searchModal.FormBorderStyle = FormBorderStyle.FixedDialog;
            searchModal.StartPosition = FormStartPosition.CenterParent;
            searchModal.MaximizeBox = false;
            searchModal.MinimizeBox = false;

            lstResults.Location = new Point(15, 15);
            lstResults.Size = new Size(455, 220);

            btnSelect.Text = "Select";
            btnSelect.DialogResult = DialogResult.OK;
            btnSelect.Location = new Point(310, 260);
            btnSelect.Size = new Size(75, 30);

            btnCancelClose.Text = "Cancel";
            btnCancelClose.DialogResult = DialogResult.Cancel;
            btnCancelClose.Location = new Point(395, 260);
            btnCancelClose.Size = new Size(75, 30);

            searchModal.Controls.AddRange(new Control[] { lstResults, btnSelect, btnCancelClose });
            searchModal.AcceptButton = btnSelect;
            searchModal.CancelButton = btnCancelClose;

            string searchQuery = @"
    SELECT rt.TransactionID AS TxID, rd.ItemID, rd.Quantity, i.DailyRate, rt.DepositAmount AS Deposit, 
           rt.TotalAmount, rt.RentalStartDate AS StartDate, rt.ExpectedReturnDate AS ExpectedReturn, 
           c.Name AS CustName, i.ItemName
    FROM RentalTransactions rt
    INNER JOIN Customers c ON rt.CustomerID = c.CustomerID
    INNER JOIN RentalDetails rd ON rt.TransactionID = rd.TransactionID
    INNER JOIN Items i ON rd.ItemID = i.ItemID
    WHERE rt.Status IN ('Ongoing', 'Confirmed') AND (c.Name LIKE @Keyword OR CAST(rt.TransactionID AS VARCHAR) = @Keyword);";


            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(searchQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    try
                    {
                        conn.Open();
                        adapter.Fill(resultsTable);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Search query failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            if (resultsTable.Rows.Count == 0)
            {
                MessageBox.Show("No ongoing rental transactions found matching your criteria.", "No Records", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            resultsTable.Columns.Add("DisplayString", typeof(string));
            foreach (DataRow row in resultsTable.Rows)
            {
                row["DisplayString"] = $"ID: {row["TxID"]} | {row["CustName"]} | {row["ItemName"]} ({row["Quantity"]}x)";
            }

            lstResults.DataSource = resultsTable;
            lstResults.DisplayMember = "DisplayString";

            if (searchModal.ShowDialog() == DialogResult.OK && lstResults.SelectedIndex >= 0)
            {
                DataRowView selectedRow = (DataRowView)lstResults.SelectedItem;

                this.activeTransactionId = Convert.ToInt32(selectedRow["TxID"]);
                this.activeItemId = Convert.ToInt32(selectedRow["ItemID"]);
                this.activeRentedQuantity = Convert.ToInt32(selectedRow["Quantity"]);
                this.activeItemDailyRate = Convert.ToDecimal(selectedRow["DailyRate"]);
                this.activeOriginalDeposit = Convert.ToDecimal(selectedRow["Deposit"]);
                this.activeTransactionTotalAmount = Convert.ToDecimal(selectedRow["TotalAmount"]);
                this.activeRentalStartDate = Convert.ToDateTime(selectedRow["StartDate"]);
                this.activeExpectedReturnDate = Convert.ToDateTime(selectedRow["ExpectedReturn"]);

                txtCustomer.Text = selectedRow["CustName"].ToString();
                txtItem.Text = $"{selectedRow["ItemName"]} ({this.activeRentedQuantity}x)";
                txtExpectedReturn.Text = this.activeExpectedReturnDate.ToString("MM/dd/yyyy hh:mm tt");

                if (numDamagedQuantity != null)
                {
                    numDamagedQuantity.Maximum = this.activeRentedQuantity;
                    numDamagedQuantity.Value = 0;
                }
                if (numBrokenQuantity != null)
                {
                    numBrokenQuantity.Maximum = this.activeRentedQuantity;
                    numBrokenQuantity.Value = 0;
                }

                CalculateRefundAndFines();
            }

            searchModal.Dispose();
        }
        private void CalculateRefundAndFines()
        {
            if (this.activeTransactionId <= 0) return;

            decimal calculatedRefund = this.activeOriginalDeposit;
            decimal overdueFine = 0;

            DateTime actualDate = dtpActualReturn.Value.Date;
            DateTime expectedDate = this.activeExpectedReturnDate.Date;

            if (actualDate > expectedDate)
            {
                int overdueDays = (int)(actualDate - expectedDate).TotalDays;
                overdueFine = this.activeItemDailyRate * this.activeRentedQuantity * overdueDays;
                if (txtOverdueFine != null) txtOverdueFine.Text = overdueFine.ToString("F2");
            }
            else if (txtOverdueFine != null)
            {
                txtOverdueFine.Text = "0.00";
            }

            if (actualDate < expectedDate)
            {
                int daysEarlier = (int)(expectedDate - actualDate).TotalDays;
                decimal earlyCredit = this.activeItemDailyRate * this.activeRentedQuantity * daysEarlier;
                calculatedRefund += earlyCredit;
            }

            if (calculatedRefund < 0) calculatedRefund = 0;

            if (txtDepositRefund != null)
            {
                txtDepositRefund.Text = calculatedRefund.ToString("F2");
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            if (this.activeTransactionId <= 0)
            {
                MessageBox.Show("Please search and select a rental transaction first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string notesText = txtDamageNotes != null ? txtDamageNotes.Text.Trim() : string.Empty;
            int damagedQty = numDamagedQuantity != null ? (int)numDamagedQuantity.Value : 0;
            int brokenQty = numBrokenQuantity != null ? (int)numBrokenQuantity.Value : 0;
            int goodQty = this.activeRentedQuantity - damagedQty - brokenQty;

            string conditionSummary = "Good";
            if (brokenQty > 0) conditionSummary = "Broken";
            else if (damagedQty > 0) conditionSummary = "Damaged";

            if (goodQty < 0)
            {
                MessageBox.Show("Total damaged + broken quantity cannot exceed rented quantity.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (conditionSummary != "Good" && string.IsNullOrWhiteSpace(notesText))
            {
                MessageBox.Show("Please provide damage notes explaining the item's condition.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal finalRefundValue = decimal.TryParse(txtDepositRefund?.Text, out decimal val) ? val : 0;
            decimal overdueFine = decimal.TryParse(txtOverdueFine?.Text ?? "0", out decimal f) ? f : 0;

            DateTime actualDate = dtpActualReturn.Value.Date;
            decimal finalTotalAmount = this.activeTransactionTotalAmount;

            if (actualDate < this.activeExpectedReturnDate.Date)
            {
                int daysUsed = (int)(actualDate - this.activeRentalStartDate.Date).TotalDays;
                if (daysUsed <= 0) daysUsed = 1;
                finalTotalAmount = this.activeItemDailyRate * this.activeRentedQuantity * daysUsed;
            }
            else if (actualDate > this.activeExpectedReturnDate.Date)
            {
                finalTotalAmount += overdueFine;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        string updateTxSql = @"
                            UPDATE RentalTransactions 
                            SET Status = 'Completed', 
                                ActualReturnDate = @ActualReturn, 
                                TotalAmount = @NewTotal,
                                Notes = @Notes 
                            WHERE TransactionID = @TxID;";

                        using (SqlCommand cmd = new SqlCommand(updateTxSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                            cmd.Parameters.AddWithValue("@ActualReturn", dtpActualReturn.Value);
                            cmd.Parameters.AddWithValue("@NewTotal", finalTotalAmount);
                            cmd.Parameters.AddWithValue("@Notes", notesText);
                            cmd.ExecuteNonQuery();
                        }

                        string updateDetailSql = @"
                            UPDATE RentalDetails
                            SET ConditionAfter = @ConditionAfter,
                                DamageNotes = @DamageNotes
                            WHERE TransactionID = @TxID AND ItemID = @ItemID;";

                        using (SqlCommand cmd = new SqlCommand(updateDetailSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                            cmd.Parameters.AddWithValue("@ItemID", this.activeItemId);
                            cmd.Parameters.AddWithValue("@ConditionAfter", conditionSummary);
                            cmd.Parameters.AddWithValue("@DamageNotes", string.IsNullOrWhiteSpace(notesText) ? (object)DBNull.Value : notesText);
                            cmd.ExecuteNonQuery();
                        }

                        if (goodQty > 0)
                        {
                            string stockSql = @"
                                UPDATE Items 
                                SET AvailableQuantity = AvailableQuantity + @Qty,
                                    Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END
                                WHERE ItemID = @ItemID;";
                            using (SqlCommand cmd = new SqlCommand(stockSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Qty", goodQty);
                                cmd.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (damagedQty > 0)
                        {
                            string maintLogSql = @"
                                INSERT INTO MaintenanceLog (ItemID, Quantity, PullDate, DamageNotes, Status)
                                VALUES (@ItemID, @Qty, GETDATE(), @Notes, 'In Repair');";

                            using (SqlCommand cmd = new SqlCommand(maintLogSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                cmd.Parameters.AddWithValue("@Qty", damagedQty);
                                cmd.Parameters.AddWithValue("@Notes", notesText);
                                cmd.ExecuteNonQuery();
                            }

                            string itemMaintSql = "UPDATE Items SET Status = 'Maintenance' WHERE ItemID = @ItemID;";
                            using (SqlCommand cmd = new SqlCommand(itemMaintSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (brokenQty > 0)
                        {
                            string breakSql = @"
                                UPDATE Items 
                                SET TotalQuantity = TotalQuantity - @Qty
                                WHERE ItemID = @ItemID;";
                            using (SqlCommand cmd = new SqlCommand(breakSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Qty", brokenQty);
                                cmd.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (overdueFine > 0)
                        {
                            string paymentSql = @"
                                INSERT INTO Payments (TransactionID, Amount, PaymentDate, Method, Notes)
                                VALUES (@TxID, @Amount, GETDATE(), 'Damage Charge', @Notes);";

                            using (SqlCommand cmd = new SqlCommand(paymentSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                                cmd.Parameters.AddWithValue("@Amount", overdueFine);
                                cmd.Parameters.AddWithValue("@Notes", $"Overdue Fine. Condition: {conditionSummary}");
                                cmd.ExecuteNonQuery();
                            }
                        }
                        if (finalRefundValue > 0)
                        {
                            string paymentSql = @"
                                INSERT INTO Payments (TransactionID, Amount, PaymentDate, Method, Notes)
                                VALUES (@TxID, @Amount, GETDATE(), 'Deposit Refund', @Notes);";

                            using (SqlCommand cmd = new SqlCommand(paymentSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                                cmd.Parameters.AddWithValue("@Amount", finalRefundValue);
                                cmd.Parameters.AddWithValue("@Notes", $"Deposit Refunded. Condition: {conditionSummary}");
                                cmd.ExecuteNonQuery();
                            }
                        }

                        string logSql = @"
                            INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                            VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, @Desc, GETDATE());";

                        using (SqlCommand cmdLog = new SqlCommand(logSql, conn, trans))
                        {
                            cmdLog.Parameters.AddWithValue("@UserID", this.currentLoggedInUserId);
                            cmdLog.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                            string actionString = $"Return processed - Good: {goodQty}, Damaged: {damagedQty}, Broken: {brokenQty}, Overdue Fine: ₱{overdueFine}";
                            cmdLog.Parameters.AddWithValue("@Desc", actionString);
                            cmdLog.ExecuteNonQuery();
                        }

                        trans.Commit();
                        MessageBox.Show("Return processed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ResetFormInputs();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save check-in transaction: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ResetFormInputs()
        {
            this.activeTransactionId = -1;
            this.activeItemId = -1;
            this.activeRentedQuantity = 0;
            this.activeItemDailyRate = 0;
            this.activeOriginalDeposit = 0;
            this.activeTransactionTotalAmount = 0;
            this.activeRentalStartDate = DateTime.Now;
            this.activeExpectedReturnDate = DateTime.Now;

            if (txtSearchCriteria != null) txtSearchCriteria.Clear();
            if (txtCustomer != null) txtCustomer.Clear();
            if (txtItem != null) txtItem.Clear();
            if (txtExpectedReturn != null) txtExpectedReturn.Clear();
            if (txtDamageNotes != null) txtDamageNotes.Clear();
            if (txtDepositRefund != null) txtDepositRefund.Text = "0.00";
            if (txtOverdueFine != null) txtOverdueFine.Text = "0.00";

            if (numDamagedQuantity != null)
            {
                numDamagedQuantity.Value = 0;
            }
            if (numBrokenQuantity != null)
            {
                numBrokenQuantity.Value = 0;
            }
            dtpActualReturn.Value = DateTime.Now;
        }

        private void LoadUserProfilePicture()
        {
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
                            if (UserNameHeader != null) UserNameHeader.Text = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "Staff Member";
                            if (pbUserProfilePic != null && reader["ImagePath"] != DBNull.Value)
                            {
                                string path = reader["ImagePath"].ToString();
                                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                                {
                                    pbUserProfilePic.Image?.Dispose();
                                    byte[] bytes = File.ReadAllBytes(path);
                                    using (MemoryStream ms = new MemoryStream(bytes)) { pbUserProfilePic.Image = Image.FromStream(ms); }
                                    pbUserProfilePic.SizeMode = PictureBoxSizeMode.Zoom;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId));
        }

        private void SafelyNavigateToForm(Form targetForm)
        {
            if (pbUserProfilePic != null && pbUserProfilePic.Image != null)
            {
                pbUserProfilePic.Image.Dispose();
                pbUserProfilePic.Image = null;
            }
            this.FormClosed -= OnFormRequiredExit;
            targetForm.FormClosed += OnFormRequiredExit;
            this.Hide();
            targetForm.Show();
            this.Dispose();
        }

        private void OnFormRequiredExit(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void btnHome_Click(object sender, EventArgs e) { SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)); }
        private void btnNewRentalTransaction_Click(object sender, EventArgs e) { SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId)); }
        private void btnCalendar_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId)); }
        private void btnInventoryManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId)); }
        private void btnRecords_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId)); }
        private void btnBookingManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId)); }

        private void btnHome_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)); }
        private void btnNewRentalTransaction_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId)); }
        private void btnCalendar_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId)); }
        private void btnInventoryManagement_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId)); }
        private void btnRecords_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId)); }
        private void btnBookingManagement_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId)); }

        private void btnUserManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new UserManagement(this.currentLoggedInUserId)); }
        private void btnReports_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Reports(this.currentLoggedInUserId)); }
    }
}
