namespace SampleUsageOfAYawnDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;
    using School;
    using Microsoft.Diagnostics.Tracing.Session;
    using YawnDB.EventSources;
    using YawnDB.PerformanceCounters;
    using System.Threading;
    using System.Data.SqlClient;

    class Program
    {
        private static SampleYawnDB.MyDataBase myDB = new SampleYawnDB.MyDataBase(@".\");
        private static int insertCount = 10;
        static void Main(string[] args)
        {
            Console.WriteLine("Setting up counters");
            if (StorageCounters.SetupCounters())
            {
                return;
            }

            Console.WriteLine("Initializing schema storage");
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            myDB.Open();
            Console.WriteLine("Finished in " + timer.ElapsedMilliseconds);
            timer.Stop();

            int runningInsertCount = 0;
            long runningInsertTime = 0;
            int noThreads = 1;

            while (true)
            {
                Console.Write("How many should i insert:");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }
                insertCount = int.Parse(input);

                Console.Write("How many threads should i branch of:");
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                noThreads = int.Parse(input);

                timer.Reset();
                timer.Start();
                Task[] threads = new Task[noThreads];
                for (int i = 0; i < noThreads; i++)
                {
                    threads[i] = new Task(Insert);
                    threads[i].Start();
                }

                Task.WaitAll(threads);

                runningInsertCount += insertCount;
                runningInsertTime += timer.ElapsedMilliseconds;
                timer.Stop();

                Console.WriteLine("Inserted " + insertCount * noThreads + " in " + timer.ElapsedMilliseconds + "ms");
                Console.WriteLine("Total inserted ");
                Console.WriteLine("Total time so far " + runningInsertTime + "ms");

                timer.Reset();
                //timer.Start();
                //var myClass = myDB.CreateRecord<Classes>();
                //myClass.Students.RefrencedIds.AddLast(1);
                //myClass.Students.RefrencedIds.AddLast(2);
                //myClass.Students.RefrencedIds.AddLast(3);

                //var res = myDB.Students.Select(x => x.FirstName);
                //var res = myClass.Students.Select(x => x.FirstName);
                //var res = myClass.Students;
                //var results = res.ToArray();
                //timer.Stop();
                //Console.WriteLine("Enumerated ALL ("+ results.Length.ToString("0,0")+ ") in  " + timer.ElapsedMilliseconds+"ms");
                //foreach (var st in results)
                //{
                //    Console.WriteLine(st.Id + ".-" + st.FirstName + " " + st.LastName + " - " + st.Age);
                //}
                Console.WriteLine("___________________________________________________________________");

                //timer.Reset();
                //timer.Start();
                //using (SqlConnection connection = new SqlConnection("Server=Rougarou\\SQLEXpress;Database=testingYwanDB;Trusted_Connection=True;"))
                //{
                //    connection.Open();
                //    SqlCommand command = connection.CreateCommand();

                //    for (int i = 0; i < insertCount; i++)
                //    {
                //        command.CommandText = "INSERT INTO Students(FirstName, LastName, Age) VALUES('Julio', 'Saenz', " + i + ")";
                //        command.ExecuteNonQuery();
                //    }
                //}
                //timer.Stop();
                //Console.WriteLine("MSSQL Insert " + insertCount + " in " + timer.ElapsedMilliseconds + "ms");
            }
            myDB.Close();
        }

        private static void Insert()
        {
            string[] names = new[] { "Julio", "Miguel", "Marco", "Omar", "Rene" };
            string[] lastNames = new[] { "Saenz", "Telles", "Ruelas", "Quirino", "Sandoval" };
            int[] ages = new[] { 37, 38, 39, 43, 17 };
            Random rnd = new Random();

            for (int i = 0; i < insertCount; i++)
            {
                var student = myDB.CreateRecord<Student>();
                student.Age = ages[rnd.Next(5)];
                student.FirstName = names[rnd.Next(5)];
                student.LastName = lastNames[rnd.Next(5)];

                myDB.SaveRecord(student).ContinueWith(loc =>
                {
                    if(loc.Result == null)
                    {
                        Console.WriteLine("Null at " + i);
                    }
                });
            }


        }
    }
}
