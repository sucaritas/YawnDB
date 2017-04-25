namespace MonitoringExampleProgram
{
    partial class MonitoringExample
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitoringExample));
            this.SysMonitorControl = new AxSystemMonitor.AxSystemMonitor();
            this.NoOfItems = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.NoOfThreads = new System.Windows.Forms.TextBox();
            this.EnableTransactions = new System.Windows.Forms.CheckBox();
            this.OpenAndCloseBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.RecordCountLbl = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.YawnTransactionRollbackFinished = new System.Windows.Forms.CheckBox();
            this.YawnTransactionRollbackFailed = new System.Windows.Forms.CheckBox();
            this.YawnTransactionRollbackStart = new System.Windows.Forms.CheckBox();
            this.YawnTransactionCommitFinished = new System.Windows.Forms.CheckBox();
            this.YawnTransactionCommitFail = new System.Windows.Forms.CheckBox();
            this.YawnTransactionCommitStart = new System.Windows.Forms.CheckBox();
            this.YawnTransactionsCreated = new System.Windows.Forms.CheckBox();
            this.YawnDBInitialized = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.StorageWriteContentions = new System.Windows.Forms.CheckBox();
            this.StorageStorageResizes = new System.Windows.Forms.CheckBox();
            this.StorageRecordIndexingFinished = new System.Windows.Forms.CheckBox();
            this.StorageRecordIndexingStart = new System.Windows.Forms.CheckBox();
            this.StorageRecordDeleteFinished = new System.Windows.Forms.CheckBox();
            this.StorageRecordDeleteStart = new System.Windows.Forms.CheckBox();
            this.StorageRecordReadsFromCache = new System.Windows.Forms.CheckBox();
            this.StorageRecordReadFinished = new System.Windows.Forms.CheckBox();
            this.StorageRecordReadStart = new System.Windows.Forms.CheckBox();
            this.StorageRecordWritesFinished = new System.Windows.Forms.CheckBox();
            this.StorageRecordWriteStart = new System.Windows.Forms.CheckBox();
            this.StorageTimesStorageHasInitialized = new System.Windows.Forms.CheckBox();
            this.RunBtn = new System.Windows.Forms.Button();
            this.LastRunTime = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.FailedOpsLbl = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.RefreshCountBtn = new System.Windows.Forms.Button();
            this.ErrorsTxt = new System.Windows.Forms.TextBox();
            this.DeleteDatabase = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.SysMonitorControl)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // SysMonitorControl
            // 
            this.SysMonitorControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SysMonitorControl.Enabled = true;
            this.SysMonitorControl.Location = new System.Drawing.Point(12, 305);
            this.SysMonitorControl.Name = "SysMonitorControl";
            this.SysMonitorControl.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("SysMonitorControl.OcxState")));
            this.SysMonitorControl.Size = new System.Drawing.Size(999, 583);
            this.SysMonitorControl.TabIndex = 0;
            // 
            // NoOfItems
            // 
            this.NoOfItems.Location = new System.Drawing.Point(237, 13);
            this.NoOfItems.Name = "NoOfItems";
            this.NoOfItems.Size = new System.Drawing.Size(74, 20);
            this.NoOfItems.TabIndex = 1;
            this.NoOfItems.Text = "1000";
            this.NoOfItems.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(219, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "How many items holud be inserted per thread";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(182, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "How many threads sholud be fired up";
            // 
            // NoOfThreads
            // 
            this.NoOfThreads.Location = new System.Drawing.Point(237, 44);
            this.NoOfThreads.Name = "NoOfThreads";
            this.NoOfThreads.Size = new System.Drawing.Size(74, 20);
            this.NoOfThreads.TabIndex = 3;
            this.NoOfThreads.Text = "100";
            this.NoOfThreads.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // EnableTransactions
            // 
            this.EnableTransactions.AutoSize = true;
            this.EnableTransactions.Location = new System.Drawing.Point(331, 16);
            this.EnableTransactions.Name = "EnableTransactions";
            this.EnableTransactions.Size = new System.Drawing.Size(119, 17);
            this.EnableTransactions.TabIndex = 6;
            this.EnableTransactions.Text = "Enable transactions";
            this.EnableTransactions.UseVisualStyleBackColor = true;
            // 
            // OpenAndCloseBtn
            // 
            this.OpenAndCloseBtn.ForeColor = System.Drawing.Color.Red;
            this.OpenAndCloseBtn.Location = new System.Drawing.Point(412, 42);
            this.OpenAndCloseBtn.Name = "OpenAndCloseBtn";
            this.OpenAndCloseBtn.Size = new System.Drawing.Size(119, 23);
            this.OpenAndCloseBtn.TabIndex = 7;
            this.OpenAndCloseBtn.Text = "Open database";
            this.OpenAndCloseBtn.UseVisualStyleBackColor = true;
            this.OpenAndCloseBtn.Click += new System.EventHandler(this.OpenAndCloseBtn_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(691, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Current record count:";
            // 
            // RecordCountLbl
            // 
            this.RecordCountLbl.AutoSize = true;
            this.RecordCountLbl.Location = new System.Drawing.Point(804, 23);
            this.RecordCountLbl.Name = "RecordCountLbl";
            this.RecordCountLbl.Size = new System.Drawing.Size(13, 13);
            this.RecordCountLbl.TabIndex = 9;
            this.RecordCountLbl.Text = "0";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.YawnTransactionRollbackFinished);
            this.groupBox1.Controls.Add(this.YawnTransactionRollbackFailed);
            this.groupBox1.Controls.Add(this.YawnTransactionRollbackStart);
            this.groupBox1.Controls.Add(this.YawnTransactionCommitFinished);
            this.groupBox1.Controls.Add(this.YawnTransactionCommitFail);
            this.groupBox1.Controls.Add(this.YawnTransactionCommitStart);
            this.groupBox1.Controls.Add(this.YawnTransactionsCreated);
            this.groupBox1.Controls.Add(this.YawnDBInitialized);
            this.groupBox1.Location = new System.Drawing.Point(15, 76);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(205, 210);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "DatabaseCounters";
            // 
            // YawnTransactionRollbackFinished
            // 
            this.YawnTransactionRollbackFinished.AutoSize = true;
            this.YawnTransactionRollbackFinished.Location = new System.Drawing.Point(7, 184);
            this.YawnTransactionRollbackFinished.Name = "YawnTransactionRollbackFinished";
            this.YawnTransactionRollbackFinished.Size = new System.Drawing.Size(179, 17);
            this.YawnTransactionRollbackFinished.TabIndex = 7;
            this.YawnTransactionRollbackFinished.Text = "# Transaction Rollback Finished";
            this.YawnTransactionRollbackFinished.UseVisualStyleBackColor = true;
            this.YawnTransactionRollbackFinished.CheckedChanged += new System.EventHandler(this.YawnTransactionRollbackFinished_CheckedChanged);
            // 
            // YawnTransactionRollbackFailed
            // 
            this.YawnTransactionRollbackFailed.AutoSize = true;
            this.YawnTransactionRollbackFailed.Location = new System.Drawing.Point(7, 160);
            this.YawnTransactionRollbackFailed.Name = "YawnTransactionRollbackFailed";
            this.YawnTransactionRollbackFailed.Size = new System.Drawing.Size(168, 17);
            this.YawnTransactionRollbackFailed.TabIndex = 6;
            this.YawnTransactionRollbackFailed.Text = "# Transaction Rollback Failed";
            this.YawnTransactionRollbackFailed.UseVisualStyleBackColor = true;
            this.YawnTransactionRollbackFailed.CheckedChanged += new System.EventHandler(this.YawnTransactionRollbackFailed_CheckedChanged);
            // 
            // YawnTransactionRollbackStart
            // 
            this.YawnTransactionRollbackStart.AutoSize = true;
            this.YawnTransactionRollbackStart.Location = new System.Drawing.Point(7, 137);
            this.YawnTransactionRollbackStart.Name = "YawnTransactionRollbackStart";
            this.YawnTransactionRollbackStart.Size = new System.Drawing.Size(162, 17);
            this.YawnTransactionRollbackStart.TabIndex = 5;
            this.YawnTransactionRollbackStart.Text = "# Transaction Rollback Start";
            this.YawnTransactionRollbackStart.UseVisualStyleBackColor = true;
            this.YawnTransactionRollbackStart.CheckedChanged += new System.EventHandler(this.YawnTransactionRollbackStart_CheckedChanged);
            // 
            // YawnTransactionCommitFinished
            // 
            this.YawnTransactionCommitFinished.AutoSize = true;
            this.YawnTransactionCommitFinished.Location = new System.Drawing.Point(7, 115);
            this.YawnTransactionCommitFinished.Name = "YawnTransactionCommitFinished";
            this.YawnTransactionCommitFinished.Size = new System.Drawing.Size(171, 17);
            this.YawnTransactionCommitFinished.TabIndex = 4;
            this.YawnTransactionCommitFinished.Text = "# Transaction Commit Finished";
            this.YawnTransactionCommitFinished.UseVisualStyleBackColor = true;
            this.YawnTransactionCommitFinished.CheckedChanged += new System.EventHandler(this.YawnTransactionCommitFinished_CheckedChanged);
            // 
            // YawnTransactionCommitFail
            // 
            this.YawnTransactionCommitFail.AutoSize = true;
            this.YawnTransactionCommitFail.Location = new System.Drawing.Point(7, 91);
            this.YawnTransactionCommitFail.Name = "YawnTransactionCommitFail";
            this.YawnTransactionCommitFail.Size = new System.Drawing.Size(148, 17);
            this.YawnTransactionCommitFail.TabIndex = 3;
            this.YawnTransactionCommitFail.Text = "# Transaction Commit Fail";
            this.YawnTransactionCommitFail.UseVisualStyleBackColor = true;
            this.YawnTransactionCommitFail.CheckedChanged += new System.EventHandler(this.YawnTransactionCommitFail_CheckedChanged);
            // 
            // YawnTransactionCommitStart
            // 
            this.YawnTransactionCommitStart.AutoSize = true;
            this.YawnTransactionCommitStart.Location = new System.Drawing.Point(7, 67);
            this.YawnTransactionCommitStart.Name = "YawnTransactionCommitStart";
            this.YawnTransactionCommitStart.Size = new System.Drawing.Size(154, 17);
            this.YawnTransactionCommitStart.TabIndex = 2;
            this.YawnTransactionCommitStart.Text = "# Transaction Commit Start";
            this.YawnTransactionCommitStart.UseVisualStyleBackColor = true;
            this.YawnTransactionCommitStart.CheckedChanged += new System.EventHandler(this.YawnTransactionCommitStart_CheckedChanged);
            // 
            // YawnTransactionsCreated
            // 
            this.YawnTransactionsCreated.AutoSize = true;
            this.YawnTransactionsCreated.Location = new System.Drawing.Point(7, 43);
            this.YawnTransactionsCreated.Name = "YawnTransactionsCreated";
            this.YawnTransactionsCreated.Size = new System.Drawing.Size(137, 17);
            this.YawnTransactionsCreated.TabIndex = 1;
            this.YawnTransactionsCreated.Text = "# Transactions Created";
            this.YawnTransactionsCreated.UseVisualStyleBackColor = true;
            this.YawnTransactionsCreated.CheckedChanged += new System.EventHandler(this.YawnTransactionsCreated_CheckedChanged);
            // 
            // YawnDBInitialized
            // 
            this.YawnDBInitialized.AutoSize = true;
            this.YawnDBInitialized.Location = new System.Drawing.Point(6, 19);
            this.YawnDBInitialized.Name = "YawnDBInitialized";
            this.YawnDBInitialized.Size = new System.Drawing.Size(146, 17);
            this.YawnDBInitialized.TabIndex = 0;
            this.YawnDBInitialized.Text = "# YawnDB Has Initialized";
            this.YawnDBInitialized.UseVisualStyleBackColor = true;
            this.YawnDBInitialized.CheckedChanged += new System.EventHandler(this.YawnDBInitialized_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.StorageWriteContentions);
            this.groupBox2.Controls.Add(this.StorageStorageResizes);
            this.groupBox2.Controls.Add(this.StorageRecordIndexingFinished);
            this.groupBox2.Controls.Add(this.StorageRecordIndexingStart);
            this.groupBox2.Controls.Add(this.StorageRecordDeleteFinished);
            this.groupBox2.Controls.Add(this.StorageRecordDeleteStart);
            this.groupBox2.Controls.Add(this.StorageRecordReadsFromCache);
            this.groupBox2.Controls.Add(this.StorageRecordReadFinished);
            this.groupBox2.Controls.Add(this.StorageRecordReadStart);
            this.groupBox2.Controls.Add(this.StorageRecordWritesFinished);
            this.groupBox2.Controls.Add(this.StorageRecordWriteStart);
            this.groupBox2.Controls.Add(this.StorageTimesStorageHasInitialized);
            this.groupBox2.Location = new System.Drawing.Point(226, 76);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(341, 210);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "StorageCounters";
            // 
            // StorageWriteContentions
            // 
            this.StorageWriteContentions.AutoSize = true;
            this.StorageWriteContentions.Location = new System.Drawing.Point(182, 90);
            this.StorageWriteContentions.Name = "StorageWriteContentions";
            this.StorageWriteContentions.Size = new System.Drawing.Size(119, 17);
            this.StorageWriteContentions.TabIndex = 11;
            this.StorageWriteContentions.Text = "# Write contentions";
            this.StorageWriteContentions.UseVisualStyleBackColor = true;
            this.StorageWriteContentions.CheckedChanged += new System.EventHandler(this.StorageWriteContentions_CheckedChanged);
            // 
            // StorageStorageResizes
            // 
            this.StorageStorageResizes.AutoSize = true;
            this.StorageStorageResizes.Location = new System.Drawing.Point(182, 67);
            this.StorageStorageResizes.Name = "StorageStorageResizes";
            this.StorageStorageResizes.Size = new System.Drawing.Size(108, 17);
            this.StorageStorageResizes.TabIndex = 10;
            this.StorageStorageResizes.Text = "# Storage resizes";
            this.StorageStorageResizes.UseVisualStyleBackColor = true;
            this.StorageStorageResizes.CheckedChanged += new System.EventHandler(this.StorageStorageResizes_CheckedChanged);
            // 
            // StorageRecordIndexingFinished
            // 
            this.StorageRecordIndexingFinished.AutoSize = true;
            this.StorageRecordIndexingFinished.Location = new System.Drawing.Point(182, 43);
            this.StorageRecordIndexingFinished.Name = "StorageRecordIndexingFinished";
            this.StorageRecordIndexingFinished.Size = new System.Drawing.Size(156, 17);
            this.StorageRecordIndexingFinished.TabIndex = 9;
            this.StorageRecordIndexingFinished.Text = "# Record Indexing Finished";
            this.StorageRecordIndexingFinished.UseVisualStyleBackColor = true;
            this.StorageRecordIndexingFinished.CheckedChanged += new System.EventHandler(this.StorageRecordIndexingFinished_CheckedChanged);
            // 
            // StorageRecordIndexingStart
            // 
            this.StorageRecordIndexingStart.AutoSize = true;
            this.StorageRecordIndexingStart.Location = new System.Drawing.Point(182, 19);
            this.StorageRecordIndexingStart.Name = "StorageRecordIndexingStart";
            this.StorageRecordIndexingStart.Size = new System.Drawing.Size(139, 17);
            this.StorageRecordIndexingStart.TabIndex = 8;
            this.StorageRecordIndexingStart.Text = "# Record Indexing Start";
            this.StorageRecordIndexingStart.UseVisualStyleBackColor = true;
            this.StorageRecordIndexingStart.CheckedChanged += new System.EventHandler(this.StorageRecordIndexingStart_CheckedChanged);
            // 
            // StorageRecordDeleteFinished
            // 
            this.StorageRecordDeleteFinished.AutoSize = true;
            this.StorageRecordDeleteFinished.Location = new System.Drawing.Point(6, 184);
            this.StorageRecordDeleteFinished.Name = "StorageRecordDeleteFinished";
            this.StorageRecordDeleteFinished.Size = new System.Drawing.Size(147, 17);
            this.StorageRecordDeleteFinished.TabIndex = 7;
            this.StorageRecordDeleteFinished.Text = "# Record Delete Finished";
            this.StorageRecordDeleteFinished.UseVisualStyleBackColor = true;
            this.StorageRecordDeleteFinished.CheckedChanged += new System.EventHandler(this.StorageRecordDeleteFinished_CheckedChanged);
            // 
            // StorageRecordDeleteStart
            // 
            this.StorageRecordDeleteStart.AutoSize = true;
            this.StorageRecordDeleteStart.Location = new System.Drawing.Point(6, 160);
            this.StorageRecordDeleteStart.Name = "StorageRecordDeleteStart";
            this.StorageRecordDeleteStart.Size = new System.Drawing.Size(130, 17);
            this.StorageRecordDeleteStart.TabIndex = 6;
            this.StorageRecordDeleteStart.Text = "# Record Delete Start";
            this.StorageRecordDeleteStart.UseVisualStyleBackColor = true;
            this.StorageRecordDeleteStart.CheckedChanged += new System.EventHandler(this.StorageRecordDeleteStart_CheckedChanged);
            // 
            // StorageRecordReadsFromCache
            // 
            this.StorageRecordReadsFromCache.AutoSize = true;
            this.StorageRecordReadsFromCache.Location = new System.Drawing.Point(6, 138);
            this.StorageRecordReadsFromCache.Name = "StorageRecordReadsFromCache";
            this.StorageRecordReadsFromCache.Size = new System.Drawing.Size(165, 17);
            this.StorageRecordReadsFromCache.TabIndex = 5;
            this.StorageRecordReadsFromCache.Text = "# Record Reads From Cache";
            this.StorageRecordReadsFromCache.UseVisualStyleBackColor = true;
            this.StorageRecordReadsFromCache.CheckedChanged += new System.EventHandler(this.StorageRecordReadsFromCache_CheckedChanged);
            // 
            // StorageRecordReadFinished
            // 
            this.StorageRecordReadFinished.AutoSize = true;
            this.StorageRecordReadFinished.Location = new System.Drawing.Point(6, 114);
            this.StorageRecordReadFinished.Name = "StorageRecordReadFinished";
            this.StorageRecordReadFinished.Size = new System.Drawing.Size(142, 17);
            this.StorageRecordReadFinished.TabIndex = 4;
            this.StorageRecordReadFinished.Text = "# Record Read Finished";
            this.StorageRecordReadFinished.UseVisualStyleBackColor = true;
            this.StorageRecordReadFinished.CheckedChanged += new System.EventHandler(this.StorageRecordReadFinished_CheckedChanged);
            // 
            // StorageRecordReadStart
            // 
            this.StorageRecordReadStart.AutoSize = true;
            this.StorageRecordReadStart.Location = new System.Drawing.Point(6, 90);
            this.StorageRecordReadStart.Name = "StorageRecordReadStart";
            this.StorageRecordReadStart.Size = new System.Drawing.Size(125, 17);
            this.StorageRecordReadStart.TabIndex = 3;
            this.StorageRecordReadStart.Text = "# Record Read Start";
            this.StorageRecordReadStart.UseVisualStyleBackColor = true;
            this.StorageRecordReadStart.CheckedChanged += new System.EventHandler(this.StorageRecordReadStart_CheckedChanged);
            // 
            // StorageRecordWritesFinished
            // 
            this.StorageRecordWritesFinished.AutoSize = true;
            this.StorageRecordWritesFinished.Location = new System.Drawing.Point(6, 67);
            this.StorageRecordWritesFinished.Name = "StorageRecordWritesFinished";
            this.StorageRecordWritesFinished.Size = new System.Drawing.Size(146, 17);
            this.StorageRecordWritesFinished.TabIndex = 2;
            this.StorageRecordWritesFinished.Text = "# Record Writes Finished";
            this.StorageRecordWritesFinished.UseVisualStyleBackColor = true;
            this.StorageRecordWritesFinished.CheckedChanged += new System.EventHandler(this.StorageRecordWritesFinished_CheckedChanged);
            // 
            // StorageRecordWriteStart
            // 
            this.StorageRecordWriteStart.AutoSize = true;
            this.StorageRecordWriteStart.Location = new System.Drawing.Point(6, 43);
            this.StorageRecordWriteStart.Name = "StorageRecordWriteStart";
            this.StorageRecordWriteStart.Size = new System.Drawing.Size(124, 17);
            this.StorageRecordWriteStart.TabIndex = 1;
            this.StorageRecordWriteStart.Text = "# Record Write Start";
            this.StorageRecordWriteStart.UseVisualStyleBackColor = true;
            this.StorageRecordWriteStart.CheckedChanged += new System.EventHandler(this.StorageRecordWriteStart_CheckedChanged);
            // 
            // StorageTimesStorageHasInitialized
            // 
            this.StorageTimesStorageHasInitialized.AutoSize = true;
            this.StorageTimesStorageHasInitialized.Location = new System.Drawing.Point(6, 19);
            this.StorageTimesStorageHasInitialized.Name = "StorageTimesStorageHasInitialized";
            this.StorageTimesStorageHasInitialized.Size = new System.Drawing.Size(169, 17);
            this.StorageTimesStorageHasInitialized.TabIndex = 0;
            this.StorageTimesStorageHasInitialized.Text = "# Times Storage has initialized";
            this.StorageTimesStorageHasInitialized.UseVisualStyleBackColor = true;
            this.StorageTimesStorageHasInitialized.CheckedChanged += new System.EventHandler(this.StorageTimesStorageHasInitialized_CheckedChanged);
            // 
            // RunBtn
            // 
            this.RunBtn.Enabled = false;
            this.RunBtn.Location = new System.Drawing.Point(331, 42);
            this.RunBtn.Name = "RunBtn";
            this.RunBtn.Size = new System.Drawing.Size(75, 23);
            this.RunBtn.TabIndex = 12;
            this.RunBtn.Text = "Run";
            this.RunBtn.UseVisualStyleBackColor = true;
            this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
            // 
            // LastRunTime
            // 
            this.LastRunTime.AutoSize = true;
            this.LastRunTime.Location = new System.Drawing.Point(804, 36);
            this.LastRunTime.Name = "LastRunTime";
            this.LastRunTime.Size = new System.Drawing.Size(13, 13);
            this.LastRunTime.TabIndex = 14;
            this.LastRunTime.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(691, 36);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "Last Runtime:";
            // 
            // FailedOpsLbl
            // 
            this.FailedOpsLbl.AutoSize = true;
            this.FailedOpsLbl.Location = new System.Drawing.Point(804, 50);
            this.FailedOpsLbl.Name = "FailedOpsLbl";
            this.FailedOpsLbl.Size = new System.Drawing.Size(13, 13);
            this.FailedOpsLbl.TabIndex = 16;
            this.FailedOpsLbl.Text = "0";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(691, 50);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 13);
            this.label6.TabIndex = 15;
            this.label6.Text = "Failed Operations";
            // 
            // RefreshCountBtn
            // 
            this.RefreshCountBtn.Location = new System.Drawing.Point(451, 13);
            this.RefreshCountBtn.Name = "RefreshCountBtn";
            this.RefreshCountBtn.Size = new System.Drawing.Size(87, 23);
            this.RefreshCountBtn.TabIndex = 17;
            this.RefreshCountBtn.Text = "Refresh Count";
            this.RefreshCountBtn.UseVisualStyleBackColor = true;
            this.RefreshCountBtn.Click += new System.EventHandler(this.RefreshCountBtn_Click);
            // 
            // ErrorsTxt
            // 
            this.ErrorsTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ErrorsTxt.Location = new System.Drawing.Point(575, 84);
            this.ErrorsTxt.Multiline = true;
            this.ErrorsTxt.Name = "ErrorsTxt";
            this.ErrorsTxt.Size = new System.Drawing.Size(436, 202);
            this.ErrorsTxt.TabIndex = 18;
            // 
            // DeleteDatabase
            // 
            this.DeleteDatabase.Location = new System.Drawing.Point(574, 13);
            this.DeleteDatabase.Name = "DeleteDatabase";
            this.DeleteDatabase.Size = new System.Drawing.Size(111, 23);
            this.DeleteDatabase.TabIndex = 19;
            this.DeleteDatabase.Text = "Delete Database";
            this.DeleteDatabase.UseVisualStyleBackColor = true;
            this.DeleteDatabase.Click += new System.EventHandler(this.DeleteDatabase_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1023, 900);
            this.Controls.Add(this.DeleteDatabase);
            this.Controls.Add(this.ErrorsTxt);
            this.Controls.Add(this.RefreshCountBtn);
            this.Controls.Add(this.FailedOpsLbl);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.LastRunTime);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.RunBtn);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.RecordCountLbl);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.OpenAndCloseBtn);
            this.Controls.Add(this.EnableTransactions);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.NoOfThreads);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.NoOfItems);
            this.Controls.Add(this.SysMonitorControl);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.SysMonitorControl)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AxSystemMonitor.AxSystemMonitor SysMonitorControl;
        private System.Windows.Forms.TextBox NoOfItems;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox NoOfThreads;
        private System.Windows.Forms.CheckBox EnableTransactions;
        private System.Windows.Forms.Button OpenAndCloseBtn;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label RecordCountLbl;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox YawnDBInitialized;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox StorageTimesStorageHasInitialized;
        private System.Windows.Forms.CheckBox YawnTransactionRollbackFailed;
        private System.Windows.Forms.CheckBox YawnTransactionRollbackStart;
        private System.Windows.Forms.CheckBox YawnTransactionCommitFinished;
        private System.Windows.Forms.CheckBox YawnTransactionCommitFail;
        private System.Windows.Forms.CheckBox YawnTransactionCommitStart;
        private System.Windows.Forms.CheckBox YawnTransactionsCreated;
        private System.Windows.Forms.CheckBox YawnTransactionRollbackFinished;
        private System.Windows.Forms.CheckBox StorageRecordWriteStart;
        private System.Windows.Forms.CheckBox StorageRecordWritesFinished;
        private System.Windows.Forms.CheckBox StorageRecordReadFinished;
        private System.Windows.Forms.CheckBox StorageRecordReadStart;
        private System.Windows.Forms.CheckBox StorageRecordReadsFromCache;
        private System.Windows.Forms.CheckBox StorageRecordDeleteStart;
        private System.Windows.Forms.CheckBox StorageRecordDeleteFinished;
        private System.Windows.Forms.CheckBox StorageRecordIndexingStart;
        private System.Windows.Forms.CheckBox StorageRecordIndexingFinished;
        private System.Windows.Forms.CheckBox StorageStorageResizes;
        private System.Windows.Forms.CheckBox StorageWriteContentions;
        private System.Windows.Forms.Button RunBtn;
        private System.Windows.Forms.Label LastRunTime;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label FailedOpsLbl;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button RefreshCountBtn;
        private System.Windows.Forms.TextBox ErrorsTxt;
        private System.Windows.Forms.Button DeleteDatabase;
    }
}

