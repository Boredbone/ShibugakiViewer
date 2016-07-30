using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Tools;
using Database.Search;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.File;

namespace Cui
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                var path = @"";
                var information = new GraphicInformation(path);
                Console.WriteLine($"{information.Type},{information.GraphicSize}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }



            Console.ReadLine();


            //var t = test();
            //Console.ReadLine();
            new Sample().Method1();
            Console.ReadLine();
        }
    }

    public class ImageFileInformation
    {
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset DateModified { get; set; }

        public long Size { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public int Rating { get; set; }
        public HashSet<string> Keywords { get; set; }

        public string Name { get; set; }
        public string Path { get; set; }
        
    }

    class Sample
    {

        private DatabaseFront database;
        private TypedTable<Record, string> table1;

        public Sample()
        {
            var dbPath = @"test.db";


            this.database = new DatabaseFront(dbPath);
            this.table1 = new TypedTable<Record, string>(this.database, "table1")
            {
                IsIdAuto = false,
                Version = 2,
            };

            //table1.AddColumnOption(nameof(Table1.NewNumber), "DEFAULT 3");
            //table1.AddColumnOption(nameof(Table1.NewText), "DEFAULT 'あおえ'");
            //table1.AddColumnOption(nameof(Table1.IsEnabled), "DEFAULT 1");
            //
            //table1.Migrating += (o, e) =>
            //{
            //    if (e.TableInformations.First(x => x.TableName.Equals(this.table1.Name))
            //        .Modified < new DateTime(2016, 1, 1))
            //    {
            //        e.Converters[nameof(Table1.NewNumber)] = "(SubNumber*2)+5";
            //    }
            //};
        }

        private string GetId() => Guid.NewGuid().ToString();

        public void Method1()
        {



            Console.WriteLine("start");
            //Console.ReadLine();

            /*
            var props = typeof(TableColumnDefinition)
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .ToArray();

            foreach (var m in props)
            {
                Console.WriteLine($"{m.PropertyType} - {m.Name}");
            }
            */

            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                this.database.Initialize(connection);
            }

            Console.WriteLine("connected");
            Console.ReadLine();

            LibraryOwner.SetConfig(new LibraryConfiguration(""));

            /*
            using (var connection = this.database.Connect())
            {
                try
                {
                    var info = this.table1.GetColumnInformations(connection);
                    foreach (var item in info)
                    {
                        Console.WriteLine(item);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }*/


            //Console.ReadLine();

            //var items = new[]
            //{
            //    new Table1() { Id = GetId(), Name = "Name-1" }.AsNumbered().WithTimeStamp(),
            //    new Table1() { Id = GetId(), Name = "Name-2" }.AsNumbered().WithTimeStamp(),
            //    new Table1() { Id = GetId(), Name = "Name-3",IsEnabled=true }.AsNumbered().WithTimeStamp(),
            //};
            var items = new[]
            {
                new Record(GetId()),
                new Record(GetId()),
                new Record(GetId()),
            };

            var testName = "n[Am]e'ji%22_h'''l";
            items[0].SetName(testName);


            var testName2 = testName.Replace('_', 'f').Replace(']', '-');
            items[1].SetName(testName2);

            this.database.RequestTransaction(async context =>
            {
                foreach (var item in items)
                {
                    await this.table1.AddAsync(item, context);
                }

                await this.table1.AddAsync(new Record(testName), context);
                await this.table1.AddAsync(new Record(testName2), context);
            });

            /*
            this.database.RequestTransaction(context =>
            {
                this.table1.Execute
                ($"UPDATE {this.table1.Name} SET ()=()",
                    context);
            });*/


            using (var connection = this.database.Connect())
            {
                var first = this.table1.AsQueryable(connection).FirstOrDefault();
                //var mappingObj = connection.Query<Table1>("SELECT * FROM table1 LIMIT 1").FirstOrDefault();

                Console.WriteLine("Id: {0}", first.Id);
                Console.WriteLine("Name: {0}", first.FileName);
                {
                    var results2 = this.table1.AsQueryable(connection)
                        .Where($"{nameof(Record.FileName)} LIKE '%2_h%'")
                        .ToArray();

                    Console.WriteLine("LIKE:");

                    foreach (var item in results2)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    var results3 = this.table1.AsQueryable(connection)
                        .Where($"{nameof(Record.FileName)} GLOB '*2_h*'")
                        .ToArray();

                    Console.WriteLine("MATCH:");

                    foreach (var item in results3)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    Console.ReadLine();
                }

                {
                    var results2 = this.table1.AsQueryable(connection)
                        .Where($"{nameof(Record.FileName)} LIKE '%[am]%'")
                        .ToArray();

                    Console.WriteLine("LIKE:");

                    foreach (var item in results2)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    var results3 = this.table1.AsQueryable(connection)
                        .Where($"lower({nameof(Record.FileName)}) GLOB '*[[]am]*'")
                        .ToArray();

                    Console.WriteLine("MATCH:");

                    foreach (var item in results3)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    Console.ReadLine();
                }

                //
                // 普通にQueryメソッドを呼び出すと, 結果はIEnumerable<T>の形で返ってきます。
                //
                var results = this.table1.AsQueryable(connection)
                    .Where($"{nameof(Record.FileName)} LIKE '{testName.Replace("'","''")}'")
                    .ToArray();
                // connection.Query<Table1>("SELECT * FROM table1");

                Console.WriteLine("A:");

                foreach (var item in results)
                {
                    Console.WriteLine(item.ToString());
                }

                var rec = this.table1.GetRecordFromKeyAsync(connection, testName).Result;

                Console.WriteLine(rec.ToString());
            }

            Console.ReadLine();

            Record[] items2 = null;
            using (var connection = this.database.Connect())
            {
                var results = this.table1.AsQueryable(connection).ToArray();

                items2 = results;

                Console.WriteLine("B:");

                foreach (var item in results)
                {
                    Console.WriteLine(item.ToString());
                }
            }


            this.database.RequestTransaction(async context =>
            {
                items2[1].SetName("Edited1");
                items2[1].Height = 22;
                await this.table1.UpdateAsync(items2[1], context);

                items2[2].SetName("Edited2");
                items2[2].Height = 58;
                await this.table1.UpdateAsync(items2[2], context, nameof(Record.FileName));
            });
            
            

            using (var connection = this.database.Connect())
            {
                var results = this.table1.AsQueryable(connection).ToArray();

                Console.WriteLine("C:");

                foreach (var item in results)
                {
                    Console.WriteLine(item.ToString());
                }
            }

            this.database.RequestTransaction(async context =>
            {
                await this.table1.UpdateAsync(items2[2], context);
            });
            

            using (var connection = this.database.Connect())
            {
                var results = this.table1.AsQueryable(connection).ToArray();

                Console.WriteLine("D:");

                foreach (var item in results)
                {
                    Console.WriteLine(item.ToString());
                }
            }

            //return;
            
            using (var connection = this.database.Connect())
            {
                //0001-01-01 00:00:00 +00:00
                this.table1.AsQueryable(connection)
                    .Select<string>($"{nameof(Record.DateModified)}")
                    .ToArray()
                    .ForEach(x => Console.WriteLine(x.ToString()));

                //0001-01-01
                this.table1.AsQueryable(connection)
                    .Select<string>($"date({nameof(Record.DateModified)})")
                    .ToArray()
                    .ForEach(x => Console.WriteLine(x.ToString()));

                //00:00:00
                this.table1.AsQueryable(connection)
                    .Select<string>($"time({nameof(Record.DateModified)})")
                    .ToArray()
                    .ForEach(x => Console.WriteLine(x.ToString()));

                //0001/01/01 0:00:00 +00:00
                this.table1.AsQueryable(connection)
                    .Select<DateTimeOffset>($"date({nameof(Record.DateModified)})")
                    .ToArray()
                    .ForEach(x => Console.WriteLine(x.ToString()));


                var today = DateTimeOffset.Now.Date;

                this.table1.AsQueryable(connection)
                    .Where($"date({nameof(Record.DateModified)})=='{DatabaseFunction.DateToString(today)}'")
                    .Select<string>($"{nameof(Record.DateModified)}")
                    .Take(2)
                    .ToArray()
                    .ForEach(x => Console.WriteLine(x.ToString()));

            }

            Console.ReadLine();
            /*

            using (var connection = this.database.Connect())
            {
                try
                {
                    var info = this.table1.GetColumnInformations(connection);
                    foreach (var item in info)
                    {
                        Console.WriteLine(item);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }*/
            
        }
        

        internal class Table1 : IRecord<string>
        {
            [RecordMember]
            public string Id { get; set; }
            [RecordMember]
            public string Name { get; set; }
            [RecordMember]
            public double Number { get; private set; }
            [RecordMember]
            public DateTimeOffset DateOffset { get; private set; }
            [RecordMember]
            public DateTime Date { get; private set; }
            [RecordMember]
            public DateTimeOffset? DateOffsetNull { get; private set; } = null;
            [RecordMember]
            public DateTime? DateNull { get; private set; } = null;
            //[RecordMember]
            //public int SubNumber { get; set; }
            [RecordMember]
            public int NewNumber { get; set; }

            [RecordMember]
            public string NewText { get; set; }

            [RecordMember]
            public bool IsEnabled { get; set; }

            public int NotMapped1 { get; set; }

            private static int count = 5;

            public Table1()
            {
            }

            public Table1 AsNumbered()
            {
                this.Number = count++;
                return this;
            }

            public Table1 WithTimeStamp()
            {
                this.Date = DateTime.Now;
                this.DateOffset = DateTimeOffset.Now;
                return this;

            }

            public override string ToString()
            {
                return $"{this.Id}, {this.Name}, {this.Number}, {this.Date}, {this.DateOffset}, {this.NewNumber}";
            }
        }
    }
}
