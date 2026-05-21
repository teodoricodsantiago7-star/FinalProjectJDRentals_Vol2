using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class NewRentalTransaction : Form
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=FinalProjectJDRENTALS;Trusted_Connection=True;";
        private Dictionary<int, decimal> itemRates = new Dictionary<int, decimal>();
        private int currentCustomerId = -1;
        private int currentLoggedInUserId = 1;
        private int autoFillTransactionId = -1;
        private bool isInitializingForm = false;

        public NewRentalTransaction(int loggedInUserId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            this.autoFillTransactionId = -1;

            SetupFormDefaults();
            LoadUserProfilePicture();
            WireUpDefaultSearchButtons();
        }

        public NewRentalTransaction(int loggedInUserId, int acceptedTxId)
        {
            InitializeComponent();
            this.currentLoggedInUserId = loggedInUserId > 0 ? loggedInUserId : 1;
            this.autoFillTransactionId = acceptedTxId;

            SetupFormDefaults();
            LoadUserProfilePicture();
            WireUpDefaultSearchButtons();
            ExecuteBookingAutoFillSequence();
        }

        private void WireUpDefaultSearchButtons()
        {
            if (btnSearch != null)
            {
                btnSearch.Click -= btnSearch_Click;
                btnSearch.Click += btnSearch_Click;
            }

            if (btnSearchItem != null)
            {
                btnSearchItem.Click -= btnSearchItem_Click;
                btnSearchItem.Click += btnSearchItem_Click;
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

                                if (pbUserProfilePic != null && reader["ImagePath"] != DBNull.Value)
                                {
                                    string imagePath = reader["ImagePath"].ToString();
                                    if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
                                    {
                                        pbUserProfilePic.Image?.Dispose();
                                        byte[] bytes = File.ReadAllBytes(imagePath);
                                        using (MemoryStream ms = new MemoryStream(bytes))
                                        {
                                            pbUserProfilePic.Image = Image.FromStream(ms);
                                        }
                                        pbUserProfilePic.SizeMode = PictureBoxSizeMode.Zoom;
                                    }
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

        private void SetupFormDefaults()
        {
            isInitializingForm = true;

            LoadCustomerDropdown();
            LoadInventoryItems();
            ResetInputFields();

            dtpStartDate.Format = DateTimePickerFormat.Short;
            dtpStartDate.ShowUpDown = false;

            dtpExpectedReturnDate.Format = DateTimePickerFormat.Short;
            dtpExpectedReturnDate.ShowUpDown = false;

            dtpStartTime.Format = DateTimePickerFormat.Time;
            dtpStartTime.ShowUpDown = true;

            dtpExpectedReturnTime.Format = DateTimePickerFormat.Time;
            dtpExpectedReturnTime.ShowUpDown = true;

            txtDailyRate.ReadOnly = true;
            txtSubTotal.ReadOnly = true;
            txtBalanceDue.ReadOnly = true;

            if (numQuantity != null)
            {
                numQuantity.Minimum = 1;
                numQuantity.Maximum = 999999;
            }

            if (numDeposit != null)
            {
                numDeposit.Minimum = 0;
                numDeposit.Maximum = 999999;
            }

            if (txtFinancialTotal != null) txtFinancialTotal.ReadOnly = true;
            if (txtFinancialAmountPaid != null) txtFinancialAmountPaid.ReadOnly = true;
            if (txtFinancialChange != null) txtFinancialChange.ReadOnly = true;

            if (txtCustNo != null) txtCustNo.ReadOnly = true;
            if (txtCustEmail != null) txtCustEmail.ReadOnly = true;
            if (txtCustAddress != null) txtCustAddress.ReadOnly = true;

            if (txtItemAvailableQty != null) txtItemAvailableQty.ReadOnly = true;
            if (txtItemCategory != null) txtItemCategory.ReadOnly = true;
            if (txtItemDescription != null) txtItemDescription.ReadOnly = true;

            WireUpCalculationEvents();

            isInitializingForm = false;
        }

        private void WireUpCalculationEvents()
        {
            if (cmbCustomerName != null)
            {
                cmbCustomerName.SelectedIndexChanged -= cmbCustomerName_SelectedIndexChanged;
                cmbCustomerName.SelectedIndexChanged += cmbCustomerName_SelectedIndexChanged;
            }

            if (cmbItem != null)
            {
                cmbItem.SelectedIndexChanged -= cmbItem_SelectedIndexChanged;
                cmbItem.SelectedIndexChanged += cmbItem_SelectedIndexChanged;
            }

            if (numQuantity != null)
            {
                numQuantity.ValueChanged -= numQuantity_ValueChanged;
                numQuantity.ValueChanged += numQuantity_ValueChanged;
            }

            if (numDeposit != null)
            {
                numDeposit.ValueChanged -= numDeposit_ValueChanged;
                numDeposit.ValueChanged += numDeposit_ValueChanged;
            }

            if (txtAmountPaid != null)
            {
                txtAmountPaid.TextChanged -= txtAmountPaid_TextChanged;
                txtAmountPaid.TextChanged += txtAmountPaid_TextChanged;
            }

            if (txtFinancialAmountPaid != null)
            {
                txtFinancialAmountPaid.TextChanged -= txtFinancialAmountPaid_TextChanged;
                txtFinancialAmountPaid.TextChanged += txtFinancialAmountPaid_TextChanged;
            }

            if (dtpStartDate != null) dtpStartDate.ValueChanged += (s, e) => CalculateTotals();
            if (dtpStartTime != null) dtpStartTime.ValueChanged += (s, e) => CalculateTotals();
            if (dtpExpectedReturnDate != null) dtpExpectedReturnDate.ValueChanged += (s, e) => CalculateTotals();
            if (dtpExpectedReturnTime != null) dtpExpectedReturnTime.ValueChanged += (s, e) => CalculateTotals();
        }

        private void LoadCustomerDropdown()
        {
            string query = "SELECT CustomerID, Name FROM Customers ORDER BY Name ASC;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        cmbCustomerName.DataSource = dt;
                        cmbCustomerName.DisplayMember = "Name";
                        cmbCustomerName.ValueMember = "CustomerID";
                        cmbCustomerName.SelectedIndex = -1;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load customers: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void cmbCustomerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCustomerName == null || cmbCustomerName.SelectedIndex == -1 || cmbCustomerName.SelectedValue == null)
            {
                if (txtCustNo != null) txtCustNo.Clear();
                if (txtCustEmail != null) txtCustEmail.Clear();
                if (txtCustAddress != null) txtCustAddress.Clear();
                return;
            }

            if (!int.TryParse(cmbCustomerName.SelectedValue.ToString(), out int customerId)) return;

            string query = "SELECT ContactNo, Email, Address FROM Customers WHERE CustomerID = @CustomerID;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (txtCustNo != null) txtCustNo.Text = reader["ContactNo"] != DBNull.Value ? reader["ContactNo"].ToString() : "";
                            if (txtCustEmail != null) txtCustEmail.Text = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "";
                            if (txtCustAddress != null) txtCustAddress.Text = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : "";
                        }
                    }
                }
                catch { }
            }
        }
        private void LoadInventoryItems()
        {
            string query = @"
                SELECT ItemID, ItemName, DailyRate, AvailableQuantity, Status 
                FROM Items 
                WHERE Status <> 'Discontinued';";

            itemRates.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        DataTable dt = new DataTable();
                        dt.Columns.Add("ItemID", typeof(int));
                        dt.Columns.Add("ItemName", typeof(string));

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = Convert.ToInt32(reader["ItemID"]);
                                decimal rate = Convert.ToDecimal(reader["DailyRate"]);
                                string name = reader["ItemName"].ToString();
                                int avail = Convert.ToInt32(reader["AvailableQuantity"]);
                                string status = reader["Status"].ToString().Trim();

                                itemRates.Add(id, rate);

                                bool isLinkedToThisBooking = (this.autoFillTransactionId > 0);

                                if (avail > 0)
                                {
                                    if (status.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
                                    {
                                        dt.Rows.Add(id, $"{name} (Partially in Maintenance | {avail} Available)");
                                    }
                                    else
                                    {
                                        dt.Rows.Add(id, name);
                                    }
                                }
                                else
                                {
                                    if (isLinkedToThisBooking)
                                    {
                                        dt.Rows.Add(id, $"{name} (Reserved for this Booking)");
                                    }
                                    else
                                    {
                                        dt.Rows.Add(id, $"{name} (Out of Stock / Fully Booked)");
                                    }
                                }
                            }
                        }

                        cmbItem.DataSource = dt;
                        cmbItem.DisplayMember = "ItemName";
                        cmbItem.ValueMember = "ItemID";
                        cmbItem.SelectedIndex = -1;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load inventory items: " + ex.Message, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void cmbItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializingForm) return;

            if (cmbItem == null || cmbItem.SelectedIndex == -1 || cmbItem.SelectedValue == null)
            {
                txtDailyRate.Text = "0.00";
                txtSubTotal.Text = "0.00";
                if (txtItemAvailableQty != null) txtItemAvailableQty.Clear();
                if (txtItemCategory != null) txtItemCategory.Clear();
                if (txtItemDescription != null) txtItemDescription.Clear();
                CalculateBalance();
                return;
            }

            if (cmbItem.SelectedValue is DataRowView || !int.TryParse(cmbItem.SelectedValue.ToString(), out int itemId))
            {
                return;
            }

            string query = "SELECT AvailableQuantity, Category, Description FROM Items WHERE ItemID = @ItemID;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ItemID", itemId);
                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (txtItemAvailableQty != null) txtItemAvailableQty.Text = reader["AvailableQuantity"].ToString();
                            if (txtItemCategory != null) txtItemCategory.Text = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : "";
                            if (txtItemDescription != null) txtItemDescription.Text = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                        }
                    }
                }
                catch { }
            }

            CalculateTotals();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            Form customerSearchModal = new Form()
            {
                Width = 460,
                Height = 360,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Search & Manage Customers",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblPrompt = new Label { Text = "Enter Customer Name:", Location = new Point(20, 20), Size = new Size(130, 20) };
            TextBox txtSearchCustName = new TextBox { Location = new Point(160, 20), Size = new Size(260, 20) };
            Label lblResultsLabel = new Label { Text = "Matching Results Found:", Location = new Point(20, 55), Size = new Size(200, 20) };
            ListBox lstCustomersFound = new ListBox { Location = new Point(20, 80), Size = new Size(400, 160) };

            Button btnSelect = new Button { Text = "Select", Location = new Point(20, 260), Size = new Size(110, 32), DialogResult = DialogResult.OK };
            Button btnAddNew = new Button { Text = "Add New Customer", Location = new Point(140, 260), Size = new Size(160, 32) };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(310, 260), Size = new Size(110, 32), DialogResult = DialogResult.Cancel };

            customerSearchModal.Controls.AddRange(new Control[] { lblPrompt, txtSearchCustName, lblResultsLabel, lstCustomersFound, btnSelect, btnAddNew, btnCancel });
            customerSearchModal.AcceptButton = btnSelect;

            txtSearchCustName.TextChanged += (s, ev) =>
            {
                string keyword = txtSearchCustName.Text.Trim();
                if (keyword.Length < 1)
                {
                    lstCustomersFound.DataSource = null;
                    lstCustomersFound.Items.Clear();
                    return;
                }

                string query = "SELECT CustomerID, Name, ContactNo FROM Customers WHERE Name LIKE '%' + @Keyword + '%' ORDER BY Name ASC;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Keyword", keyword);
                    try
                    {
                        conn.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        DataTable displayTable = new DataTable();
                        displayTable.Columns.Add("CustomerID", typeof(int));
                        displayTable.Columns.Add("DisplayString", typeof(string));

                        foreach (DataRow row in dt.Rows)
                        {
                            string contact = row["ContactNo"] != DBNull.Value ? row["ContactNo"].ToString() : "No Contact";
                            string customerDisplay = $"{row["Name"]} ({contact})";
                            displayTable.Rows.Add(row["CustomerID"], customerDisplay);
                        }

                        lstCustomersFound.DataSource = displayTable;
                        lstCustomersFound.DisplayMember = "DisplayString";
                        lstCustomersFound.ValueMember = "CustomerID";
                    }
                    catch { }
                }
            };

            btnAddNew.Click += (s, ev) =>
            {
                string currentTypedName = txtSearchCustName.Text.Trim();
                customerSearchModal.Close();
                CreateNewCustomerInlineModal(currentTypedName);
            };

            if (customerSearchModal.ShowDialog() == DialogResult.OK)
            {
                if (lstCustomersFound.SelectedValue != null && int.TryParse(lstCustomersFound.SelectedValue.ToString(), out int targetCustomerId))
                {
                    cmbCustomerName.SelectedValue = targetCustomerId;
                }
                else
                {
                    MessageBox.Show("Please choose a customer from the results list or create a new profile.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnSearchItem_Click(object sender, EventArgs e)
        {
            Form itemSearchModal = new Form()
            {
                Width = 450,
                Height = 320,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Search Inventory Items",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblPrompt = new Label { Text = "Enter Item Name:", Location = new Point(20, 20), Size = new Size(130, 20) };
            TextBox txtSearchItemName = new TextBox { Location = new Point(160, 20), Size = new Size(250, 20) };
            Label lblResultsLabel = new Label { Text = "Matching Results Found:", Location = new Point(20, 55), Size = new Size(200, 20) };
            ListBox lstItemsFound = new ListBox { Location = new Point(20, 80), Size = new Size(390, 140) };

            Button btnSelect = new Button { Text = "Select", Location = new Point(200, 235), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(310, 235), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };

            itemSearchModal.Controls.AddRange(new Control[] { lblPrompt, txtSearchItemName, lblResultsLabel, lstItemsFound, btnSelect, btnCancel });
            itemSearchModal.AcceptButton = btnSelect;

            txtSearchItemName.TextChanged += (s, ev) =>
            {
                string keyword = txtSearchItemName.Text.Trim();
                if (keyword.Length < 1)
                {
                    lstItemsFound.DataSource = null;
                    lstItemsFound.Items.Clear();
                    return;
                }

                string query = @"
                    SELECT ItemID, ItemName, AvailableQuantity, DailyRate, Status
                    FROM Items 
                    WHERE ItemName LIKE '%' + @Keyword + '%' AND Status <> 'Discontinued'
                    ORDER BY ItemName ASC;";

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Keyword", keyword);
                    try
                    {
                        conn.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        DataTable displayTable = new DataTable();
                        displayTable.Columns.Add("ItemID", typeof(int));
                        displayTable.Columns.Add("DisplayString", typeof(string));

                        foreach (DataRow row in dt.Rows)
                        {
                            int itemId = Convert.ToInt32(row["ItemID"]);
                            int availQty = Convert.ToInt32(row["AvailableQuantity"]);
                            string statusStr = row["Status"].ToString().Trim();
                            string stockStatus = "";

                            if (availQty > 0)
                            {
                                if (statusStr.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
                                {
                                    stockStatus = $"In Maintenance | {availQty} Available";
                                }
                                else
                                {
                                    stockStatus = $"Available: {availQty}";
                                }
                            }
                            else
                            {
                                if (this.autoFillTransactionId > 0)
                                {
                                    stockStatus = "Reserved for this Booking";
                                }
                                else
                                {
                                    stockStatus = "Fully Booked";
                                }
                            }

                            string itemDisplay = $"{row["ItemName"]} ({stockStatus} | ₱{Convert.ToDecimal(row["DailyRate"]):N2}/day)";
                            displayTable.Rows.Add(itemId, itemDisplay);
                        }

                        lstItemsFound.DataSource = displayTable;
                        lstItemsFound.DisplayMember = "DisplayString";
                        lstItemsFound.ValueMember = "ItemID";
                    }
                    catch { }
                }
            };

            if (itemSearchModal.ShowDialog() == DialogResult.OK)
            {
                if (lstItemsFound.SelectedValue != null && int.TryParse(lstItemsFound.SelectedValue.ToString(), out int targetItemId))
                {
                    cmbItem.SelectedValue = targetItemId;
                }
                else
                {
                    MessageBox.Show("Please select an item from the results list.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void CreateNewCustomerInlineModal(string defaultName)
        {
            Form customerForm = new Form()
            {
                Width = 400,
                Height = 280,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Register New Customer",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblName = new Label { Text = "Full Name:", Location = new Point(20, 20), Size = new Size(100, 20) };
            TextBox txtName = new TextBox { Text = defaultName, Location = new Point(140, 20), Size = new Size(220, 20) };
            Label lblContact = new Label { Text = "Contact Number:", Location = new Point(20, 60), Size = new Size(100, 20) };
            TextBox txtContact = new TextBox { Location = new Point(140, 60), Size = new Size(220, 20) };
            Label lblAddress = new Label { Text = "Home Address:", Location = new Point(20, 100), Size = new Size(100, 20) };
            TextBox txtAddress = new TextBox { Location = new Point(140, 100), Size = new Size(220, 20) };
            Label lblEmail = new Label { Text = "Email Address:", Location = new Point(20, 140), Size = new Size(100, 20) };
            TextBox txtEmail = new TextBox { Location = new Point(140, 140), Size = new Size(220, 20) };

            Button btnSave = new Button { Text = "Register", Location = new Point(140, 190), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(260, 190), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };

            customerForm.Controls.AddRange(new Control[] { lblName, txtName, lblContact, txtContact, lblAddress, txtAddress, lblEmail, txtEmail, btnSave, btnCancel });
            customerForm.AcceptButton = btnSave;

            if (customerForm.ShowDialog() == DialogResult.OK)
            {
                string customerName = txtName.Text.Trim();
                string contact = txtContact.Text.Trim();
                string address = txtAddress.Text.Trim();
                string email = txtEmail.Text.Trim();

                if (string.IsNullOrWhiteSpace(customerName))
                {
                    MessageBox.Show("Customer Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string insertQuery = @"
                    INSERT INTO Customers (Name, ContactNo, Address, Email, TotalRentals, CreatedAt)
                    VALUES (@Name, @Contact, @Address, @Email, 0, GETDATE());";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", customerName);
                        cmd.Parameters.AddWithValue("@Contact", string.IsNullOrEmpty(contact) ? DBNull.Value : (object)contact);
                        cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(address) ? DBNull.Value : (object)address);
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)email);

                        try
                        {
                            conn.Open();
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("New customer profile registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            LoadCustomerDropdown();
                            cmbCustomerName.Text = customerName;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Registration failed: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void numQuantity_ValueChanged(object sender, EventArgs e)
        {
            CalculateTotals();
        }

        private void numDeposit_ValueChanged(object sender, EventArgs e)
        {
            CalculateBalance();
        }

        private void txtAmountPaid_TextChanged(object sender, EventArgs e)
        {
            if (txtFinancialAmountPaid != null && txtAmountPaid != null)
            {
                txtFinancialAmountPaid.TextChanged -= txtFinancialAmountPaid_TextChanged;
                txtFinancialAmountPaid.Text = txtAmountPaid.Text;
                txtFinancialAmountPaid.TextChanged += txtFinancialAmountPaid_TextChanged;
            }
            CalculateBalance();
        }

        private DateTime GetCombinedStartDateTime()
        {
            if (dtpStartDate == null || dtpStartTime == null) return DateTime.Now;

            DateTime datePart = dtpStartDate.Value.Date;
            TimeSpan timePart = dtpStartTime.Value.TimeOfDay;

            return datePart.Add(timePart);
        }

        private DateTime GetCombinedEndDateTime()
        {
            if (dtpExpectedReturnDate == null || dtpExpectedReturnTime == null) return DateTime.Now.AddDays(1);

            DateTime datePart = dtpExpectedReturnDate.Value.Date;
            TimeSpan timePart = dtpExpectedReturnTime.Value.TimeOfDay;

            return datePart.Add(timePart);
        }

        private void CalculateTotals()
        {
            if (cmbItem == null || cmbItem.SelectedIndex == -1 || cmbItem.SelectedValue == null) return;

            if (!int.TryParse(cmbItem.SelectedValue.ToString(), out int itemId)) return;

            if (itemRates != null && itemRates.TryGetValue(itemId, out decimal dailyRate))
            {
                txtDailyRate.Text = dailyRate.ToString("F2");
            }

            if (decimal.TryParse(txtDailyRate.Text, out decimal finalRate))
            {
                int qty = numQuantity != null ? (int)numQuantity.Value : 1;
                DateTime start = GetCombinedStartDateTime();
                DateTime end = GetCombinedEndDateTime();

                double totalHours = (end - start).TotalHours;
                int days = (int)Math.Ceiling(totalHours / 24.0);
                if (days <= 0) days = 1;

                decimal subTotal = finalRate * qty * days;

                if (txtSubTotal != null)
                {
                    txtSubTotal.Text = subTotal.ToString("F2");
                }

                CalculateBalance();
            }
        }

        private void CalculateBalance()
        {
            if (txtSubTotal == null || txtBalanceDue == null) return;

            if (!decimal.TryParse(txtSubTotal.Text, out decimal subTotal)) subTotal = 0;

            decimal deposit = numDeposit != null ? numDeposit.Value : 0;
            string rawPaidText = txtAmountPaid != null ? txtAmountPaid.Text.Trim() : "0.00";
            decimal amountPaid = 0;

            if (!string.IsNullOrWhiteSpace(rawPaidText) && !decimal.TryParse(rawPaidText, out amountPaid))
            {
                txtBalanceDue.Text = "Invalid Amount";
                return;
            }

            decimal balanceDue = (subTotal + deposit) - amountPaid;
            if (balanceDue < 0) balanceDue = 0;
            txtBalanceDue.Text = balanceDue.ToString("F2");

            decimal grossTotal = subTotal + deposit;
            if (txtFinancialTotal != null) txtFinancialTotal.Text = grossTotal.ToString("F2");

            if (txtFinancialAmountPaid != null) txtFinancialAmountPaid.Text = amountPaid.ToString("F2");

            decimal change = amountPaid - grossTotal;
            if (change < 0) change = 0;
            if (txtFinancialChange != null) txtFinancialChange.Text = change.ToString("F2");
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cmbCustomerName.SelectedIndex == -1 || cmbCustomerName.SelectedValue == null)
            {
                MessageBox.Show("Please choose a customer first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbItem.SelectedIndex == -1 || cmbItem.SelectedValue == null)
            {
                MessageBox.Show("Please choose an item first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int quantity = (int)numQuantity.Value;
            if (quantity <= 0)
            {
                MessageBox.Show("Quantity must be 1 or more.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime finalStartDateTime = GetCombinedStartDateTime();
            DateTime finalEndDateTime = GetCombinedEndDateTime();
            if (finalEndDateTime < finalStartDateTime)
            {
                MessageBox.Show("Return date cannot be earlier than the start date.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtSubTotal.Text, out decimal totalAmount)) totalAmount = 0;

            decimal amountPaid = 0;
            if (!string.IsNullOrWhiteSpace(txtAmountPaid.Text) && !decimal.TryParse(txtAmountPaid.Text.Trim(), out amountPaid))
            {
                MessageBox.Show("Please enter a valid amount paid.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int customerId = Convert.ToInt32(cmbCustomerName.SelectedValue);
            int itemId = Convert.ToInt32(cmbItem.SelectedValue);
            string paymentMethod = rbCash.Checked ? "Cash" : rbGCash.Checked ? "GCash" : "Partial";

            decimal depositAmount = numDeposit != null ? numDeposit.Value : 0;
            decimal grossTotalBill = totalAmount + depositAmount;

            if (paymentMethod == "Cash" || paymentMethod == "GCash")
            {
                if (amountPaid < grossTotalBill)
                {
                    MessageBox.Show($"Insufficient payment.\n\nThe selected payment method '{paymentMethod}' requires full payment of ₱{grossTotalBill:N2}.\n\nProvided amount: ₱{amountPaid:N2}.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (paymentMethod == "Partial")
            {
                if (amountPaid <= 0)
                {
                    MessageBox.Show("Please enter a valid amount paid for partial payment.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (amountPaid >= grossTotalBill)
                {
                    MessageBox.Show($"The amount paid covers the full bill (₱{grossTotalBill:N2}).\n\nPlease select 'Cash' or 'GCash' instead of 'Partial'.", "Payment Method Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            string confirmationPrompt = $"Are you sure you want to save this rental transaction?\n\n" +
                                         $"Customer: {cmbCustomerName.Text}\n" +
                                         $"Item: {cmbItem.Text} ({quantity}x)\n" +
                                         $"Total Amount Due: ₱{grossTotalBill:N2}\n" +
                                         $"Amount Paid: ₱{amountPaid:N2} ({paymentMethod})";

            DialogResult saveConfirmation = MessageBox.Show(confirmationPrompt, "Confirm Transaction", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (saveConfirmation != DialogResult.Yes) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    if (this.autoFillTransactionId <= 0)
                    {
                        string checkStockQuery = "SELECT AvailableQuantity FROM Items WHERE ItemID = @ItemID;";
                        using (SqlCommand cmd = new SqlCommand(checkStockQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                            int available = Convert.ToInt32(cmd.ExecuteScalar());

                            if (available < quantity)
                            {
                                MessageBox.Show($"Not enough stock available. Only {available} items left.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                transaction.Rollback();
                                return;
                            }
                        }
                    }

                    int currentTransactionId = this.autoFillTransactionId;

                    if (this.autoFillTransactionId > 0)
                    {
                        string updateTxQuery = @"
                    UPDATE RentalTransactions 
                    SET RentalStartDate = @StartDate,
                        ExpectedReturnDate = @ExpectedReturnDate,
                        TotalAmount = @TotalAmount,
                        DepositAmount = @DepositAmount,
                        AmountPaid = @AmountPaid,
                        PaymentMethod = @PaymentMethod,
                        Status = 'Ongoing',
                        Notes = @Notes
                    WHERE TransactionID = @TxID;";

                        using (SqlCommand cmd = new SqlCommand(updateTxQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TxID", currentTransactionId);
                            cmd.Parameters.AddWithValue("@StartDate", finalStartDateTime);
                            cmd.Parameters.AddWithValue("@ExpectedReturnDate", finalEndDateTime);
                            cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                            cmd.Parameters.AddWithValue("@DepositAmount", depositAmount);
                            cmd.Parameters.AddWithValue("@AmountPaid", amountPaid);
                            cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                            cmd.Parameters.AddWithValue("@Notes", txtNotes.Text.Trim());

                            cmd.ExecuteNonQuery();
                        }

                        string updateDetailsQuery = "UPDATE RentalDetails SET Subtotal = @Subtotal WHERE TransactionID = @TxID AND ItemID = @ItemID;";
                        using (SqlCommand cmdDetails = new SqlCommand(updateDetailsQuery, conn, transaction))
                        {
                            cmdDetails.Parameters.AddWithValue("@TxID", currentTransactionId);
                            cmdDetails.Parameters.AddWithValue("@ItemID", itemId);
                            cmdDetails.Parameters.AddWithValue("@Subtotal", totalAmount);
                            cmdDetails.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertTxQuery = @"
                    INSERT INTO RentalTransactions (CustomerID, UserID, RentalStartDate, ExpectedReturnDate, TotalAmount, DepositAmount, AmountPaid, PaymentMethod, Status, Notes, CreatedAt)
                    OUTPUT INSERTED.TransactionID
                    VALUES (@CustomerID, @UserID, @StartDate, @ExpectedReturnDate, @TotalAmount, @DepositAmount, @AmountPaid, @PaymentMethod, 'Ongoing', @Notes, GETDATE());";

                        using (SqlCommand cmd = new SqlCommand(insertTxQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CustomerID", customerId);
                            cmd.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                            cmd.Parameters.AddWithValue("@StartDate", finalStartDateTime);
                            cmd.Parameters.AddWithValue("@ExpectedReturnDate", finalEndDateTime);
                            cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                            cmd.Parameters.AddWithValue("@DepositAmount", depositAmount);
                            cmd.Parameters.AddWithValue("@AmountPaid", amountPaid);
                            cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                            cmd.Parameters.AddWithValue("@Notes", txtNotes.Text.Trim());

                            currentTransactionId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        string insertDetailQuery = @"
                    INSERT INTO RentalDetails (TransactionID, ItemID, Quantity, Subtotal, ConditionBefore)
                    VALUES (@TransactionID, @ItemID, @Quantity, @Subtotal, 'Good');";

                        using (SqlCommand cmd = new SqlCommand(insertDetailQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TransactionID", currentTransactionId);
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                            cmd.Parameters.AddWithValue("@Quantity", quantity);
                            cmd.Parameters.AddWithValue("@Subtotal", totalAmount);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    string updateStockAndCustomerQuery = "";

                    if (this.autoFillTransactionId > 0)
                    {
                        updateStockAndCustomerQuery = "SELECT 1;";
                    }
                    else
                    {
                        updateStockAndCustomerQuery = @"
                    UPDATE Items 
                    SET AvailableQuantity = AvailableQuantity - @Quantity,
                        Status = CASE WHEN (AvailableQuantity - @Quantity) <= 0 THEN 'Fully Booked' ELSE Status END
                    WHERE ItemID = @ItemID;

                    UPDATE Customers SET TotalRentals = TotalRentals + 1 WHERE CustomerID = @CustomerID;";
                    }

                    using (SqlCommand cmd = new SqlCommand(updateStockAndCustomerQuery, conn, transaction))
                    {
                        if (this.autoFillTransactionId <= 0)
                        {
                            cmd.Parameters.AddWithValue("@Quantity", quantity);
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                        }
                        cmd.Parameters.AddWithValue("@CustomerID", customerId);
                        cmd.ExecuteNonQuery();
                    }

                    string logActionQuery = @"
                INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                VALUES (@UserID, 'UPDATE', 'RentalTransactions', @RecordID, @Description, GETDATE());";

                    using (SqlCommand cmd = new SqlCommand(logActionQuery, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@UserID", currentLoggedInUserId);
                        cmd.Parameters.AddWithValue("@RecordID", currentTransactionId);
                        string modeDesc = this.autoFillTransactionId > 0 ? "Processed reservation checkout redirect with advance balance" : "Created fresh walk-in rental";
                        cmd.Parameters.AddWithValue("@Description", $"{modeDesc} for Tx ID {currentTransactionId} linking Customer ID {customerId}");

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    MessageBox.Show("Rental transaction saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.autoFillTransactionId = -1;
                    ResetInputFields();
                    ReturnToDashboard();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Could not save the transaction. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private void HandleUnsavedExitSequence(Form targetForm)
        {
            string alertPrompt = this.autoFillTransactionId > 0
                ? "Would you like to cancel this checkout?\n\nThe reservation will be returned safely to the Booking tab."
                : "Are you sure you want to leave?\n\nAny information you typed into this screen will be lost.";

            DialogResult doubleCheck = MessageBox.Show(alertPrompt, "Leave Screen?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (doubleCheck != DialogResult.Yes) return;

            if (this.autoFillTransactionId > 0)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            string rollbackSql = @"
                        UPDATE RentalTransactions 
                        SET Status = 'Pending' 
                        WHERE TransactionID = @TxID;

                        INSERT INTO AuditLog (UserID, ActionType, TableName, RecordID, Description, ActionTime)
                        VALUES (@UserID, 'UPDATE', 'RentalTransactions', @TxID, 'Abandoned booking checkout. Reverted transaction pipeline status back to Pending safety line.', GETDATE());";

                            using (SqlCommand cmdRollback = new SqlCommand(rollbackSql, conn, trans))
                            {
                                cmdRollback.Parameters.AddWithValue("@TxID", this.autoFillTransactionId);
                                cmdRollback.Parameters.AddWithValue("@UserID", this.currentLoggedInUserId);
                                cmdRollback.ExecuteNonQuery();
                            }

                            trans.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to return the booking to pending: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            ExecuteFormNavigationCleanup(targetForm);
        }


        private void ExecuteFormNavigationCleanup(Form targetForm)
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

        private void btnCancel_Click(object sender, EventArgs e)
        {
            HandleUnsavedExitSequence(new DashBoard1(this.currentLoggedInUserId));
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            HandleUnsavedExitSequence(new DashBoard1(this.currentLoggedInUserId));
        }

        private void btnCalendar_Click(object sender, EventArgs e)
        {
            HandleUnsavedExitSequence(new Calendar(this.currentLoggedInUserId));
        }

        private void btnInventoryManagement_Click(object sender, EventArgs e)
        {
            HandleUnsavedExitSequence(new Inventory_Management(this.currentLoggedInUserId));
        }

        private void btnRecords_Click(object sender, EventArgs e)
        {
            HandleUnsavedExitSequence(new Customer_Records(this.currentLoggedInUserId));
        }

        private void btnBookingManagement_Click(object sender, EventArgs e)
        {
            HandleUnsavedExitSequence(new Booking_Management(this.currentLoggedInUserId));
        }

        private void ResetInputFields()
        {
            cmbCustomerName.SelectedIndex = -1;
            cmbItem.SelectedIndex = -1;
            if (numQuantity != null) numQuantity.Value = 1;
            if (numDeposit != null) numDeposit.Value = 0;
            txtNotes.Text = string.Empty;
            rbCash.Checked = true;

            dtpStartDate.Value = DateTime.Now;
            dtpStartTime.Value = DateTime.Now;
            dtpExpectedReturnDate.Value = DateTime.Now;
            dtpExpectedReturnTime.Value = DateTime.Now.AddDays(1);

            txtDailyRate.Text = "0.00";
            txtSubTotal.Text = "0.00";
            txtAmountPaid.Text = "0.00";
            txtBalanceDue.Text = "0.00";

            if (txtFinancialTotal != null) txtFinancialTotal.Text = "0.00";
            if (txtFinancialAmountPaid != null) txtFinancialAmountPaid.Text = "0.00";
            if (txtFinancialChange != null) txtFinancialChange.Text = "0.00";

            if (txtCustNo != null) txtCustNo.Clear();
            if (txtCustEmail != null) txtCustEmail.Clear();
            if (txtCustAddress != null) txtCustAddress.Clear();

            if (txtItemAvailableQty != null) txtItemAvailableQty.Clear();
            if (txtItemCategory != null) txtItemCategory.Clear();
            if (txtItemDescription != null) txtItemDescription.Clear();

            currentCustomerId = -1;
        }

        private void ReturnToDashboard()
        {
            ExecuteFormNavigationCleanup(new DashBoard1(this.currentLoggedInUserId));
        }

        private void txtFinancialAmountPaid_TextChanged(object sender, EventArgs e)
        {
            if (txtAmountPaid != null && txtFinancialAmountPaid != null)
            {
                txtAmountPaid.TextChanged -= txtAmountPaid_TextChanged;
                txtAmountPaid.Text = txtFinancialAmountPaid.Text;
                txtAmountPaid.TextChanged += txtAmountPaid_TextChanged;
            }
            CalculateBalance();
        }

        private void ExecuteBookingAutoFillSequence()
        {
            if (this.autoFillTransactionId <= 0) return;

            string query = @"
                SELECT t.CustomerID, rd.ItemID, rd.Quantity, t.DepositAmount, t.AmountPaid, t.Notes,
                       t.RentalStartDate, t.ExpectedReturnDate
                FROM RentalTransactions t
                INNER JOIN RentalDetails rd ON t.TransactionID = rd.TransactionID
                WHERE t.TransactionID = @TxID;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@TxID", this.autoFillTransactionId);
                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int cid = Convert.ToInt32(reader["CustomerID"]);
                            int iid = Convert.ToInt32(reader["ItemID"]);
                            int qty = Convert.ToInt32(reader["Quantity"]);
                            decimal dep = Convert.ToDecimal(reader["DepositAmount"]);
                            decimal advancePaid = Convert.ToDecimal(reader["AmountPaid"]);
                            string bookingNotes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : "";

                            DateTime startDateTime = Convert.ToDateTime(reader["RentalStartDate"]);
                            DateTime endDateTime = Convert.ToDateTime(reader["ExpectedReturnDate"]);

                            if (cmbCustomerName != null) cmbCustomerName.SelectedValue = cid;
                            if (cmbItem != null) cmbItem.SelectedValue = iid;
                            if (numQuantity != null) numQuantity.Value = qty;
                            if (numDeposit != null) numDeposit.Value = dep;
                            if (txtNotes != null) txtNotes.Text = bookingNotes;
                            if (txtAmountPaid != null) txtAmountPaid.Text = advancePaid.ToString("F2");

                            if (dtpStartDate != null) dtpStartDate.Value = startDateTime.Date;
                            if (dtpStartTime != null) dtpStartTime.Value = startDateTime;

                            if (dtpExpectedReturnDate != null) dtpExpectedReturnDate.Value = endDateTime.Date;
                            if (dtpExpectedReturnTime != null) dtpExpectedReturnTime.Value = endDateTime;

                            CalculateTotals();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Auto-fill loading sequence failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnReturnsCheckIn_Click(object sender, EventArgs e)
        {
            SafelyNavigateToForm(new ReturnsCheckIn(this.currentLoggedInUserId));
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
