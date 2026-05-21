namespace FinalProject
{
    partial class Inventory_Management
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button2 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnReturnsCheckIn = new System.Windows.Forms.Button();
            this.btnUserManagement = new System.Windows.Forms.Button();
            this.btnBookingManagement = new System.Windows.Forms.Button();
            this.btnReports = new System.Windows.Forms.Button();
            this.btnRecords = new System.Windows.Forms.Button();
            this.btnCalendar = new System.Windows.Forms.Button();
            this.button14 = new System.Windows.Forms.Button();
            this.btnNewRentalTransaction = new System.Windows.Forms.Button();
            this.btnHome = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label7 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.avail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DailyRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.stat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.act = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Category = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnAddNewItem = new System.Windows.Forms.Button();
            this.pbUserProfilePic = new System.Windows.Forms.PictureBox();
            this.UserNameHeader = new System.Windows.Forms.Label();
            this.btnSearchItem = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.cmbFilters = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbUserProfilePic)).BeginInit();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.Ivory;
            this.button2.Location = new System.Drawing.Point(974, 6);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(25, 19);
            this.button2.TabIndex = 108;
            this.button2.UseVisualStyleBackColor = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Sienna;
            this.panel1.Controls.Add(this.btnReturnsCheckIn);
            this.panel1.Controls.Add(this.btnUserManagement);
            this.panel1.Controls.Add(this.btnBookingManagement);
            this.panel1.Controls.Add(this.btnReports);
            this.panel1.Controls.Add(this.btnRecords);
            this.panel1.Controls.Add(this.btnCalendar);
            this.panel1.Controls.Add(this.button14);
            this.panel1.Controls.Add(this.btnNewRentalTransaction);
            this.panel1.Controls.Add(this.btnHome);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(188, 566);
            this.panel1.TabIndex = 105;
            // 
            // btnReturnsCheckIn
            // 
            this.btnReturnsCheckIn.BackColor = System.Drawing.Color.Sienna;
            this.btnReturnsCheckIn.FlatAppearance.BorderSize = 0;
            this.btnReturnsCheckIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReturnsCheckIn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReturnsCheckIn.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.btnReturnsCheckIn.Location = new System.Drawing.Point(-1, 327);
            this.btnReturnsCheckIn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReturnsCheckIn.Name = "btnReturnsCheckIn";
            this.btnReturnsCheckIn.Size = new System.Drawing.Size(191, 28);
            this.btnReturnsCheckIn.TabIndex = 92;
            this.btnReturnsCheckIn.Text = "  Returns / Check-in";
            this.btnReturnsCheckIn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReturnsCheckIn.UseVisualStyleBackColor = false;
            this.btnReturnsCheckIn.Click += new System.EventHandler(this.btnReturnsCheckIn_Click);
            // 
            // btnUserManagement
            // 
            this.btnUserManagement.BackColor = System.Drawing.Color.Sienna;
            this.btnUserManagement.FlatAppearance.BorderSize = 0;
            this.btnUserManagement.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUserManagement.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUserManagement.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.btnUserManagement.Location = new System.Drawing.Point(0, 383);
            this.btnUserManagement.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnUserManagement.Name = "btnUserManagement";
            this.btnUserManagement.Size = new System.Drawing.Size(191, 28);
            this.btnUserManagement.TabIndex = 91;
            this.btnUserManagement.Text = "  User Management";
            this.btnUserManagement.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnUserManagement.UseVisualStyleBackColor = false;
            this.btnUserManagement.Click += new System.EventHandler(this.btnUserManagement_Click);
            // 
            // btnBookingManagement
            // 
            this.btnBookingManagement.BackColor = System.Drawing.Color.Sienna;
            this.btnBookingManagement.FlatAppearance.BorderSize = 0;
            this.btnBookingManagement.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBookingManagement.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBookingManagement.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.btnBookingManagement.Location = new System.Drawing.Point(-1, 299);
            this.btnBookingManagement.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnBookingManagement.Name = "btnBookingManagement";
            this.btnBookingManagement.Size = new System.Drawing.Size(191, 28);
            this.btnBookingManagement.TabIndex = 90;
            this.btnBookingManagement.Text = "  Booking Management";
            this.btnBookingManagement.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBookingManagement.UseVisualStyleBackColor = false;
            this.btnBookingManagement.Click += new System.EventHandler(this.btnBookingManagement_Click);
            // 
            // btnReports
            // 
            this.btnReports.BackColor = System.Drawing.Color.Sienna;
            this.btnReports.FlatAppearance.BorderSize = 0;
            this.btnReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReports.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReports.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.btnReports.Location = new System.Drawing.Point(0, 355);
            this.btnReports.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReports.Name = "btnReports";
            this.btnReports.Size = new System.Drawing.Size(191, 28);
            this.btnReports.TabIndex = 89;
            this.btnReports.Text = "  Reports";
            this.btnReports.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReports.UseVisualStyleBackColor = false;
            this.btnReports.Click += new System.EventHandler(this.btnReports_Click);
            // 
            // btnRecords
            // 
            this.btnRecords.BackColor = System.Drawing.Color.Sienna;
            this.btnRecords.FlatAppearance.BorderSize = 0;
            this.btnRecords.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecords.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRecords.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.btnRecords.Location = new System.Drawing.Point(0, 271);
            this.btnRecords.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRecords.Name = "btnRecords";
            this.btnRecords.Size = new System.Drawing.Size(191, 28);
            this.btnRecords.TabIndex = 88;
            this.btnRecords.Text = "  Records\r\n";
            this.btnRecords.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRecords.UseVisualStyleBackColor = false;
            this.btnRecords.Click += new System.EventHandler(this.btnRecords_Click);
            // 
            // btnCalendar
            // 
            this.btnCalendar.BackColor = System.Drawing.Color.Sienna;
            this.btnCalendar.FlatAppearance.BorderSize = 0;
            this.btnCalendar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalendar.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCalendar.ForeColor = System.Drawing.SystemColors.InactiveBorder;
            this.btnCalendar.Location = new System.Drawing.Point(-1, 215);
            this.btnCalendar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnCalendar.Name = "btnCalendar";
            this.btnCalendar.Size = new System.Drawing.Size(191, 28);
            this.btnCalendar.TabIndex = 87;
            this.btnCalendar.Text = "  Calendar";
            this.btnCalendar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCalendar.UseVisualStyleBackColor = false;
            this.btnCalendar.Click += new System.EventHandler(this.btnCalendar_Click);
            // 
            // button14
            // 
            this.button14.BackColor = System.Drawing.Color.Ivory;
            this.button14.FlatAppearance.BorderSize = 0;
            this.button14.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button14.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button14.Location = new System.Drawing.Point(-3, 243);
            this.button14.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button14.Name = "button14";
            this.button14.Size = new System.Drawing.Size(191, 28);
            this.button14.TabIndex = 86;
            this.button14.Text = "  Inventory Management";
            this.button14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button14.UseVisualStyleBackColor = false;
            // 
            // btnNewRentalTransaction
            // 
            this.btnNewRentalTransaction.BackColor = System.Drawing.Color.Sienna;
            this.btnNewRentalTransaction.FlatAppearance.BorderColor = System.Drawing.Color.Sienna;
            this.btnNewRentalTransaction.FlatAppearance.BorderSize = 0;
            this.btnNewRentalTransaction.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewRentalTransaction.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNewRentalTransaction.ForeColor = System.Drawing.SystemColors.Window;
            this.btnNewRentalTransaction.Location = new System.Drawing.Point(-2, 187);
            this.btnNewRentalTransaction.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnNewRentalTransaction.Name = "btnNewRentalTransaction";
            this.btnNewRentalTransaction.Size = new System.Drawing.Size(191, 28);
            this.btnNewRentalTransaction.TabIndex = 85;
            this.btnNewRentalTransaction.Text = "  + New Rental Transaction";
            this.btnNewRentalTransaction.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNewRentalTransaction.UseVisualStyleBackColor = false;
            this.btnNewRentalTransaction.Click += new System.EventHandler(this.btnNewRentalTransaction_Click);
            // 
            // btnHome
            // 
            this.btnHome.BackColor = System.Drawing.Color.Sienna;
            this.btnHome.FlatAppearance.BorderSize = 0;
            this.btnHome.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHome.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHome.ForeColor = System.Drawing.SystemColors.InactiveBorder;
            this.btnHome.Location = new System.Drawing.Point(-1, 156);
            this.btnHome.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(191, 28);
            this.btnHome.TabIndex = 84;
            this.btnHome.Text = "   Home";
            this.btnHome.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHome.UseVisualStyleBackColor = false;
            this.btnHome.Click += new System.EventHandler(this.btnHome_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::FinalProject.Properties.Resources.bd5ad008_ab6d_4842_b59b_6d43d8564f91;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(26, 28);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(132, 71);
            this.pictureBox1.TabIndex = 30;
            this.pictureBox1.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Arial Narrow", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label7.Location = new System.Drawing.Point(204, 50);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(287, 24);
            this.label7.TabIndex = 106;
            this.label7.Text = "JD Woodworks and Rentals System";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(1068, 0);
            this.button5.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(33, 25);
            this.button5.TabIndex = 104;
            this.button5.Text = "Close";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(1036, 0);
            this.button7.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(33, 25);
            this.button7.TabIndex = 110;
            this.button7.Text = "Max";
            this.button7.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(1005, 0);
            this.button6.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(33, 25);
            this.button6.TabIndex = 109;
            this.button6.Text = "Min";
            this.button6.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.ItemName,
            this.TotalQty,
            this.avail,
            this.DailyRate,
            this.stat,
            this.act,
            this.Category,
            this.Description});
            this.dataGridView1.Location = new System.Drawing.Point(208, 105);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 62;
            this.dataGridView1.RowTemplate.Height = 28;
            this.dataGridView1.Size = new System.Drawing.Size(870, 411);
            this.dataGridView1.TabIndex = 111;
            // 
            // id
            // 
            this.id.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.id.HeaderText = "ID";
            this.id.MinimumWidth = 8;
            this.id.Name = "id";
            // 
            // ItemName
            // 
            this.ItemName.HeaderText = "Item Name";
            this.ItemName.MinimumWidth = 8;
            this.ItemName.Name = "ItemName";
            this.ItemName.Width = 150;
            // 
            // TotalQty
            // 
            this.TotalQty.HeaderText = "Total Qty";
            this.TotalQty.MinimumWidth = 8;
            this.TotalQty.Name = "TotalQty";
            this.TotalQty.Width = 150;
            // 
            // avail
            // 
            this.avail.HeaderText = "Available";
            this.avail.MinimumWidth = 8;
            this.avail.Name = "avail";
            this.avail.Width = 150;
            // 
            // DailyRate
            // 
            this.DailyRate.HeaderText = "Daily Rate";
            this.DailyRate.MinimumWidth = 8;
            this.DailyRate.Name = "DailyRate";
            this.DailyRate.Width = 150;
            // 
            // stat
            // 
            this.stat.HeaderText = "Status";
            this.stat.MinimumWidth = 8;
            this.stat.Name = "stat";
            this.stat.Width = 150;
            // 
            // act
            // 
            this.act.HeaderText = "Action";
            this.act.MinimumWidth = 8;
            this.act.Name = "act";
            this.act.Width = 150;
            // 
            // Category
            // 
            this.Category.HeaderText = "Category";
            this.Category.MinimumWidth = 6;
            this.Category.Name = "Category";
            this.Category.Width = 125;
            // 
            // Description
            // 
            this.Description.HeaderText = "Description";
            this.Description.MinimumWidth = 6;
            this.Description.Name = "Description";
            this.Description.Width = 125;
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.Sienna;
            this.btnRefresh.ForeColor = System.Drawing.SystemColors.InactiveBorder;
            this.btnRefresh.Location = new System.Drawing.Point(362, 521);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(147, 34);
            this.btnRefresh.TabIndex = 113;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnAddNewItem
            // 
            this.btnAddNewItem.BackColor = System.Drawing.Color.Sienna;
            this.btnAddNewItem.ForeColor = System.Drawing.SystemColors.InactiveBorder;
            this.btnAddNewItem.Location = new System.Drawing.Point(209, 521);
            this.btnAddNewItem.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnAddNewItem.Name = "btnAddNewItem";
            this.btnAddNewItem.Size = new System.Drawing.Size(147, 34);
            this.btnAddNewItem.TabIndex = 112;
            this.btnAddNewItem.Text = "Add New Item";
            this.btnAddNewItem.UseVisualStyleBackColor = false;
            this.btnAddNewItem.Click += new System.EventHandler(this.btnAddNewItem_Click);
            // 
            // pbUserProfilePic
            // 
            this.pbUserProfilePic.BackColor = System.Drawing.Color.Gainsboro;
            this.pbUserProfilePic.Location = new System.Drawing.Point(1028, 47);
            this.pbUserProfilePic.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pbUserProfilePic.Name = "pbUserProfilePic";
            this.pbUserProfilePic.Size = new System.Drawing.Size(47, 38);
            this.pbUserProfilePic.TabIndex = 107;
            this.pbUserProfilePic.TabStop = false;
            // 
            // UserNameHeader
            // 
            this.UserNameHeader.AutoSize = true;
            this.UserNameHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserNameHeader.Location = new System.Drawing.Point(847, 67);
            this.UserNameHeader.Name = "UserNameHeader";
            this.UserNameHeader.Size = new System.Drawing.Size(48, 18);
            this.UserNameHeader.TabIndex = 115;
            this.UserNameHeader.Text = "Name";
            // 
            // btnSearchItem
            // 
            this.btnSearchItem.BackColor = System.Drawing.Color.Sienna;
            this.btnSearchItem.ForeColor = System.Drawing.SystemColors.InactiveBorder;
            this.btnSearchItem.Location = new System.Drawing.Point(931, 521);
            this.btnSearchItem.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnSearchItem.Name = "btnSearchItem";
            this.btnSearchItem.Size = new System.Drawing.Size(147, 34);
            this.btnSearchItem.TabIndex = 116;
            this.btnSearchItem.Text = "Search";
            this.btnSearchItem.UseVisualStyleBackColor = false;
            this.btnSearchItem.Click += new System.EventHandler(this.btnSearchItem_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSearch.Location = new System.Drawing.Point(683, 524);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(242, 27);
            this.txtSearch.TabIndex = 117;
            // 
            // cmbFilters
            // 
            this.cmbFilters.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbFilters.FormattingEnabled = true;
            this.cmbFilters.Location = new System.Drawing.Point(515, 524);
            this.cmbFilters.Name = "cmbFilters";
            this.cmbFilters.Size = new System.Drawing.Size(162, 28);
            this.cmbFilters.TabIndex = 118;
            this.cmbFilters.Text = "Filter";
            this.cmbFilters.SelectedIndexChanged += new System.EventHandler(this.cmbFilters_SelectedIndexChanged);
            // 
            // Inventory_Management
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Ivory;
            this.ClientSize = new System.Drawing.Size(1101, 566);
            this.Controls.Add(this.cmbFilters);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnSearchItem);
            this.Controls.Add(this.UserNameHeader);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnAddNewItem);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.pbUserProfilePic);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Inventory_Management";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Inventory_Management";
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbUserProfilePic)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnReturnsCheckIn;
        private System.Windows.Forms.Button btnUserManagement;
        private System.Windows.Forms.Button btnBookingManagement;
        private System.Windows.Forms.Button btnReports;
        private System.Windows.Forms.Button btnRecords;
        private System.Windows.Forms.Button btnCalendar;
        private System.Windows.Forms.Button button14;
        private System.Windows.Forms.Button btnNewRentalTransaction;
        private System.Windows.Forms.Button btnHome;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.PictureBox pbUserProfilePic;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnAddNewItem;
        private System.Windows.Forms.Label UserNameHeader;
        private System.Windows.Forms.Button btnSearchItem;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ComboBox cmbFilters;
        private System.Windows.Forms.DataGridViewTextBoxColumn id;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn avail;
        private System.Windows.Forms.DataGridViewTextBoxColumn DailyRate;
        private System.Windows.Forms.DataGridViewTextBoxColumn stat;
        private System.Windows.Forms.DataGridViewTextBoxColumn act;
        private System.Windows.Forms.DataGridViewTextBoxColumn Category;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
    }
}