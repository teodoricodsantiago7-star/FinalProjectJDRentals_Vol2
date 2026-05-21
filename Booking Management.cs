using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class Booking_Management : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private int currentLoggedInUserId;


        public Booking_Management()
        {
            InitializeComponent();
            this.currentLoggedInUserId = 1;
            SetupFormDefaults();
        }

        public Booking_Management(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            SetupFormDefaults();
        }

        private void Booking_Management_Load(object sender, EventArgs e)
        {
            SetupFormDefaults();
        }
        private void SetupFormDefaults()
        {
            ConfigureBookingGrid();
            RefreshBookingData();
            LoadUserProfilePicture();

            if (btnAddBooking != null)
            {
                btnAddBooking.Click -= btnAddBooking_Click;
                btnAddBooking.Click += btnAddBooking_Click;
            }
        }


        private void btnAddBooking_Click(object sender, EventArgs e)
        {
            Form addModal = new Form()
            {
                Width = 440,
                Height = 440,
                Text = "Create New Booking Reservation",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblCust = new Label { Text = "Select Customer:", Location = new Point(20, 20), Size = new Size(120, 20) };
            ComboBox cmbCust = new ComboBox { Location = new Point(150, 17), Size = new Size(240, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblItem = new Label { Text = "Select Item:", Location = new Point(20, 60), Size = new Size(120, 20) };
            ComboBox cmbItm = new ComboBox { Location = new Point(150, 57), Size = new Size(240, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblQty = new Label { Text = "Quantity:", Location = new Point(20, 100), Size = new Size(120, 20) };
            NumericUpDown numQty = new NumericUpDown { Location = new Point(150, 98), Size = new Size(240, 20), Minimum = 1, Maximum = 9999 };

            Label lblStart = new Label { Text = "Start Date:", Location = new Point(20, 140), Size = new Size(120, 20) };
            DateTimePicker dtpStart = new DateTimePicker { Location = new Point(150, 137), Size = new Size(240, 20), Format = DateTimePickerFormat.Short };
            dtpStart.MinDate = DateTime.Today;

            Label lblEnd = new Label { Text = "Return Date:", Location = new Point(20, 180), Size = new Size(120, 20) };
            DateTimePicker dtpEnd = new DateTimePicker { Location = new Point(150, 177), Size = new Size(240, 20), Format = DateTimePickerFormat.Short };
            dtpEnd.MinDate = dtpStart.Value.Date;

            dtpStart.ValueChanged += (s, ev) =>
            {
                dtpEnd.MinDate = dtpStart.Value.Date;
            };

            Label lblAdvancePaid = new Label { Text = "Advance Paid (₱):", Location = new Point(20, 220), Size = new Size(120, 20) };
            NumericUpDown numAdvancePaid = new NumericUpDown { Location = new Point(150, 218), Size = new Size(240, 20), Minimum = 0, Maximum = 999999, DecimalPlaces = 2 };

            Label lblNotes = new Label { Text = "Booking Notes:", Location = new Point(20, 260), Size = new Size(120, 20) };
            RichTextBox rtbNotesInput = new RichTextBox { Location = new Point(150, 257), Size = new Size(240, 60) };

            Button btnSave = new Button { Text = "Save Booking", Location = new Point(150, 340), Size = new Size(115, 32), DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(275, 340), Size = new Size(115, 32), DialogResult = DialogResult.Cancel };

            addModal.Controls.AddRange(new Control[] { lblCust, cmbCust, lblItem, cmbItm, lblQty, numQty, lblStart, dtpStart, lblEnd, dtpEnd, lblAdvancePaid, numAdvancePaid, lblNotes, rtbNotesInput, btnSave, btnCancel });
            addModal.AcceptButton = btnSave;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT CustomerID, Name FROM Customers ORDER BY Name ASC;", conn))
                    {
                        DataTable dtCust = new DataTable();
                        dtCust.Load(cmd.ExecuteReader());
                        cmbCust.DataSource = dtCust;
                        cmbCust.DisplayMember = "Name";
                        cmbCust.ValueMember = "CustomerID";
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT ItemID, ItemName FROM Items WHERE Status = 'Available' AND AvailableQuantity > 0 ORDER BY ItemName ASC;", conn))
                    {
                        DataTable dtItem = new DataTable();
                        dtItem.Load(cmd.ExecuteReader());
                        cmbItm.DataSource = dtItem;
                        cmbItm.DisplayMember = "ItemName";
                        cmbItm.ValueMember = "ItemID";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Initialization error: " + ex.Message);
                    return;
                }
            }

            if (addModal.ShowDialog() == DialogResult.OK)
            {
                if (cmbCust.SelectedValue == null || cmbItm.SelectedValue == null)
                {
                    MessageBox.Show("Please choose a customer and an item first.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int customerId = Convert.ToInt32(cmbCust.SelectedValue);
                int itemId = Convert.ToInt32(cmbItm.SelectedValue);
                int qty = (int)numQty.Value;
                DateTime start = dtpStart.Value.Date.AddHours(8);
                DateTime end = dtpEnd.Value.Date.AddHours(17);
                string textNotes = rtbNotesInput.Text.Trim();

                if (start < DateTime.Today)
                {
                    MessageBox.Show("Booking start date cannot be set earlier than the current date.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (end < start)
                {
                    MessageBox.Show("Return date cannot be earlier than the start date.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        decimal itemRate = 0;
                        int currentAvailable = 0;
                        using (SqlCommand cmdItem = new SqlCommand("SELECT DailyRate, AvailableQuantity FROM Items WHERE ItemID = @ItemID;", conn, trans))
                        {
                            cmdItem.Parameters.AddWithValue("@ItemID", itemId);
                            using (SqlDataReader reader = cmdItem.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    itemRate = Convert.ToDecimal(reader["DailyRate"]);
                                    currentAvailable = Convert.ToInt32(reader["AvailableQuantity"]);
                                }
                            }
                        }

                        if (currentAvailable < qty)
                        {
                            MessageBox.Show($"Not enough stock available. Only {currentAvailable} items left.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            trans.Rollback();
                            return;
                        }

                        int totalDays = (int)Math.Ceiling((end - start).TotalDays);
                        if (totalDays <= 0) totalDays = 1;
                        decimal totalContractPrice = itemRate * qty * totalDays;

                        string insertTx = @"
                            INSERT INTO RentalTransactions (CustomerID, UserID, RentalStartDate, ExpectedReturnDate, TotalAmount, DepositAmount, AmountPaid, PaymentMethod, Status, Notes, CreatedAt)
                            OUTPUT INSERTED.TransactionID
                            VALUES (@CustomerID, @UserID, @StartDate, @ExpectedReturnDate, @TotalAmount, 0, @AmountPaid, 'Cash', 'Pending', @Notes, GETDATE());";

                        int newTxId = 0;
                        using (SqlCommand cmdTx = new SqlCommand(insertTx, conn, trans))
                        {
                            cmdTx.Parameters.AddWithValue("@CustomerID", customerId);
                            cmdTx.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                            cmdTx.Parameters.AddWithValue("@StartDate", start);
                            cmdTx.Parameters.AddWithValue("@ExpectedReturnDate", end);
                            cmdTx.Parameters.AddWithValue("@TotalAmount", totalContractPrice);
                            cmdTx.Parameters.AddWithValue("@AmountPaid", numAdvancePaid.Value);
                            cmdTx.Parameters.AddWithValue("@Notes", textNotes);
                            newTxId = Convert.ToInt32(cmdTx.ExecuteScalar());
                        }

                        string insertDetails = @"
                            INSERT INTO RentalDetails (TransactionID, ItemID, Quantity, Subtotal, ConditionBefore)
                            VALUES (@TransactionID, @ItemID, @Quantity, @Subtotal, 'Good');";

                        using (SqlCommand cmdDetails = new SqlCommand(insertDetails, conn, trans))
                        {
                            cmdDetails.Parameters.AddWithValue("@TransactionID", newTxId);
                            cmdDetails.Parameters.AddWithValue("@ItemID", itemId);
                            cmdDetails.Parameters.AddWithValue("@Quantity", qty);
                            cmdDetails.Parameters.AddWithValue("@Subtotal", totalContractPrice);
                            cmdDetails.ExecuteNonQuery();
                        }

                        string deductAndTrackSql = @"
                            UPDATE Items 
                            SET AvailableQuantity = AvailableQuantity - @Quantity 
                            WHERE ItemID = @ItemID;

                            UPDATE Items
                            SET Status = 'Fully Booked'
                            WHERE ItemID = @ItemID AND AvailableQuantity <= 0;

                            /* INCREMENT TOTAL RENTALS COUNTER FOR CUSTOMER IMMEDIATELY */
                            UPDATE Customers 
                            SET TotalRentals = TotalRentals + 1 
                            WHERE CustomerID = @CustomerID;";

                        using (SqlCommand cmdDeduct = new SqlCommand(deductAndTrackSql, conn, trans))
                        {
                            cmdDeduct.Parameters.AddWithValue("@Quantity", qty);
                            cmdDeduct.Parameters.AddWithValue("@ItemID", itemId);
                            cmdDeduct.Parameters.AddWithValue("@CustomerID", customerId);
                            cmdDeduct.ExecuteNonQuery();
                        }

                        trans.Commit();
                        MessageBox.Show("Booking reservation saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshBookingData();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Could not save the booking. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }





        private void ConfigureBookingGrid()
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

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TransactionID",
                HeaderText = "ID",
                DataPropertyName = "TransactionID",
                ReadOnly = true,
                Visible = false
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Customer Name", DataPropertyName = "CustomerName", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "Rented Items", DataPropertyName = "ItemName", ReadOnly = true });

            var startCol = new DataGridViewTextBoxColumn { Name = "RentalStartDate", HeaderText = "Start Date", DataPropertyName = "RentalStartDate", ReadOnly = true };
            startCol.DefaultCellStyle.Format = "MM/dd/yyyy hh:mm tt";
            dataGridView1.Columns.Add(startCol);

            var returnCol = new DataGridViewTextBoxColumn { Name = "ExpectedReturnDate", HeaderText = "Return Date", DataPropertyName = "ExpectedReturnDate", ReadOnly = true };
            returnCol.DefaultCellStyle.Format = "MM/dd/yyyy hh:mm tt";
            dataGridView1.Columns.Add(returnCol);

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", ReadOnly = true });

            var viewButtonCol = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "Review",
                Text = "View Info",
                UseColumnTextForButtonValue = true
            };
            dataGridView1.Columns.Add(viewButtonCol);

            dataGridView1.CellContentClick -= DataGridView1_CellContentClick;
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
        }

        private void RefreshBookingData()
        {
            if (dataGridView1 == null) return;

            string searchKeyword = txtSearch != null ? txtSearch.Text.Trim() : "";

            string query = @"
                SELECT t.TransactionID, c.Name AS CustomerName, i.ItemName, t.RentalStartDate, t.ExpectedReturnDate, t.Status
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE t.Status IN ('Pending', 'Confirmed')";

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query += " AND (c.Name LIKE '%' + @Search + '%' OR i.ItemName LIKE '%' + @Search + '%')";
            }
            query += " ORDER BY t.RentalStartDate ASC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not load the booking data.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dataGridView1.Columns["Action"].Index) return;

            int transactionId = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["TransactionID"].Value);
            ShowBookingViewReviewModal(transactionId);
        }
        private void ShowBookingViewReviewModal(int txId)
        {
            string customerName = "";
            string itemName = "";
            int quantity = 0;
            DateTime start = DateTime.Now;
            DateTime returnDate = DateTime.Now;
            decimal total = 0;
            decimal deposit = 0;
            decimal paid = 0;
            string method = "Cash";
            string currentStatus = "Pending";
            string notes = "";
            int itemId = 0;
            int customerId = 0;

            string selectQuery = @"
                SELECT c.CustomerID, c.Name AS CustomerName, i.ItemID, i.ItemName, rd.Quantity, 
                       t.RentalStartDate, t.ExpectedReturnDate, t.TotalAmount, t.DepositAmount, 
                       t.AmountPaid, t.PaymentMethod, t.Status, t.Notes
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE t.TransactionID = @TxID;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@TxID", txId);
                try
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            customerId = Convert.ToInt32(r["CustomerID"]);
                            customerName = r["CustomerName"].ToString();
                            itemId = Convert.ToInt32(r["ItemID"]);
                            itemName = r["ItemName"].ToString();
                            quantity = Convert.ToInt32(r["Quantity"]);
                            start = Convert.ToDateTime(r["RentalStartDate"]);
                            returnDate = Convert.ToDateTime(r["ExpectedReturnDate"]);
                            total = Convert.ToDecimal(r["TotalAmount"]);
                            deposit = Convert.ToDecimal(r["DepositAmount"]);
                            paid = Convert.ToDecimal(r["AmountPaid"]);
                            method = r["PaymentMethod"].ToString();
                            currentStatus = r["Status"].ToString();
                            notes = r["Notes"].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open booking information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form viewModal = new Form()
            {
                Width = 520,
                Height = 460,
                Text = $"Booking Information - ID {txId}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblDetails = new Label()
            {
                Left = 25,
                Top = 20,
                Width = 460,
                Height = 130,
                Font = new Font("Arial", 10, FontStyle.Regular),
                Text = "TRANSACTION REVIEWS SHEET\n" +
                       "--------------------------------------------------\n\n" +
                       $"Customer Name: {customerName}\n" +
                       $"Item Selection: {itemName} ({quantity}x)\n" +
                       $"Rental Period: {start:MM/dd/yyyy hh:mm tt} to {returnDate:MM/dd/yyyy hh:mm tt}\n" +
                       $"Total Contract: ₱{total:N2}  |  Deposit Held: ₱{deposit:N2}\n" +
                       $"Amount Paid: ₱{paid:N2} ({method})\n" +
                       $"Current Status: {currentStatus}"
            };

            Label lblNotesTitle = new Label { Text = "Booking Notes:", Location = new Point(25, 160), Size = new Size(120, 18), Font = new Font("Arial", 10, FontStyle.Bold) };
            RichTextBox rtbNotesDisplay = new RichTextBox { Location = new Point(25, 185), Size = new Size(450, 65), ReadOnly = true, Text = string.IsNullOrWhiteSpace(notes) ? "No notes found." : notes, BackColor = Color.FromArgb(245, 245, 245) };

            Button btnAccept = new Button { Text = "Accept Rental", Left = 25, Top = 270, Width = 140, Height = 35, BackColor = Color.LightGreen, Font = new Font("Arial", 9, FontStyle.Bold) };
            Button btnCancel = new Button { Text = "Cancel Rental", Left = 180, Top = 270, Width = 140, Height = 35, BackColor = Color.MistyRose, ForeColor = Color.DarkRed, Font = new Font("Arial", 9, FontStyle.Bold) };
            Button btnEdit = new Button { Text = "Edit Details", Left = 335, Top = 270, Width = 140, Height = 35, BackColor = Color.LightCyan, Font = new Font("Arial", 9, FontStyle.Bold) };
            Button btnClose = new Button { Text = "Close Window", Left = 335, Top = 360, Width = 140, Height = 30, DialogResult = DialogResult.Cancel };

            if (currentStatus == "Ongoing" || currentStatus == "Completed" || currentStatus == "Cancelled" || currentStatus == "Overdue")
            {
                btnAccept.Enabled = false;
                btnCancel.Enabled = false;
            }

            viewModal.Controls.AddRange(new Control[] { lblDetails, lblNotesTitle, rtbNotesDisplay, btnAccept, btnCancel, btnEdit, btnClose });

            btnAccept.Click += (s, ev) =>
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            string updateSql = @"
                        UPDATE RentalTransactions SET Status = 'Ongoing' WHERE TransactionID = @TxID;
                        INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                        VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, 'Accepted booking to Ongoing state via workflow override pipeline.', GETDATE());";

                            using (SqlCommand cmdUpdate = new SqlCommand(updateSql, conn, trans))
                            {
                                cmdUpdate.Parameters.AddWithValue("@TxID", txId);
                                cmdUpdate.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                                cmdUpdate.ExecuteNonQuery();
                            }

                            trans.Commit();
                            viewModal.Close();

                            if (pbProfilePic != null && pbProfilePic.Image != null)
                            {
                                pbProfilePic.Image.Dispose();
                                pbProfilePic.Image = null;
                            }
                            this.FormClosed -= (object src, FormClosedEventArgs args) => Application.Exit();

                            NewRentalTransaction checkOutForm = new NewRentalTransaction(this.currentLoggedInUserId, txId);
                            checkOutForm.FormClosed += (object src, FormClosedEventArgs args) => Application.Exit();

                            DateTimePicker targetStart = checkOutForm.Controls.Find("dtpStartDate", true).Length > 0
                                ? (DateTimePicker)checkOutForm.Controls.Find("dtpStartDate", true)[0] : null;
                            DateTimePicker targetEnd = checkOutForm.Controls.Find("dtpExpectedReturnDate", true).Length > 0
                                ? (DateTimePicker)checkOutForm.Controls.Find("dtpExpectedReturnDate", true)[0] : null;

                            if (targetStart != null && targetEnd != null)
                            {
                                targetStart.MinDate = DateTime.MinValue;
                                targetEnd.MinDate = DateTime.MinValue;
                            }

                            this.Hide();
                            checkOutForm.Show();
                            this.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Transaction execution error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };



            btnCancel.Click += (s, ev) =>
            {
                DialogResult doubleCheck = MessageBox.Show("Are you sure you want to cancel this booking?", "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (doubleCheck != DialogResult.Yes) return;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                string cancelSql = @"
                                    UPDATE RentalTransactions SET Status = 'Cancelled' WHERE TransactionID = @TxID;
                                    UPDATE Items SET AvailableQuantity = AvailableQuantity + @Qty,
                                           Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END
                                    WHERE ItemID = @ItemID;
                                    INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                                    VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, 'Cancelled booking reference and returned items stock.', GETDATE());";

                                using (SqlCommand cmdCancel = new SqlCommand(cancelSql, conn, trans))
                                {
                                    cmdCancel.Parameters.AddWithValue("@TxID", txId);
                                    cmdCancel.Parameters.AddWithValue("@Qty", quantity);
                                    cmdCancel.Parameters.AddWithValue("@ItemID", itemId);
                                    cmdCancel.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                                    cmdCancel.ExecuteNonQuery();
                                }

                                trans.Commit();
                                MessageBox.Show("Booking successfully updated to Cancelled. Inventory counts replenished.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                viewModal.Close();

                                SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId));
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                MessageBox.Show("Failed to complete processing cancellation transaction: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database operation error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            btnEdit.Click += (s, ev) =>
            {
                viewModal.Close();
                ShowBookingLegacyEditForm(txId);
            };

            viewModal.ShowDialog();
            viewModal.Dispose();
        }

        private void ShowBookingLegacyEditForm(int txId)
        {
            string customerName = ""; string itemName = ""; int quantity = 0; DateTime start = DateTime.Now; DateTime returnDate = DateTime.Now;
            decimal total = 0; decimal deposit = 0; decimal paid = 0; string method = "Cash"; string currentStatus = "Pending"; string notes = "";
            int itemId = 0; int customerId = 0;

            string selectQuery = @"
                SELECT t.CustomerID, c.Name AS CustomerName, i.ItemID, i.ItemName, rd.Quantity, t.RentalStartDate, t.ExpectedReturnDate, 
                       t.TotalAmount, t.DepositAmount, t.AmountPaid, t.PaymentMethod, t.Status, t.Notes
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE t.TransactionID = @TxID;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@TxID", txId);
                try
                {
                    conn.Open(); using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            customerId = Convert.ToInt32(r["CustomerID"]); customerName = r["CustomerName"].ToString();
                            itemId = Convert.ToInt32(r["ItemID"]); itemName = r["ItemName"].ToString(); quantity = Convert.ToInt32(r["Quantity"]);
                            start = Convert.ToDateTime(r["RentalStartDate"]); returnDate = Convert.ToDateTime(r["ExpectedReturnDate"]);
                            total = Convert.ToDecimal(r["TotalAmount"]); deposit = Convert.ToDecimal(r["DepositAmount"]);
                            paid = Convert.ToDecimal(r["AmountPaid"]); method = r["PaymentMethod"].ToString();
                            currentStatus = r["Status"].ToString(); notes = r["Notes"].ToString();
                        }
                    }
                }
                catch { return; }
            }

            Form modal = new Form()
            {
                Width = 520,
                Height = 480,
                Text = $"Modify Parameters - ID {txId}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblInfo = new Label() { Left = 20, Top = 15, Width = 460, Font = new Font("Arial", 10, FontStyle.Bold), Text = $"Customer: {customerName}\nItem: {itemName} (Qty: {quantity})\nDates: {start:MM/dd/yyyy} to {returnDate:MM/dd/yyyy}", Height = 55 };
            Label lblCosts = new Label() { Left = 20, Top = 80, Width = 460, Font = new Font("Arial", 9, FontStyle.Regular), Text = $"Total Price: ₱{total:N2}   |   Deposit Left: ₱{deposit:N2}" };
            Label lblPaid = new Label() { Left = 20, Top = 120, Width = 150, Text = "Amount Paid (₱):" };
            TextBox txtPaid = new TextBox() { Left = 180, Top = 117, Width = 150, Text = paid.ToString("F2") };
            Label lblMethod = new Label() { Left = 20, Top = 160, Width = 150, Text = "Payment Method:" };
            ComboBox cmbMethod = new ComboBox() { Left = 180, Top = 157, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMethod.Items.AddRange(new string[] { "Cash", "GCash", "Partial" }); cmbMethod.SelectedItem = method;
            Label lblStatus = new Label() { Left = 20, Top = 200, Width = 150, Text = "Booking Status:" };
            ComboBox cmbStatus = new ComboBox() { Left = 180, Top = 197, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new string[] { "Pending", "Confirmed", "Ongoing", "Completed", "Cancelled", "Overdue" }); cmbStatus.SelectedItem = cmbStatus.Items.Contains(currentStatus) ? currentStatus : "Pending";
            Label lblNotes = new Label() { Left = 20, Top = 240, Width = 150, Text = "Booking Notes:" };
            TextBox txtNotesBox = new TextBox() { Left = 20, Top = 265, Width = 460, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = notes };
            Button btnSave = new Button { Text = "Save Changes", Left = 260, Top = 370, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Left = 380, Top = 370, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };

            modal.Controls.AddRange(new Control[] { lblInfo, lblCosts, lblPaid, txtPaid, lblMethod, cmbStatus, lblNotes, txtNotesBox, btnSave, btnCancel, lblStatus, cmbMethod });
            modal.AcceptButton = btnSave;

            if (modal.ShowDialog() == DialogResult.OK)
            {
                if (!decimal.TryParse(txtPaid.Text.Trim(), out decimal newPaid) || newPaid < 0) return;
                string newStatus = cmbStatus.SelectedItem.ToString(); string newMethod = cmbMethod.SelectedItem.ToString(); string newNotes = txtNotesBox.Text.Trim();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open(); using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            if (currentStatus != "Ongoing" && currentStatus != "Completed" && currentStatus != "Overdue" && (newStatus == "Ongoing" || newStatus == "Overdue"))
                            {
                                string checkStock = "SELECT AvailableQuantity FROM Items WHERE ItemID = @ItemID;"; int available = 0;
                                using (SqlCommand cmdStock = new SqlCommand(checkStock, conn, trans)) { cmdStock.Parameters.AddWithValue("@ItemID", itemId); available = Convert.ToInt32(cmdStock.ExecuteScalar()); }
                                if (available < quantity) { trans.Rollback(); return; }
                                string deductStock = "UPDATE Items SET AvailableQuantity = AvailableQuantity - @Qty, Status = CASE WHEN (AvailableQuantity - @Qty) <= 0 THEN 'Fully Booked' ELSE Status END WHERE ItemID = @ItemID;";
                                using (SqlCommand cmdDeduct = new SqlCommand(deductStock, conn, trans)) { cmdDeduct.Parameters.AddWithValue("@Qty", quantity); cmdDeduct.Parameters.AddWithValue("@ItemID", itemId); cmdDeduct.ExecuteNonQuery(); }
                            }
                            if ((currentStatus == "Ongoing" || currentStatus == "Overdue") && (newStatus == "Cancelled" || newStatus == "Pending" || newStatus == "Confirmed"))
                            {
                                string returnStock = "UPDATE Items SET AvailableQuantity = AvailableQuantity + @Qty, Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END WHERE ItemID = @ItemID;";
                                using (SqlCommand cmdReturn = new SqlCommand(returnStock, conn, trans)) { cmdReturn.Parameters.AddWithValue("@Qty", quantity); cmdReturn.Parameters.AddWithValue("@ItemID", itemId); cmdReturn.ExecuteNonQuery(); }
                            }

                            string updateTxSql = ((currentStatus == "Ongoing" || currentStatus == "Overdue") && newStatus == "Completed")
                                ? "UPDATE RentalTransactions SET AmountPaid=@Paid, PaymentMethod=@Method, Status=@Status, Notes=@Notes, ActualReturnDate=GETDATE() WHERE TransactionID=@TxID; UPDATE Items SET AvailableQuantity=AvailableQuantity+@Qty, Status=CASE WHEN Status='Fully Booked' THEN 'Available' ELSE Status END WHERE ItemID=@ItemID; UPDATE Customers SET TotalRentals=TotalRentals+1 WHERE CustomerID=@CustomerID;"
                                : "UPDATE RentalTransactions SET AmountPaid=@Paid, PaymentMethod=@Method, Status=@Status, Notes=@Notes WHERE TransactionID=@TxID;";

                            using (SqlCommand cmdUpdate = new SqlCommand(updateTxSql, conn, trans))
                            {
                                cmdUpdate.Parameters.AddWithValue("@Paid", newPaid); cmdUpdate.Parameters.AddWithValue("@Method", newMethod); cmdUpdate.Parameters.AddWithValue("@Status", newStatus); cmdUpdate.Parameters.AddWithValue("@Notes", newNotes); cmdUpdate.Parameters.AddWithValue("@TxID", txId);
                                if (updateTxSql.Contains("@Qty")) { cmdUpdate.Parameters.AddWithValue("@Qty", quantity); cmdUpdate.Parameters.AddWithValue("@ItemID", itemId); cmdUpdate.Parameters.AddWithValue("@CustomerID", customerId); }
                                cmdUpdate.ExecuteNonQuery();
                            }

                            string logSql = "INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime) VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, @Desc, GETDATE());";
                            using (SqlCommand cmdLog = new SqlCommand(logSql, conn, trans)) { cmdLog.Parameters.AddWithValue("@UserID", currentLoggedInUserId); cmdLog.Parameters.AddWithValue("@TxID", txId); cmdLog.Parameters.AddWithValue("@Desc", $"Edited details via legacy dashboard override script for row {txId}."); cmdLog.ExecuteNonQuery(); }

                            trans.Commit(); MessageBox.Show("Changes synchronized successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); RefreshBookingData();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Data exception error details: " + ex.Message); }
                }
            }
            modal.Dispose();
        }

        private void cmbFilters_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshBookingData();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            RefreshBookingData();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (txtSearch != null) txtSearch.Text = string.Empty;
            RefreshBookingData();
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
                    conn.Open(); using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (UserNameHeader != null) UserNameHeader.Text = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "Staff Member";
                            if (pbProfilePic != null && reader["ImagePath"] != DBNull.Value)
                            {
                                string path = reader["ImagePath"].ToString();
                                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                                {
                                    pbProfilePic.Image?.Dispose(); byte[] bytes = File.ReadAllBytes(path);
                                    using (MemoryStream ms = new MemoryStream(bytes)) { pbProfilePic.Image = Image.FromStream(ms); }
                                    pbProfilePic.SizeMode = PictureBoxSizeMode.Zoom;
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

        private void SafelyNavigateToForm(Form targetForm)
        {
            if (pbProfilePic != null && pbProfilePic.Image != null) { pbProfilePic.Image.Dispose(); pbProfilePic.Image = null; }
            this.FormClosed -= (s, a) => Application.Exit(); targetForm.FormClosed += (s, a) => Application.Exit();
            this.Hide(); targetForm.Show(); this.Dispose();
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
