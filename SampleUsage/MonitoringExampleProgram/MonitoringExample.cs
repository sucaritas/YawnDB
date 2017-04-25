namespace MonitoringExampleProgram
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Reflection;
    using SystemMonitor;
    using School;

    public partial class MonitoringExample : Form
    {
        private SampleYawnDB.MyDataBase myDB = new SampleYawnDB.MyDataBase(@".\SchoolDatabase");
        private bool isOpened = false;
        private int failed = 0;
        private Task runner;
        private Task countRunner;
        private HashSet<long> idsCreated = new HashSet<long>();
        private object syncLock = new object();
        private object errorSyncLock = new object();
        private object createSyncLock = new object();
        private List<string> errors = new List<string>();

        public MonitoringExample()
        {
            InitializeComponent();
            myDB.Open(false);
            SysMonitorControl.BackColor = System.Drawing.Color.Black;
            SysMonitorControl.BackColorCtl = System.Drawing.Color.Gray;
            SysMonitorControl.GridColor = System.Drawing.Color.Gray;
            SysMonitorControl.ShowToolbar = true;
            SysMonitorControl.ShowValueBar = true;
            SysMonitorControl.ShowTimeAxisLabels = true;
            SysMonitorControl.ShowVerticalGrid = true;
            SysMonitorControl.ShowHorizontalGrid = true;
            SysMonitorControl.ShowLegend = true;
            SysMonitorControl.ChartScroll = false;
            SysMonitorControl.MaximumScale = 25000;
            myDB.Close();
        }

        private void OpenAndCloseBtn_Click(object sender, EventArgs e)
        {
            if (isOpened)
            {
                myDB.Close();
                isOpened = false;
                OpenAndCloseBtn.Text = "Open database";
                OpenAndCloseBtn.ForeColor = Color.Red;
                EnableTransactions.Enabled = true;
                RunBtn.Enabled = false;
                return;
            }

            myDB.Open(EnableTransactions.Checked);
            OpenAndCloseBtn.Text = "Close database";
            isOpened = true;
            OpenAndCloseBtn.ForeColor = Color.Green;
            EnableTransactions.Enabled = false;
            RunBtn.Enabled = true;
        }

        #region YawnBD Counters
        private void YawnDBInitialized_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item = null;
            if (YawnDBInitialized.Checked)
            {
                SysMonitorControl.DeleteCounter(item);
            }

            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnDBInitialized.Text, out item);
        }

        private void YawnTransactionsCreated_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionsCreated.Text, out item);
        }

        private void YawnTransactionCommitStart_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionCommitStart.Text, out item);
        }

        private void YawnTransactionCommitFail_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionCommitFail.Text, out item);
        }

        private void YawnTransactionCommitFinished_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionCommitFinished.Text, out item);
        }

        private void YawnTransactionRollbackStart_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionRollbackStart.Text, out item);
        }

        private void YawnTransactionRollbackFailed_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionRollbackFailed.Text, out item);
        }

        private void YawnTransactionRollbackFinished_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            SysMonitorControl.AddCounter("\\YawnDB Counters(*)\\" + YawnTransactionRollbackFinished.Text, out item);
        }
        #endregion

        #region Storage Counters
        private void StorageTimesStorageHasInitialized_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageTimesStorageHasInitialized.Checked)
            {
                item = this.GetCounter(StorageTimesStorageHasInitialized.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageTimesStorageHasInitialized.Text, out item);
        }

        private void StorageRecordWriteStart_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordWriteStart.Checked)
            {
                item = this.GetCounter(StorageRecordWriteStart.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordWriteStart.Text, out item);
        }

        private void StorageRecordWritesFinished_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordWritesFinished.Checked)
            {
                item = this.GetCounter(StorageRecordWritesFinished.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordWritesFinished.Text, out item);
        }

        private void StorageRecordReadStart_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordReadStart.Checked)
            {
                item = this.GetCounter(StorageRecordReadStart.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordReadStart.Text, out item);
        }

        private void StorageRecordReadFinished_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordReadFinished.Checked)
            {
                item = this.GetCounter(StorageRecordReadFinished.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordReadFinished.Text, out item);
        }

        private void StorageRecordReadsFromCache_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordReadsFromCache.Checked)
            {
                item = this.GetCounter(StorageRecordReadsFromCache.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }
            
            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordReadsFromCache.Text, out item);
        }

        private void StorageRecordDeleteStart_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordDeleteStart.Checked)
            {
                item = this.GetCounter(StorageRecordDeleteStart.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordDeleteStart.Text, out item);
        }

        private void StorageRecordDeleteFinished_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordDeleteFinished.Checked)
            {
                item = this.GetCounter(StorageRecordDeleteFinished.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordDeleteFinished.Text, out item);
        }

        private void StorageRecordIndexingStart_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordIndexingStart.Checked)
            {
                item = this.GetCounter(StorageRecordIndexingStart.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }
            
            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordIndexingStart.Text, out item);
        }

        private void StorageRecordIndexingFinished_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageRecordIndexingFinished.Checked)
            {
                item = this.GetCounter(StorageRecordIndexingFinished.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageRecordIndexingFinished.Text, out item);
        }

        private void StorageStorageResizes_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageStorageResizes.Checked)
            {
                item = this.GetCounter(StorageStorageResizes.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageStorageResizes.Text, out item);
        }

        private void StorageWriteContentions_CheckedChanged(object sender, EventArgs e)
        {
            ICounterItem item;
            if (!StorageWriteContentions.Checked)
            {
                item = this.GetCounter(StorageWriteContentions.Text);
                if (item != null)
                {
                    SysMonitorControl.DeleteCounter(item);
                }

                return;
            }

            SysMonitorControl.AddCounter("\\YawnDB Storage Counters(*)\\" + StorageWriteContentions.Text, out item);
        }
        #endregion

        private ICounterItem GetCounter(string counterName)
        {
            foreach (ICounterItem counter in SysMonitorControl.Counters)
            {
                if (counter.Path.ToString().EndsWith(counterName))
                {
                    return counter;
                }
            }

            return null;
        }

        private void RunBtn_Click(object sender, EventArgs e)
        {
            RunBtn.Enabled = false;
            this.runner = new Task(() => runTest());
            this.runner.Start();
        }

        private void runTest()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            int noThreads = int.Parse(this.NoOfThreads.Text);
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Reset();
            timer.Start();
            Task[] threads = new Task[noThreads];
            for (int i = 0; i < noThreads; i++)
            {
                threads[i] = new Task(Insert);
                threads[i].Start();
            }

            Task.WaitAll(threads);
            timer.Stop();

            foreach (var x in threads)
            {
                x.Dispose();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            SetControlPropertyThreadSafe(LastRunTime, "Text", timer.ElapsedMilliseconds.ToString() + " ms");
            //SetControlPropertyThreadSafe(RecordCountLbl, "Text", myDB.Students.Count() + " Students");
            SetControlPropertyThreadSafe(RunBtn, "Enabled", true);
            SetControlPropertyThreadSafe(FailedOpsLbl, "Text", this.failed + " Failures");
            SetControlPropertyThreadSafe(ErrorsTxt, "Text", string.Join(Environment.NewLine + "_______________________" + Environment.NewLine, errors));
        }

        private void Insert()
        {
            int insertCount = int.Parse(this.NoOfItems.Text);
            string[] names = new[] { "Julio", "Miguel", "Marco", "Omar", "Rene" };
            string[] lastNames = new[] { "Saenz", "Telles", "Ruelas", "Quirino", "Sandoval" };
            int[] ages = new[] { 37, 38, 39, 43, 17 };
            Random rnd = new Random();

            for (int i = 0; i < insertCount; i++)
            {
                Student student;
                lock (createSyncLock)
                {
                    student = myDB.CreateRecord<Student>();
                }

                student.Age = ages[rnd.Next(5)];
                student.FirstName = names[rnd.Next(5)];
                student.LastName = lastNames[rnd.Next(5)];

                try
                {
                    if (myDB.SaveRecord(student) == null)
                    {
                        lock (this.errorSyncLock)
                        {
                            errors.Add("id: " + student.Id + ", was not saved");
                        }

                        this.failed++;
                    }

                    if (this.idsCreated.Contains(student.Id))
                    {
                        lock (this.errorSyncLock)
                        {
                            errors.Add("id: " + student.Id + ", is duplicate");
                        }

                        this.failed++;
                    }
                }
                catch(Exception e)
                {
                    this.failed++;
                    lock (this.errorSyncLock)
                    {
                        errors.Add("id: " + student.Id + "; " + e.Message);
                    }
                }

                lock (this.syncLock)
                {
                    this.idsCreated.Add(student.Id);
                }
            }
        }

        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        public static void SetControlPropertyThreadSafe(Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate
                (SetControlPropertyThreadSafe),
                new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    null,
                    control,
                    new object[] { propertyValue });
            }
        }

        private void RefreshCountBtn_Click(object sender, EventArgs e)
        {
           
            this.countRunner = new Task(()=>
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Reset();
                timer.Start();
                SetControlPropertyThreadSafe(RecordCountLbl, "Text", myDB.Students.Count().ToString("N0") + " Students");
                timer.Stop();
                SetControlPropertyThreadSafe(LastRunTime, "Text", timer.ElapsedMilliseconds.ToString("N0") + " ms");
            });

            
            this.countRunner.Start();
        }

        private void DeleteDatabase_Click(object sender, EventArgs e)
        {
            if (isOpened)
            {
                this.OpenAndCloseBtn_Click(sender, e);
            }

            this.idsCreated = new HashSet<long>();
            System.IO.Directory.Delete(@".\SchoolDatabase", true);
            this.myDB = new SampleYawnDB.MyDataBase(@".\SchoolDatabase");
        }
    }
}