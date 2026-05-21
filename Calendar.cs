using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class Calendar : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private int currentLoggedInUserId;
        private DateTime currentCalendarMonthStart;
        private bool isListViewActive = false;

        public Calendar(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            currentCalendarMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            ConfigureCalendarGridStructure();
            SetupSearchFilterDropdown();

            if (btnSearch != null) btnSearch.Enabled = false;
            if (txtSearch != null) txtSearch.Enabled = false;
            if (cmbFilters != null) cmbFilters.Enabled = false;

            RefreshCalendarData();
        }

        private void Calendar_Load(object sender, EventArgs e)
        {
            ConfigureCalendarGridStructure();
            SetupSearchFilterDropdown();

            if (btnSearch != null) btnSearch.Enabled = false;
            if (txtSearch != null) txtSearch.Enabled = false;
            if (cmbFilters != null) cmbFilters.Enabled = false;

            RefreshCalendarData();
        }


        private void SetupSearchFilterDropdown()
        {
            if (cmbFilters == null) return;

            cmbFilters.SelectedIndexChanged -= cmbFilters_SelectedIndexChanged;
            cmbFilters.Items.Clear();
            cmbFilters.Items.AddRange(new string[] { "All", "Ongoing", "Overdue", "Completed", "Cancelled" });
            cmbFilters.SelectedIndex = 0;
            cmbFilters.SelectedIndexChanged += cmbFilters_SelectedIndexChanged;
        }

        private void RefreshCalendarData()
        {
            try
            {
                if (lblMonthYearHeader != null)
                {
                    lblMonthYearHeader.Text = currentCalendarMonthStart.ToString("MMMM yyyy");
                }
            }
            catch { }

            PopulateWeeklyScheduleGrid();
            LoadUserProfilePicture();
        }

        private void ConfigureCalendarGridStructure()
        {
            if (dataGridView1 == null) return;

            dataGridView1.Columns.Clear();
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            string[] days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            for (int i = 0; i < 7; i++)
            {
                dataGridView1.Columns.Add("col" + days[i], days[i]);
            }

            dataGridView1.CellClick -= DataGridView1_CellClick;
            dataGridView1.CellClick += DataGridView1_CellClick;
        }

        private void PopulateWeeklyScheduleGrid()
        {
            if (dataGridView1 == null) return;
            dataGridView1.Rows.Clear();

            DateTime firstDayOfMonth = currentCalendarMonthStart;
            int daysInMonth = DateTime.DaysInMonth(firstDayOfMonth.Year, firstDayOfMonth.Month);
            int dayOfWeekOffset = (int)firstDayOfMonth.DayOfWeek;

            int totalCellsNeeded = daysInMonth + dayOfWeekOffset;
            int totalRowsNeeded = (int)Math.Ceiling((double)totalCellsNeeded / 7);

            DateTime calendarGridStart = firstDayOfMonth.AddDays(-dayOfWeekOffset);

            for (int r = 0; r < totalRowsNeeded; r++)
            {
                int rowIndex = dataGridView1.Rows.Add();
                dataGridView1.Rows[rowIndex].Height = 100;

                for (int c = 0; c < 7; c++)
                {
                    DateTime targetDate = calendarGridStart.AddDays((r * 7) + c);
                    DataGridViewCell cell = dataGridView1.Rows[rowIndex].Cells[c];
                    cell.Style.BackColor = Color.White;

                    if (targetDate.Month == firstDayOfMonth.Month)
                    {

                        cell.Value = $"[{targetDate.Day}]\nNo Rentals\nNo Bookings";
                        cell.Style.ForeColor = Color.DarkGray;
                        cell.Tag = targetDate.Date;
                    }
                    else
                    {
                        cell.Value = "";
                        cell.Style.BackColor = Color.WhiteSmoke;
                        cell.Tag = null;
                    }
                }
            }

            DateTime monthEnd = firstDayOfMonth.AddMonths(1);
            string filterStatus = cmbFilters != null ? cmbFilters.SelectedItem?.ToString() : "All";
            string searchKeyword = txtSearch != null ? txtSearch.Text.Trim() : "";

            string scheduleQuery = @"
                SELECT 
                    t.RentalStartDate,
                    t.Status
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE CAST(t.RentalStartDate AS DATE) < CAST(@MonthEnd AS DATE) 
                  AND CAST(t.ExpectedReturnDate AS DATE) >= CAST(@MonthStart AS DATE)";

            if (filterStatus != "All")
            {
                scheduleQuery += " AND t.Status = @Status";
            }
            else
            {
                scheduleQuery += " AND t.Status IN ('Pending', 'Ongoing', 'Overdue', 'Completed', 'Cancelled')";
            }

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                scheduleQuery += " AND (c.Name LIKE '%' + @Search + '%' OR i.ItemName LIKE '%' + @Search + '%')";
            }

            Dictionary<DateTime, int> rentalCounts = new Dictionary<DateTime, int>();
            Dictionary<DateTime, int> bookingCounts = new Dictionary<DateTime, int>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(scheduleQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MonthStart", firstDayOfMonth.Date);
                    cmd.Parameters.AddWithValue("@MonthEnd", monthEnd.Date);
                    if (filterStatus != "All") cmd.Parameters.AddWithValue("@Status", filterStatus);
                    if (!string.IsNullOrEmpty(searchKeyword)) cmd.Parameters.AddWithValue("@Search", searchKeyword);

                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime start = Convert.ToDateTime(reader["RentalStartDate"]).Date;
                                string status = reader["Status"].ToString().Trim();

                                if (status == "Pending")
                                {
                                    if (bookingCounts.ContainsKey(start)) bookingCounts[start]++;
                                    else bookingCounts[start] = 1;
                                }
                                else
                                {
                                    if (rentalCounts.ContainsKey(start)) rentalCounts[start]++;
                                    else rentalCounts[start] = 1;
                                }
                            }
                        }

                        for (int r = 0; r < dataGridView1.Rows.Count; r++)
                        {
                            for (int c = 0; c < 7; c++)
                            {
                                DataGridViewCell cell = dataGridView1.Rows[r].Cells[c];
                                if (cell.Tag != null)
                                {
                                    DateTime cellDate = (DateTime)cell.Tag;

                                    int rentals = rentalCounts.ContainsKey(cellDate) ? rentalCounts[cellDate] : 0;
                                    int bookings = bookingCounts.ContainsKey(cellDate) ? bookingCounts[cellDate] : 0;

                                    string rentalText = rentals == 0 ? "No Rentals" : (rentals == 1 ? "1 Rental" : $"{rentals} Rentals");
                                    string bookingText = bookings == 0 ? "No Bookings" : (bookings == 1 ? "1 Booking" : $"{bookings} Bookings");

                                    cell.Value = $"[{cellDate.Day}]\n{rentalText}\n{bookingText}";

                                    if (rentals > 0 || bookings > 0)
                                    {
                                        cell.Style.ForeColor = Color.Black;
                                    }
                                    else
                                    {
                                        cell.Style.ForeColor = Color.DarkGray;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not load calendar entries.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (isListViewActive)
            {
                PopulateRentalListView();
            }
            else
            {
                RefreshCalendarData();
            }
        }

        private void cmbFilters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (richTextBox1 != null) richTextBox1.Clear();

            if (isListViewActive)
            {
                PopulateRentalListView();
            }
            else
            {
                RefreshCalendarData();
            }
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
                            if (UserNameHeader != null)
                            {
                                UserNameHeader.Text = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "Staff Member";
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
                catch { if (pbUserProfilePic != null) pbUserProfilePic.Image = null; }
            }
        }
        private void btnToggleView_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            isListViewActive = !isListViewActive;
            if (richTextBox1 != null) richTextBox1.Clear();

            if (isListViewActive)
            {
                if (btn != null) btn.Text = "Switch to Grid View";

                if (lblMonthYearHeader != null) lblMonthYearHeader.Text = "Rental List";

                if (btnPrevMonth != null) btnPrevMonth.Enabled = false;
                if (btnNextMonth != null) btnNextMonth.Enabled = false;
                if (btnToday != null) btnToday.Enabled = false;

                if (btnSearch != null) btnSearch.Enabled = true;
                if (txtSearch != null) txtSearch.Enabled = true;
                if (cmbFilters != null) cmbFilters.Enabled = true;

                PopulateRentalListView();
            }
            else
            {
                if (btn != null) btn.Text = "Switch to List View";

                if (lblMonthYearHeader != null) lblMonthYearHeader.Text = currentCalendarMonthStart.ToString("MMMM yyyy");

                if (btnPrevMonth != null) btnPrevMonth.Enabled = true;
                if (btnNextMonth != null) btnNextMonth.Enabled = true;
                if (btnToday != null) btnToday.Enabled = true;

                if (btnSearch != null) btnSearch.Enabled = false;
                if (txtSearch != null) txtSearch.Enabled = false;
                if (cmbFilters != null) cmbFilters.Enabled = false;

                if (dataGridView1 != null) dataGridView1.DataSource = null;
                ConfigureCalendarGridStructure();
                RefreshCalendarData();
            }
        }


        private void PopulateRentalListView()
        {
            if (dataGridView1 == null) return;

            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionID", HeaderText = "ID", DataPropertyName = "TransactionID", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Customer", DataPropertyName = "CustomerName", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "Item Rented", DataPropertyName = "ItemName", ReadOnly = true });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Qty", DataPropertyName = "Quantity", ReadOnly = true });

            var colStart = new DataGridViewTextBoxColumn { Name = "RentalStartDate", HeaderText = "Start Date", DataPropertyName = "RentalStartDate", ReadOnly = true };
            colStart.DefaultCellStyle.Format = "MM/dd/yyyy hh:mm tt";
            dataGridView1.Columns.Add(colStart);

            var colEnd = new DataGridViewTextBoxColumn { Name = "ExpectedReturnDate", HeaderText = "Expected Return", DataPropertyName = "ExpectedReturnDate", ReadOnly = true };
            colEnd.DefaultCellStyle.Format = "MM/dd/yyyy hh:mm tt";
            dataGridView1.Columns.Add(colEnd);

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", ReadOnly = true });

            var actionButtonCol = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "Action",
                Text = "Edit Details",
                UseColumnTextForButtonValue = true
            };
            dataGridView1.Columns.Add(actionButtonCol);

            string filterStatus = cmbFilters != null ? cmbFilters.SelectedItem?.ToString() : "All";
            string searchKeyword = txtSearch != null ? txtSearch.Text.Trim() : "";

            string listQuery = @"
                SELECT 
                    t.TransactionID,
                    c.Name AS CustomerName,
                    i.ItemName,
                    rd.Quantity,
                    t.RentalStartDate,
                    t.ExpectedReturnDate,
                    t.Status
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE t.Status IN ('Ongoing', 'Completed', 'Cancelled', 'Overdue')";

            if (filterStatus != "All" && !string.IsNullOrEmpty(filterStatus))
            {
                listQuery += " AND t.Status = @Status";
            }

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                listQuery += " AND (c.Name LIKE '%' + @Search + '%' OR i.ItemName LIKE '%' + @Search + '%')";
            }

            listQuery += " ORDER BY t.TransactionID DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(listQuery, conn))
            {
                if (filterStatus != "All" && !string.IsNullOrEmpty(filterStatus))
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

                    dataGridView1.DataSource = dt;

                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        row.Height = 28;
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            cell.Style.BackColor = Color.White;
                            cell.Style.ForeColor = Color.Black;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load List View entries: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (isListViewActive)
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name == "Action")
                {
                    int transactionId = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["TransactionID"].Value);
                    ShowBookingEditModal(transactionId);
                }
                return;
            }

            if (richTextBox1 == null) return;

            DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.Tag == null)
            {
                richTextBox1.Clear();
                return;
            }

            DateTime selectedDate = (DateTime)cell.Tag;
            DisplayBookingsInRichTextBox(selectedDate);
        }

        private void DisplayBookingsInRichTextBox(DateTime selectedDate)
        {
            if (richTextBox1 == null) return;

            richTextBox1.Clear();
            richTextBox1.Multiline = true;
            richTextBox1.WordWrap = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;

            string dayQuery = @"
            SELECT 
            c.Name AS CustomerName,
            i.ItemName,
            rd.Quantity,
            t.RentalStartDate,
            t.ExpectedReturnDate,
            t.Status
            FROM RentalTransactions t
            INNER JOIN Customers c ON t.CustomerID = c.CustomerID
            INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
            INNER JOIN Items i ON rd.ItemID = i.ItemID
            WHERE CAST(t.RentalStartDate AS DATE) = @SelectedDate
            AND t.Status IN ('Pending', 'Ongoing', 'Overdue', 'Completed', 'Cancelled');";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(dayQuery, conn))
            {
                cmd.Parameters.AddWithValue("@SelectedDate", selectedDate.Date);
                try
                {
                    conn.Open();
                    int rentalCount = 0;
                    int bookingCount = 0;

                    var tempRows = new List<dynamic>();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string status = reader["Status"].ToString().Trim();
                            if (status == "Pending") bookingCount++;
                            else rentalCount++;

                            tempRows.Add(new
                            {
                                Customer = reader["CustomerName"].ToString(),
                                Item = reader["ItemName"].ToString(),
                                Qty = reader["Quantity"].ToString(),
                                Status = status,
                                Start = Convert.ToDateTime(reader["RentalStartDate"]),
                                End = Convert.ToDateTime(reader["ExpectedReturnDate"])
                            });
                        }
                    }

                    string rentSummary = rentalCount == 1 ? "1 Rental" : $"{rentalCount} Rentals";
                    string bookSummary = bookingCount == 1 ? "1 Booking" : $"{bookingCount} Bookings";

                    AppendColoredText($"SCHEDULE SUMMARY ({selectedDate:MM/dd/yyyy})\n", Color.Black, true);
                    AppendColoredText($"{rentSummary}  |  {bookSummary}\n", Color.DarkSlateGray, true);
                    AppendColoredText(new string('=', 35) + "\n\n", Color.LightGray, false);

                    if (tempRows.Count == 0)
                    {
                        AppendColoredText("No active schedules for this day.", Color.DarkGray, false);
                        return;
                    }

                    foreach (var row in tempRows)
                    {
                        Color itemColor = Color.Black;
                        switch (row.Status)
                        {
                            case "Pending": itemColor = Color.Purple; break;
                            case "Overdue": itemColor = Color.Red; break;
                            case "Ongoing": itemColor = Color.Teal; break;
                            case "Completed": itemColor = Color.Green; break;
                            case "Cancelled": itemColor = Color.Gray; break;
                        }

                        AppendColoredText("Customer: ", Color.Black, true);
                        AppendColoredText($"{row.Customer}\n", Color.Black, false);

                        AppendColoredText("Item: ", Color.Black, true);
                        AppendColoredText($"{row.Item} ({row.Qty}x)\n", itemColor, false);

                        AppendColoredText("Start: ", Color.Black, true);
                        AppendColoredText($"{row.Start:hh:mm tt}\n", Color.Black, false);

                        AppendColoredText("Until: ", Color.Black, true);
                        AppendColoredText($"{row.End:hh:mm tt}\n", Color.Black, false);

                        AppendColoredText(new string('-', 30) + "\n", Color.LightGray, false);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not load schedule breakdown summary.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private void AppendColoredText(string text, Color color, bool isBold)
        {
            if (richTextBox1 == null) return;

            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectionColor = color;

            if (isBold)
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
            else
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);

            richTextBox1.AppendText(text);
            richTextBox1.SelectionFont = richTextBox1.Font;
        }

        private void ShowBookingEditModal(int txId)
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
            string currentStatus = "Ongoing";
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
                    MessageBox.Show("Could not open booking details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form()
            {
                Width = 520,
                Height = 480,
                Text = $"Edit Booking - ID {txId}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblInfo = new Label() { Left = 20, Top = 15, Width = 460, Font = new Font("Arial", 10, FontStyle.Bold), Text = $"Customer: {customerName}\nItem: {itemName} (Qty: {quantity})\nDates: {start:MM/dd} to {returnDate:MM/dd}" };
            lblInfo.Height = 55;

            Label lblCosts = new Label() { Left = 20, Top = 80, Width = 460, Font = new Font("Arial", 9, FontStyle.Regular), Text = $"Total Price: ₱{total:N2}   |   Deposit Left: ₱{deposit:N2}" };

            Label lblPaid = new Label() { Left = 20, Top = 120, Width = 150, Text = "Amount Paid (₱):" };
            TextBox txtPaid = new TextBox() { Left = 180, Top = 117, Width = 150, Text = paid.ToString("F2") };

            Label lblMethod = new Label() { Left = 20, Top = 160, Width = 150, Text = "Payment Method:" };
            ComboBox cmbMethod = new ComboBox() { Left = 180, Top = 157, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMethod.Items.AddRange(new string[] { "Cash", "GCash", "Partial" });
            cmbMethod.SelectedItem = method;

            Label lblStatus = new Label() { Left = 20, Top = 200, Width = 150, Text = "Rental Status:" };
            ComboBox cmbStatus = new ComboBox() { Left = 180, Top = 197, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new string[] { "Ongoing", "Completed", "Cancelled", "Overdue" });
            cmbStatus.SelectedItem = cmbStatus.Items.Contains(currentStatus) ? currentStatus : "Ongoing";

            Label lblNotes = new Label() { Left = 20, Top = 240, Width = 150, Text = "Rental Notes:" };
            TextBox txtNotesBox = new TextBox() { Left = 20, Top = 265, Width = 460, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = notes };

            Button btnSave = new Button { Text = "Save Changes", Left = 260, Top = 370, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Left = 380, Top = 370, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };

            modal.Controls.AddRange(new Control[] { lblInfo, lblCosts, lblPaid, txtPaid, lblMethod, cmbStatus, lblNotes, txtNotesBox, btnSave, btnCancel });
            modal.Controls.Add(lblStatus);
            modal.Controls.Add(cmbMethod);
            modal.AcceptButton = btnSave;

            if (modal.ShowDialog() == DialogResult.OK)
            {
                if (!decimal.TryParse(txtPaid.Text.Trim(), out decimal newPaid) || newPaid < 0)
                {
                    MessageBox.Show("Please enter a valid amount paid.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string newStatus = cmbStatus.SelectedItem.ToString();
                string newMethod = cmbMethod.SelectedItem.ToString();
                string newNotes = txtNotesBox.Text.Trim();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                if ((currentStatus == "Completed" || currentStatus == "Cancelled") && (newStatus == "Ongoing" || newStatus == "Overdue"))
                                {
                                    string checkStock = "SELECT AvailableQuantity FROM Items WHERE ItemID = @ItemID;";
                                    int available = 0;
                                    using (SqlCommand cmdStock = new SqlCommand(checkStock, conn, trans))
                                    {
                                        cmdStock.Parameters.AddWithValue("@ItemID", itemId);
                                        available = Convert.ToInt32(cmdStock.ExecuteScalar());
                                    }

                                    if (available < quantity)
                                    {
                                        MessageBox.Show($"Not enough stock available. Only {available} left in inventory.", "Stock Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        trans.Rollback();
                                        return;
                                    }

                                    string deductStock = @"
                                        UPDATE Items 
                                        SET AvailableQuantity = AvailableQuantity - @Qty,
                                            Status = CASE WHEN (AvailableQuantity - @Qty) <= 0 THEN 'Fully Booked' ELSE Status END
                                        WHERE ItemID = @ItemID;";
                                    using (SqlCommand cmdDeduct = new SqlCommand(deductStock, conn, trans))
                                    {
                                        cmdDeduct.Parameters.AddWithValue("@Qty", quantity);
                                        cmdDeduct.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdDeduct.ExecuteNonQuery();
                                    }
                                }

                                if ((currentStatus == "Ongoing" || currentStatus == "Overdue") && newStatus == "Cancelled")
                                {
                                    string returnStock = @"
                                        UPDATE Items 
                                        SET AvailableQuantity = AvailableQuantity + @Qty,
                                            Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END
                                        WHERE ItemID = @ItemID;";
                                    using (SqlCommand cmdReturn = new SqlCommand(returnStock, conn, trans))
                                    {
                                        cmdReturn.Parameters.AddWithValue("@Qty", quantity);
                                        cmdReturn.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdReturn.ExecuteNonQuery();
                                    }
                                }

                                string updateTxSql = "";
                                if ((currentStatus == "Ongoing" || currentStatus == "Overdue") && newStatus == "Completed")
                                {
                                    updateTxSql = @"
                                        UPDATE RentalTransactions 
                                        SET AmountPaid = @Paid, PaymentMethod = @Method, Status = @Status, Notes = @Notes, ActualReturnDate = GETDATE()
                                        WHERE TransactionID = @TxID;

                                        UPDATE Items 
                                        SET AvailableQuantity = AvailableQuantity + @Qty,
                                            Status = CASE WHEN Status = 'Fully Booked' THEN 'Available' ELSE Status END
                                        WHERE ItemID = @ItemID;

                                        UPDATE Customers 
                                        SET TotalRentals = TotalRentals + 1 
                                        WHERE CustomerID = @CustomerID;";
                                }
                                else
                                {
                                    updateTxSql = @"
                                        UPDATE RentalTransactions 
                                        SET AmountPaid = @Paid, PaymentMethod = @Method, Status = @Status, Notes = @Notes
                                        WHERE TransactionID = @TxID;";
                                }

                                using (SqlCommand cmdUpdate = new SqlCommand(updateTxSql, conn, trans))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@Paid", newPaid);
                                    cmdUpdate.Parameters.AddWithValue("@Method", newMethod);
                                    cmdUpdate.Parameters.AddWithValue("@Status", newStatus);
                                    cmdUpdate.Parameters.AddWithValue("@Notes", newNotes);
                                    cmdUpdate.Parameters.AddWithValue("@TxID", txId);
                                    if (updateTxSql.Contains("@Qty"))
                                    {
                                        cmdUpdate.Parameters.AddWithValue("@Qty", quantity);
                                        cmdUpdate.Parameters.AddWithValue("@ItemID", itemId);
                                        cmdUpdate.Parameters.AddWithValue("@CustomerID", customerId);
                                    }
                                    cmdUpdate.ExecuteNonQuery();
                                }

                                string logSql = @"
                                    INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                                    VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, @Desc, GETDATE());";
                                using (SqlCommand cmdLog = new SqlCommand(logSql, conn, trans))
                                {
                                    cmdLog.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                                    cmdLog.Parameters.AddWithValue("@TxID", txId);
                                    cmdLog.Parameters.AddWithValue("@Desc", $"Updated Rental {txId} via Calendar. Status changed from {currentStatus} to {newStatus}.");
                                    cmdLog.ExecuteNonQuery();
                                }

                                trans.Commit();
                                MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                PopulateRentalListView();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                MessageBox.Show("Could not save changes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Connection error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            modal.Dispose();
        }

        private void btnPrevMonth_Click(object sender, EventArgs e)
        {
            currentCalendarMonthStart = currentCalendarMonthStart.AddMonths(-1);
            RefreshCalendarData();
        }

        private void btnNextMonth_Click(object sender, EventArgs e)
        {
            currentCalendarMonthStart = currentCalendarMonthStart.AddMonths(1);
            RefreshCalendarData();
        }

        private void btnToday_Click(object sender, EventArgs e)
        {
            currentCalendarMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            RefreshCalendarData();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId));
        }

        private void btnNewRentalTransaction_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId));
        }

        private void btnInventoryManagement_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId));
        }

        private void btnRecords_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId));
        }

        private void btnBookingManagement_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId));
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
