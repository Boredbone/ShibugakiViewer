using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Extensions;
using Boredbone.Utility.Tools;
using System.Data;

namespace Test
{
    [TestClass]
    public class DatabaseTest
    {
        [TestMethod]
        public async Task DatabaseUsageTest()
        {
            await new Sample().Method1();
        }

        [TestMethod]
        public async Task DatabaseTimeTest()
        {
            await new Sample2().Test1();
        }
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

        public async Task Method1()
        {

            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                await this.database.InitializeAsync(connection);
            }
            

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

            var currentTime = DateTimeOffset.Now;

            items[1].DateModified = currentTime - TimeSpan.FromDays(2);
            items[2].DateModified= currentTime;

            await this.database.RequestTransactionAsync(async context =>
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

                Assert.AreEqual(items[0].Id, first.Id);
                Assert.AreEqual(testName, first.FileName);


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

                    Assert.AreEqual(4,results2.Length);

                    Assert.AreEqual(items[0].Id, results2[0].Id);
                    Assert.AreEqual(items[1].Id, results2[1].Id);
                    Assert.AreEqual(testName, results2[2].Id);
                    Assert.AreEqual(testName2, results2[3].Id);

                    var results3 = this.table1.AsQueryable(connection)
                        .Where($"{nameof(Record.FileName)} GLOB '*2_h*'")
                        .ToArray();

                    Console.WriteLine("MATCH:");

                    foreach (var item in results3)
                    {
                        Console.WriteLine(item.ToString());
                    }


                    Assert.AreEqual(2, results3.Length);

                    Assert.AreEqual(items[0].Id, results3[0].Id);
                    Assert.AreEqual(testName, results3[1].Id);
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

                    Assert.AreEqual(2, results2.Length);
                    Assert.AreEqual(items[0].Id, results2[0].Id);
                    Assert.AreEqual(testName, results2[1].Id);

                    var results3 = this.table1.AsQueryable(connection)
                        .Where($"lower({nameof(Record.FileName)}) GLOB '*[[]am]*'")
                        .ToArray();

                    Console.WriteLine("MATCH:");

                    foreach (var item in results3)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    Assert.AreEqual(2, results3.Length);
                    Assert.AreEqual(items[0].Id, results3[0].Id);
                    Assert.AreEqual(testName, results3[1].Id);
                }

                //
                // 普通にQueryメソッドを呼び出すと, 結果はIEnumerable<T>の形で返ってきます。
                //
                var results = this.table1.AsQueryable(connection)
                    .Where($"{nameof(Record.FileName)} LIKE '{testName.Replace("'", "''")}'")
                    .ToArray();
                // connection.Query<Table1>("SELECT * FROM table1");

                Console.WriteLine("A:");

                foreach (var item in results)
                {
                    Console.WriteLine(item.ToString());
                }

                Assert.AreEqual(2, results.Length);
                Assert.AreEqual(items[0].Id, results[0].Id);
                Assert.AreEqual(testName, results[1].Id);

                var rec = this.table1.GetRecordFromKeyAsync(connection, testName).Result;

                Console.WriteLine(rec.ToString());
                Assert.AreEqual(testName, rec.Id);
            }
            

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

                Assert.AreEqual(5, results.Length);
                Assert.AreEqual(items[0].Id, results[0].Id);
                Assert.AreEqual(items[1].Id, results[1].Id);
                Assert.AreEqual(items[2].Id, results[2].Id);
                Assert.AreEqual(testName, results[3].Id);
                Assert.AreEqual(testName2, results[4].Id);
            }


            await this.database.RequestTransactionAsync(async context =>
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
                Assert.AreEqual(5, results.Length);
                Assert.AreEqual(items[0].Id, results[0].Id);
                Assert.AreEqual(items[1].Id, results[1].Id);
                Assert.AreEqual(items[2].Id, results[2].Id);
                Assert.AreEqual(testName, results[3].Id);
                Assert.AreEqual(testName2, results[4].Id);
            }

            await this.database.RequestTransactionAsync(async context =>
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
                Assert.AreEqual(5, results.Length);
                Assert.AreEqual(items[0].Id, results[0].Id);
                Assert.AreEqual(items[1].Id, results[1].Id);
                Assert.AreEqual(items[2].Id, results[2].Id);
                Assert.AreEqual(testName, results[3].Id);
                Assert.AreEqual(testName2, results[4].Id);
            }

            //return;

            using (var connection = this.database.Connect())
            {
                Console.WriteLine("a");
                //0001-01-01 00:00:00 +00:00
                Assert.AreEqual("0",//"0001-01-01 00:00:00 +00:00", 
                    this.table1.AsQueryable(connection)
                    .Select<string>(nameof(Record.DateModified))
                    .ToArray()
                    .First());

                Console.WriteLine("b");
                //0001-01-01
                Assert.AreEqual((-DateTimeOffset.Now.Offset.TotalSeconds).ToString(),
                    this.table1.AsQueryable(connection)
                    .Select<string>(DatabaseFunction.GetDate(nameof(Record.DateModified)))
                    .ToArray()
                    .First());

                Console.WriteLine("c");
                ////00:00:00
                //Assert.AreEqual("00:00:00",
                //    this.table1.AsQueryable(connection)
                //    .Select<string>($"time({nameof(Record.DateModified)})")
                //    .ToArray()
                //    .First());

                //0001/01/01 0:00:00 +00:00
                Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(0), this.table1.AsQueryable(connection)
                    .Select<DateTimeOffset>(nameof(Record.DateModified))
                    .ToArray()
                    .First());


