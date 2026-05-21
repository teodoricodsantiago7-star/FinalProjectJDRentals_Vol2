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

            if (cmbConditionAfter != null)
            {
                cmbConditionAfter.Items.Clear();
                cmbConditionAfter.Items.AddRange(new string[] { "Good", "Damaged", "Broken" });
                cmbConditionAfter.SelectedIndex = 0;

                cmbConditionAfter.SelectedIndexChanged -= CmbConditionAfter_SelectedIndexChanged;
                cmbConditionAfter.SelectedIndexChanged += CmbConditionAfter_SelectedIndexChanged;
            }

            if (txtDamageNotes != null) txtDamageNotes.Enabled = false;

            dtpActualReturn.Value = DateTime.Now;

            dtpActualReturn.ValueChanged -= (s, e) => CalculateRefundAndFines();
            dtpActualReturn.ValueChanged += (s, e) => CalculateRefundAndFines();
        }

        private void CmbConditionAfter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbConditionAfter.SelectedItem.ToString() == "Good")
            {
                if (txtDamageNotes != null)
                {
                    txtDamageNotes.Enabled = false;
                    txtDamageNotes.Text = string.Empty;
                }
            }
            else
            {
                if (txtDamageNotes != null) txtDamageNotes.Enabled = true;
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

            Form searchModal = new Form()
            {
                Width = 500,
                Height = 320,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Select Active Rental Record",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            ListBox lstResults = new ListBox { Location = new Point(20, 20), Size = new Size(440, 200) };
            Button btnPick = new Button { Text = "Select Record", Location = new Point(240, 235), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            Button btnClose = new Button { Text = "Cancel", Location = new Point(350, 235), Size = new Size(110, 30), DialogResult = DialogResult.Cancel };

            searchModal.Controls.AddRange(new Control[] { lstResults, btnPick, btnClose });
            searchModal.AcceptButton = btnPick;

            string query = @"
                SELECT t.TransactionID, c.Name AS CustomerName, i.ItemName, rd.Quantity, t.RentalStartDate, t.ExpectedReturnDate, i.DailyRate, t.DepositAmount, t.TotalAmount, i.ItemID
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE t.Status IN ('Ongoing', 'Overdue')
                  AND (c.Name LIKE '%' + @Keyword + '%' OR CAST(t.TransactionID AS VARCHAR) = @Keyword)
                ORDER BY t.TransactionID DESC;";

            DataTable displayTable = new DataTable();
            displayTable.Columns.Add("TxID", typeof(int));
            displayTable.Columns.Add("Display", typeof(string));
            displayTable.Columns.Add("ItemID", typeof(int));
            displayTable.Columns.Add("Quantity", typeof(int));
            displayTable.Columns.Add("DailyRate", typeof(decimal));
            displayTable.Columns.Add("Deposit", typeof(decimal));
            displayTable.Columns.Add("TotalAmount", typeof(decimal));
            displayTable.Columns.Add("CustName", typeof(string));
            displayTable.Columns.Add("ItemName", typeof(string));
            displayTable.Columns.Add("StartDate", typeof(DateTime));
            displayTable.Columns.Add("ExpectedReturn", typeof(DateTime));

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Keyword", keyword);
                try
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int tid = Convert.ToInt32(r["TransactionID"]);
                            string cname = r["CustomerName"].ToString();
                            string iname = r["ItemName"].ToString();
                            int qty = Convert.ToInt32(r["Quantity"]);
                            DateTime start = Convert.ToDateTime(r["RentalStartDate"]);
                            DateTime expected = Convert.ToDateTime(r["ExpectedReturnDate"]);

                            string rowString = $"Tx {tid}: {cname} - {iname} ({qty}x) | Return: {expected:MM/dd/yyyy}";
                            displayTable.Rows.Add(tid, rowString, r["ItemID"], qty, r["DailyRate"], r["DepositAmount"], r["TotalAmount"], cname, iname, start, expected);
                        }
                    }
                }
                catch { }
            }

            lstResults.DataSource = displayTable;
            lstResults.DisplayMember = "Display";
            lstResults.ValueMember = "TxID";

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

                CalculateRefundAndFines();
            }
        }

        private void CalculateRefundAndFines()
        {
            if (this.activeTransactionId <= 0) return;

            decimal calculatedRefund = this.activeOriginalDeposit;
            string condition = cmbConditionAfter?.SelectedItem?.ToString() ?? "Good";

            if (condition == "Damaged")
            {
                calculatedRefund = this.activeOriginalDeposit * 0.50m;
            }
            else if (condition == "Broken")
            {
                calculatedRefund = 0.00m;
            }

            DateTime actualDate = dtpActualReturn.Value.Date;
            DateTime startDate = this.activeRentalStartDate.Date;
            DateTime expectedDate = this.activeExpectedReturnDate.Date;

            if (actualDate < startDate)
            {
                calculatedRefund = this.activeTransactionTotalAmount + this.activeOriginalDeposit;
                if (txtDamageNotes != null)
                {
                    txtDamageNotes.Text = string.Empty;
                }
            }
            else if (actualDate > expectedDate)
            {
                int overdueDays = (int)(actualDate - expectedDate).TotalDays;
                if (overdueDays <= 0) overdueDays = 1;

                decimal fineAmount = this.activeItemDailyRate * this.activeRentedQuantity * overdueDays;
                calculatedRefund -= fineAmount;
            }
            else if (actualDate < expectedDate)
            {
                int daysEarlier = (int)(expectedDate - actualDate).TotalDays;

                if (daysEarlier > 0)
                {
                    decimal netEarlyReturnCredit = this.activeItemDailyRate * this.activeRentedQuantity * daysEarlier;
                    calculatedRefund += netEarlyReturnCredit;
                }
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

            string condition = cmbConditionAfter.SelectedItem.ToString();
            string notesText = txtDamageNotes.Text.Trim();
            decimal finalRefundValue = decimal.TryParse(txtDepositRefund.Text, out decimal val) ? val : 0;

            DateTime actualDate = dtpActualReturn.Value.Date;
            DateTime startDate = this.activeRentalStartDate.Date;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            if (actualDate < startDate)
                            {
                                string cancelTxSql = @"
                                    UPDATE RentalTransactions 
                                    SET Status = 'Cancelled', ActualReturnDate = @ActualReturn, Notes = @Notes, TotalAmount = 0
                                    WHERE TransactionID = @TxID;

                                    UPDATE Items 
                                    SET AvailableQuantity = AvailableQuantity + @Qty,
                                        Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END
                                    WHERE ItemID = @ItemID;";

                                using (SqlCommand cmdCancel = new SqlCommand(cancelTxSql, conn, trans))
                                {
                                    cmdCancel.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                                    cmdCancel.Parameters.AddWithValue("@ActualReturn", dtpActualReturn.Value);
                                    cmdCancel.Parameters.AddWithValue("@Notes", "Cancelled via Early Return prior to Start: " + notesText);
                                    cmdCancel.Parameters.AddWithValue("@Qty", this.activeRentedQuantity);
                                    cmdCancel.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                    cmdCancel.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string completeTxSql = @"
                                    DECLARE @OriginalStart DATETIME, @ExpectedEnd DATETIME, @DailyRate DECIMAL(10,2), @Qty INT;
                                    
                                    SELECT @OriginalStart = t.RentalStartDate, @ExpectedEnd = t.ExpectedReturnDate, @DailyRate = i.DailyRate, @Qty = rd.Quantity
                                    FROM RentalTransactions t
                                    INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                                    INNER JOIN Items i ON rd.ItemID = i.ItemID
                                    WHERE t.TransactionID = @TxID;

                                    IF @ActualReturn < @ExpectedEnd AND @ActualReturn >= @OriginalStart
                                    BEGIN
                                        DECLARE @DaysUsed INT, @DaysEarlier INT, @NewTotal DECIMAL(10,2);
                                        
                                        SET @DaysUsed = DATEDIFF(day, CAST(@OriginalStart AS DATE), CAST(@ActualReturn AS DATE));
                                        IF @DaysUsed <= 0 SET @DaysUsed = 1;

                                        SET @DaysEarlier = DATEDIFF(day, CAST(@ActualReturn AS DATE), CAST(@ExpectedEnd AS DATE));
                                        
                                        IF @DaysEarlier > 0
                                        BEGIN
                                            SET @NewTotal = (@DailyRate * @Qty * @DaysUsed);
                                            
                                            UPDATE RentalTransactions 
                                            SET TotalAmount = @NewTotal
                                            WHERE TransactionID = @TxID;
                                            
                                            UPDATE RentalDetails 
                                            SET Subtotal = @NewTotal 
                                            WHERE TransactionID = @TxID AND ItemID = @ItemID;
                                        END
                                    END;

                                    UPDATE RentalTransactions 
                                    SET Status = 'Completed', ActualReturnDate = @ActualReturn, Notes = CASE WHEN @Notes <> '' THEN @Notes ELSE Notes END
                                    WHERE TransactionID = @TxID;

                                    UPDATE RentalDetails 
                                    SET ConditionAfter = @Condition, DamageNotes = @Notes 
                                    WHERE TransactionID = @TxID AND ItemID = @ItemID;";

                                using (SqlCommand cmdComplete = new SqlCommand(completeTxSql, conn, trans))
                                {
                                    cmdComplete.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                                    cmdComplete.Parameters.AddWithValue("@ActualReturn", dtpActualReturn.Value);
                                    cmdComplete.Parameters.AddWithValue("@Notes", notesText);
                                    cmdComplete.Parameters.AddWithValue("@Condition", condition);
                                    cmdComplete.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                    cmdComplete.ExecuteNonQuery();
                                }

                                string stockSql = "";
                                if (condition == "Good")
                                {
                                    stockSql = @"
                                        UPDATE Items 
                                        SET AvailableQuantity = AvailableQuantity + @Qty,
                                            Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END
                                        WHERE ItemID = @ItemID;";
                                }
                                else
                                {
                                    stockSql = "UPDATE Items SET Status = 'Maintenance' WHERE ItemID = @ItemID;";
                                }

                                using (SqlCommand cmdStock = new SqlCommand(stockSql, conn, trans))
                                {
                                    cmdStock.Parameters.AddWithValue("@ItemID", this.activeItemId);
                                    cmdStock.Parameters.AddWithValue("@Qty", this.activeRentedQuantity);
                                    cmdStock.ExecuteNonQuery();
                                }
                            }

                            if (finalRefundValue > 0)
                            {
                                string paymentSql = @"
                                    INSERT INTO Payments (TransactionID, PaymentDate, Amount, Method, Notes)
                                    VALUES (@TxID, GETDATE(), @Amount, 'Deposit Refund', @Notes);";

                                using (SqlCommand cmdPay = new SqlCommand(paymentSql, conn, trans))
                                {
                                    cmdPay.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                                    cmdPay.Parameters.AddWithValue("@Amount", finalRefundValue);
                                    cmdPay.Parameters.AddWithValue("@Notes", $"Processed check-in settlement. Condition: {condition}.");
                                    cmdPay.ExecuteNonQuery();
                                }
                            }

                            string logSql = @"
                                INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                                VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, @Desc, GETDATE());";

                            using (SqlCommand cmdLog = new SqlCommand(logSql, conn, trans))
                            {
                                cmdLog.Parameters.AddWithValue("@UserID", this.currentLoggedInUserId);
                                cmdLog.Parameters.AddWithValue("@TxID", this.activeTransactionId);
                                string actionString = actualDate < startDate ? "Full early-return cancellation sweep" : "Partial contract early check-in settlement";
                                cmdLog.Parameters.AddWithValue("@Desc", $"Executed {actionString} for Item ID {this.activeItemId}. Condition: {condition}");
                                cmdLog.ExecuteNonQuery();
                            }

                            trans.Commit();
                            MessageBox.Show("Transaction status updated and closed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ResetFormInputs();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show("Failed to save check-in transaction: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connection failure: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (cmbConditionAfter != null) cmbConditionAfter.SelectedIndex = 0;
            dtpActualReturn.Value = DateTime.Now;
        }

        private void OnFormRequiredExit(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
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
