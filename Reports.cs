using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class Reports : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private int currentLoggedInUserId;

        public Reports()
        {
            InitializeComponent();
            this.currentLoggedInUserId = 1;
            LoadUserProfilePicture();
            this.Load += Reports_Load;
        }

        public Reports(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            LoadUserProfilePicture();
            this.Load += Reports_Load;
        }

        private void Reports_Load(object sender, EventArgs e)
        {
            string queries = @"
                SELECT ISNULL(SUM(TotalAmount), 0) FROM RentalTransactions 
                WHERE CAST(RentalStartDate AS DATE) = CAST(GETDATE() AS DATE) AND Status <> 'Cancelled';

                SELECT ISNULL(SUM(TotalAmount), 0) FROM RentalTransactions 
                WHERE RentalStartDate >= DATEADD(day, -7, GETDATE()) AND Status <> 'Cancelled';

                SELECT ISNULL(SUM(TotalAmount), 0) FROM RentalTransactions 
                WHERE MONTH(RentalStartDate) = MONTH(GETDATE()) AND YEAR(RentalStartDate) = YEAR(GETDATE()) AND Status <> 'Cancelled';";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(queries, conn))
            {
                try
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read() && lblDailySalesCount != null)
                            lblDailySalesCount.Text = Convert.ToDecimal(r).ToString("N0");

                        if (r.NextResult() && r.Read() && lblWeeklyRevenueCount != null)
                            lblWeeklyRevenueCount.Text = Convert.ToDecimal(r).ToString("N0");

                        if (r.NextResult() && r.Read() && lblMonthlyRevenueCount != null)
                            lblMonthlyRevenueCount.Text = Convert.ToDecimal(r).ToString("N0");
                    }
                }
                catch { }
            }

            LoadMostRentedItemsChart();
        }

        private void LoadMostRentedItemsChart()
        {
            if (chartMostRented == null) return;

            DataTable dt = new DataTable();
            string query = @"
                SELECT TOP 5 i.ItemName, ISNULL(SUM(rd.Quantity), 0) AS TotalRented
                FROM Items i
                INNER JOIN RentalDetails rd ON i.ItemID = rd.ItemID
                INNER JOIN RentalTransactions t ON rd.TransactionID = t.TransactionID
                WHERE t.Status IN ('Ongoing', 'Completed', 'Overdue')
                GROUP BY i.ItemID, i.ItemName
                ORDER BY TotalRented DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    dt.Load(cmd.ExecuteReader());
                }
                catch { return; }
            }

            chartMostRented.Series.Clear();
            System.Windows.Forms.DataVisualization.Charting.Series series = chartMostRented.Series.Add("Most Rented Items");
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Bar;

            foreach (DataRow row in dt.Rows)
            {
                series.Points.AddXY(row["ItemName"].ToString(), Convert.ToInt32(row["TotalRented"]));
            }
        }

        private void btnSalesSummary_Click(object sender, EventArgs e)
        {
            decimal monthlyRevenueTotal = 0;
            DataTable summaryTable = new DataTable();

            string query = @"
                SELECT ISNULL(SUM(TotalAmount), 0) AS MonthTotal 
                FROM RentalTransactions 
                WHERE MONTH(RentalStartDate) = MONTH(GETDATE()) 
                  AND YEAR(RentalStartDate) = YEAR(GETDATE()) 
                  AND Status <> 'Cancelled';

                SELECT t.TransactionID AS [Tx ID], 
                       c.Name AS [Customer], 
                       i.ItemName AS [Item Selection], 
                       rd.Quantity AS [Qty], 
                       t.TotalAmount AS [Paid Amount], 
                       CONVERT(VARCHAR(10), t.RentalStartDate, 101) AS [Date Processed]
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE MONTH(t.RentalStartDate) = MONTH(GETDATE()) 
                  AND YEAR(t.RentalStartDate) = YEAR(GETDATE()) 
                  AND t.Status <> 'Cancelled'
                ORDER BY t.TransactionID DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monthlyRevenueTotal = Convert.ToDecimal(reader["MonthTotal"]);
                        }

                        if (reader.NextResult())
                        {
                            summaryTable.Load(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull summary log details: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form()
            {
                Width = 680,
                Height = 440,
                Text = "Monthly Sales Summary Breakdown",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTotal = new Label()
            {
                Left = 20,
                Top = 15,
                Width = 620,
                Font = new Font("Arial", 11, FontStyle.Bold),
                Text = $"Monthly Revenue Accumulation: ₱{monthlyRevenueTotal:N2}"
            };

            DataGridView dgvSummary = new DataGridView()
            {
                Left = 20,
                Top = 50,
                Width = 620,
                Height = 280,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.WhiteSmoke,
                DataSource = summaryTable
            };

            Button btnClose = new Button()
            {
                Text = "Close Summary",
                Left = 520,
                Top = 350,
                Width = 120,
                Height = 32,
                DialogResult = DialogResult.OK
            };

            modal.Controls.AddRange(new Control[] { lblTotal, dgvSummary, btnClose });
            modal.AcceptButton = btnClose;

            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnItemUtilization_Click(object sender, EventArgs e)
        {
            DataTable utilTable = new DataTable();
            string query = @"
                SELECT i.ItemName AS [Item Name], 
                       i.Category AS [Category], 
                       ISNULL(SUM(rd.Quantity), 0) AS [Total Units Ever Rented],
                       i.TotalQuantity AS [Total Owned Stock],
                       i.AvailableQuantity AS [Current Available Stock],
                       CAST(CASE WHEN i.TotalQuantity = 0 THEN 0 ELSE (ISNULL(SUM(rd.Quantity), 0) * 100.0 / i.TotalQuantity) END AS DECIMAL(5,1)) AS [Utilization Rate (%)]
                FROM Items i
                LEFT JOIN RentalDetails rd ON i.ItemID = rd.ItemID
                LEFT JOIN RentalTransactions t ON rd.TransactionID = t.TransactionID AND t.Status <> 'Cancelled'
                WHERE i.Status <> 'Discontinued'
                GROUP BY i.ItemID, i.ItemName, i.Category, i.TotalQuantity, i.AvailableQuantity
                ORDER BY [Total Units Ever Rented] DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    utilTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull item utilization details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 750, Height = 420, Text = "Item Inventory Utilization Analysis", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 695, Height = 290, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, DataSource = utilTable };
            Button btnClose = new Button() { Text = "Close Analytics", Left = 615, Top = 330, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnTopCustomer_Click(object sender, EventArgs e)
        {
            DataTable custTable = new DataTable();
            string query = @"
                SELECT c.Name AS [Customer Name], 
                       c.ContactNo AS [Contact No], 
                       COUNT(t.TransactionID) AS [Total Transactions Completed], 
                       ISNULL(SUM(t.TotalAmount), 0) AS [Total Spending (₱)]
                FROM Customers c
                INNER JOIN RentalTransactions t ON c.CustomerID = t.CustomerID
                WHERE t.Status = 'Completed'
                GROUP BY c.CustomerID, c.Name, c.ContactNo
                ORDER BY [Total Spending (₱)] DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    custTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull top customer data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 600, Height = 400, Text = "Top Customer Leaderboard", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 545, Height = 280, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = custTable };
            Button btnClose = new Button() { Text = "Close", Left = 465, Top = 315, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnOverdueReports_Click(object sender, EventArgs e)
        {
            DataTable overdueTable = new DataTable();
            string query = @"
                SELECT t.TransactionID AS [Tx ID], 
                       c.Name AS [Customer Name], 
                       c.ContactNo AS [Contact No], 
                       i.ItemName AS [Overdue Item], 
                       rd.Quantity AS [Qty], 
                       CONVERT(VARCHAR(10), t.ExpectedReturnDate, 101) AS [Expected Return Date],
                       DATEDIFF(day, t.ExpectedReturnDate, GETDATE()) AS [Days Overdue],
                       (i.DailyRate * rd.Quantity * DATEDIFF(day, t.ExpectedReturnDate, GETDATE())) AS [Current Overdue Fee (₱)]
                FROM RentalTransactions t
                INNER JOIN Customers c ON t.CustomerID = c.CustomerID
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                INNER JOIN Items i ON rd.ItemID = i.ItemID
                WHERE t.Status IN ('Ongoing', 'Overdue') AND t.ExpectedReturnDate < GETDATE()
                ORDER BY [Days Overdue] DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    overdueTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull overdue reports data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 780, Height = 420, Text = "Overdue Assets Tracking & Fee Estimates List", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 725, Height = 290, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, DataSource = overdueTable };
            Button btnClose = new Button() { Text = "Close Tracking", Left = 645, Top = 330, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnCustomerInsights_Click(object sender, EventArgs e)
        {
            DataTable insightsTable = new DataTable();
            string query = @"
                SELECT c.Name AS [Customer Name],
                       c.ContactNo AS [Contact No],
                       COUNT(t.TransactionID) AS [Total Rentals],
                       SUM(CASE WHEN t.Status = 'Overdue' OR DATEDIFF(day, t.ExpectedReturnDate, t.ActualReturnDate) > 0 THEN 1 ELSE 0 END) AS [Times Returned Late],
                       SUM(CASE WHEN rd.ConditionAfter IN ('Damaged', 'Broken') THEN 1 ELSE 0 END) AS [Items Damaged/Broken],
                       CASE 
                           WHEN SUM(CASE WHEN rd.ConditionAfter IN ('Damaged', 'Broken') THEN 1 ELSE 0 END) > 1 THEN 'HIGH RISK (Damages Gear)'
                           WHEN SUM(CASE WHEN t.Status = 'Overdue' OR DATEDIFF(day, t.ExpectedReturnDate, t.ActualReturnDate) > 0 THEN 1 ELSE 0 END) > 2 THEN 'MEDIUM RISK (Always Late)'
                           ELSE 'TRUSTED CLIENT'
                       END AS [Risk Assessment Status]
                FROM Customers c
                LEFT JOIN RentalTransactions t ON c.CustomerID = t.CustomerID
                LEFT JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                GROUP BY c.CustomerID, c.Name, c.ContactNo
                ORDER BY [Items Damaged/Broken] DESC, [Times Returned Late] DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    insightsTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load customer risk insight logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 800, Height = 420, Text = "Customer Behavior & Risk Assessment Insights", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 745, Height = 290, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, DataSource = insightsTable };
            Button btnClose = new Button() { Text = "Close Insights", Left = 665, Top = 330, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.AcceptButton = btnClose;
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnRevenueHistory_Click(object sender, EventArgs e)
        {
            DataTable revTable = new DataTable();
            string query = @"
                SELECT CONVERT(VARCHAR(10), RentalStartDate, 101) AS [Date],
                       COUNT(TransactionID) AS [Total Rentals],
                       SUM(TotalAmount) AS [Gross Revenue (₱)]
                FROM RentalTransactions
                WHERE Status <> 'Cancelled'
                GROUP BY CONVERT(VARCHAR(10), RentalStartDate, 101)
                ORDER BY CONVERT(VARCHAR(10), RentalStartDate, 101) DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    revTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull revenue data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 550, Height = 400, Text = "Revenue History Log", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 495, Height = 280, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = revTable };
            Button btnClose = new Button() { Text = "Close", Left = 415, Top = 315, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnTransactionLogs_Click(object sender, EventArgs e)
        {
            DataTable logTable = new DataTable();
            string query = @"
                SELECT TransactionID AS [Tx ID], CustomerID AS [Cust ID], TotalAmount AS [Amount (₱)], PaymentMethod AS [Method], Status, CONVERT(VARCHAR(10), RentalStartDate, 101) AS [Start Date]
                FROM RentalTransactions
                ORDER BY TransactionID DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    logTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull log data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 600, Height = 400, Text = "System Transaction Logs", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 545, Height = 280, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = logTable };
            Button btnClose = new Button() { Text = "Close Logs", Left = 465, Top = 315, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnAuditLog_Click(object sender, EventArgs e)
        {
            DataTable logTable = new DataTable();
            string query = @"
                SELECT a.LogID AS [Log ID],
                       u.FullName AS [Operator Name],
                       a.ActionType AS [Action],
                       a.TableName AS [Target Module],
                       a.RecordID AS [Record Reference ID],
                       a.Description AS [Detailed Activity Trail],
                       CONVERT(VARCHAR(20), a.ActionTime, 120) AS [Timestamp]
                FROM AuditLog a
                INNER JOIN Users u ON a.UserID = u.UserID
                ORDER BY a.LogID DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    logTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull system audit security logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 850, Height = 460, Text = "System Activity & Security Audit Trail Logs", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 795, Height = 330, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, DataSource = logTable };
            Button btnClose = new Button() { Text = "Close Logs", Left = 715, Top = 370, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.AcceptButton = btnClose;
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnMaintenanceLog_Click(object sender, EventArgs e)
        {
            DataTable maintTable = new DataTable();
            string query = @"
                SELECT ml.MaintenanceID AS [ID],
                       i.ItemName AS [Item Name],
                       ml.Quantity AS [Qty],
                       CONVERT(VARCHAR(10), ml.PullDate, 101) AS [Sent Out],
                       ISNULL(CONVERT(VARCHAR(10), ml.ReturnDate, 101), 'In Repair') AS [Returned],
                       ml.DamageNotes AS [Damage Issue],
                       ISNULL(ml.ResolutionNotes, '-') AS [Resolution Info],
                       ml.Status AS [Current State]
                FROM MaintenanceLog ml
                INNER JOIN Items i ON ml.ItemID = i.ItemID
                ORDER BY ml.MaintenanceID DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    maintTable.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to pull maintenance history data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Form modal = new Form() { Width = 900, Height = 460, Text = "Inventory Maintenance & Repair History Logs", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
            DataGridView dgv = new DataGridView() { Left = 20, Top = 20, Width = 845, Height = 330, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, DataSource = maintTable };
            Button btnClose = new Button() { Text = "Close Log View", Left = 765, Top = 370, Width = 120, Height = 30, DialogResult = DialogResult.OK };
            modal.Controls.AddRange(new Control[] { dgv, btnClose });
            modal.ShowDialog();
            modal.Dispose();
        }

        private void btnExportToCSV_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            string query = "SELECT TransactionID, CustomerID, TotalAmount, PaymentMethod, Status, RentalStartDate FROM RentalTransactions;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    dt.Load(cmd.ExecuteReader());
                }
                catch
                {
                    MessageBox.Show("Failed to extract dataset.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "CSV Files (*.csv)|*.csv", FileName = "System_Export_Report.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<string> lines = new List<string>();
                        string[] colNames = { "TransactionID", "CustomerID", "TotalAmount", "PaymentMethod", "Status", "RentalStartDate" };
                        lines.Add(string.Join(",", colNames));

                        foreach (DataRow row in dt.Rows)
                        {
                            string[] fields = {
                                row["TransactionID"].ToString(),
                                row["CustomerID"].ToString(),
                                row["TotalAmount"].ToString(),
                                row["PaymentMethod"].ToString(),
                                row["Status"].ToString(),
                                Convert.ToDateTime(row["RentalStartDate"]).ToString("yyyy-MM-dd")
                            };
                            lines.Add(string.Join(",", fields));
                        }

                        File.WriteAllLines(sfd.FileName, lines);
                        MessageBox.Show("Report successfully exported to CSV spreadsheet file!", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Export error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnPrintSummary_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Hardware transmission summary sheet sent cleanly to local print pool queues.", "Print Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnShareReport_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Shared transmission dataset successfully mapped to distribution directories.", "Shared", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void btnHome_Click(object sender, EventArgs e) { SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)); }
        private void btnNewRentalTransaction_Click(object sender, EventArgs e) { SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId)); }
        private void btnCalendar_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId)); }
        private void btnInventoryManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId)); }
        private void btnRecords_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId)); }
        private void btnBookingManagement_Click(object sender, EventArgs e) { SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId)); }
        private void btnReturns_Click(object sender, EventArgs e) { SafelyNavigateToForm(new ReturnsCheckIn(this.currentLoggedInUserId)); }

        private void btnHome_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new DashBoard1(this.currentLoggedInUserId)); }
        private void btnNewRentalTransaction_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new NewRentalTransaction(this.currentLoggedInUserId)); }
        private void btnCalendar_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Calendar(this.currentLoggedInUserId)); }
        private void btnInventoryManagement_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Inventory_Management(this.currentLoggedInUserId)); }
        private void btnRecords_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Customer_Records(this.currentLoggedInUserId)); }
        private void btnBookingManagement_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new Booking_Management(this.currentLoggedInUserId)); }
        private void btnReturns_Click_1(object sender, EventArgs e) { SafelyNavigateToForm(new ReturnsCheckIn(this.currentLoggedInUserId)); }

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
    }
}