                Console.WriteLine("d");
                var today = DateTimeOffset.Now.Date;

                var mods = this.table1.AsQueryable(connection)
                    .Where($"{DatabaseFunction.GetDate(nameof(Record.DateModified))}=={UnixTime.FromDateTime(today.Date)}")
                    .Select<DateTimeOffset>($"{nameof(Record.DateModified)}")
                    .Take(2)
                    .ToArray();

                Console.WriteLine("e");
                Assert.AreEqual(1, mods.Length);

                Console.WriteLine("f");
                Assert.AreEqual(new DateTimeOffset(currentTime.Date)
                    .AddHours(currentTime.Hour)
                    .AddMinutes(currentTime.Minute)
                    .AddSeconds(currentTime.Second),
                    mods[0]);

                Console.WriteLine("g");
            }
            
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
        
    }
    class Sample2
    {

        private DatabaseFront database;
        private TypedTable<Table1, string> table1;

        public Sample2()
        {
            var dbPath = @"test2.db";


            this.database = new DatabaseFront(dbPath);
            this.table1 = new TypedTable<Table1, string>(this.database, "table1")
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

        public async Task Test1()
        {
            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                await this.database.InitializeAsync(connection);
            }
            using (var connection = this.database.Connect())
            {
                Debug.WriteLine("a");
                var date = DateTimeOffset.Now;
                await this.TestUnixDate(date, connection);

                Debug.WriteLine("b");
                var d2 = new DateTimeOffset(date.Year, date.Month, date.Day,
                    date.Hour, date.Minute, date.Second, TimeSpan.FromHours(-10.5));
                await this.TestUnixDate(d2, connection);

                Debug.WriteLine("c");
                var d3 = new DateTimeOffset(date.Year, date.Month, date.Day,
                    date.Hour, date.Minute, date.Second, TimeSpan.FromHours(0));
                await this.TestUnixDate(d3, connection);

            }
        }

        private async Task TestUnixDate(DateTimeOffset date,IDbConnection connection)
        {

            {
                var dateNum = await table1.QueryAsync<long>
                    (connection, $"Select strftime('%s','{date.ToString("yyyy-MM-dd HH:mm:ss zzz")}')");

                this.AreEqual(date.ToUnixTimeSeconds(), dateNum.FirstOrDefault());
            }
            this.AreEqual(date.ToUniversalTime().ToUnixTimeSeconds(), date.ToUnixTimeSeconds());

            {
                var dateNum = await table1.QueryAsync<long>
                    (connection, $"Select strftime('%s','{date.ToString("yyyy-MM-dd HH:mm:ss")}')");

                if (date.Offset == default(TimeSpan))
                {
                    this.AreEqual(date.ToUnixTimeSeconds(), dateNum.FirstOrDefault());
                }
                else
                {
                    this.AreNotEqual(date.ToUnixTimeSeconds(), dateNum.FirstOrDefault());
                }
            }
            {
                var dateNum = await table1.QueryAsync<long>
                    (connection, $"Select strftime('%s','{date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}')");

                this.AreEqual(date.ToUnixTimeSeconds(), dateNum.FirstOrDefault());
            }

            {
                var unixTime = $"strftime('%s','{date.ToString("yyyy-MM-dd HH:mm:ss zzz")}')";
                var unixDate = DatabaseFunction.GetDate(unixTime);

                var dateNum = await table1.QueryAsync<long>
                    (connection, $"Select {unixDate}");

                this.AreEqual(DatabaseFunction.DateOffsetReference(date), dateNum.FirstOrDefault().ToString());
            }
            {
                var unixTime = $"strftime('%s','{date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}')";
                var unixDate = DatabaseFunction.GetDate(unixTime);

                var dateNum = await table1.QueryAsync<long>
                    (connection, $"Select {unixDate}");

                this.AreEqual(DatabaseFunction.DateOffsetReference(date.ToUniversalTime()), dateNum.FirstOrDefault().ToString());
            }
            {
                var container = new Table1{ DateOffset = date };

                using(var tr = connection.BeginTransaction())
                {
                    await table1.ReplaceAsync(container, connection, tr);
                    tr.Commit();
                }

                container.DateOffset = date.AddSeconds(1);

                var dateNum = await table1.QueryAsync<string>
                    (connection, $"Select DateOffset FROM {table1.Name} WHERE (DateOffset+1) == @DateOffset", container);

                Assert.AreEqual(1, dateNum.Count());
                this.AreEqual(UnixTime.FromDateTime(date).ToString(), dateNum.FirstOrDefault());
            }
        }


        public async Task Test2()
        {
            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                await this.database.InitializeAsync(connection);
            }


            //1970年より前

            //3000年

            //null

            //異なるオフセット

            //現在地の日付で検索

            //


        }

        private void AreEqual<T>(T a, T b)
        {
            Debug.WriteLine((a?.ToString() ?? "null") + ", " + (b?.ToString() ?? "null"));
            Assert.AreEqual(a, b);
        }
        private void AreNotEqual<T>(T a, T b)
        {
            Debug.WriteLine((a?.ToString() ?? "null") + ", " + (b?.ToString() ?? "null"));
            Assert.AreNotEqual(a, b);
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
            public DateTimeOffset DateOffset { get; set; }
            [RecordMember]
            public DateTime Date { get; set; }
            [RecordMember]
            public DateTimeOffset? DateOffsetNull { get; set; } = null;
            [RecordMember]
            public DateTime? DateNull { get; set; } = null;
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

