﻿using System;
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
using Boredbone.Utility;

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
        public async Task DatabaseNameTest()
        {
            await new Sample().NameTest();
        }

        [TestMethod]
        public async Task TimeOperationTest()
        {
            await new Sample().Method2(DateTimeOffset.Now.Offset);
            await new Sample().Method2(TimeSpan.FromHours(3));
            await new Sample().Method2(TimeSpan.FromHours(-7.5));
            await new Sample().Method2(TimeSpan.FromHours(0));
        }

        [TestMethod]
        public async Task DatabaseTimeTest()
        {
            await new Sample2().Test1(DateTimeOffset.Now.Offset);
            await new Sample2().Test1(TimeSpan.FromHours(5));
            await new Sample2().Test1(TimeSpan.FromHours(-10.5));
            await new Sample2().Test1(TimeSpan.FromHours(0));
        }

        [TestMethod]
        public async Task DatabaseTimeTest2()
        {
            await new Sample2().Test2(DateTimeOffset.Now.Offset);
            await new Sample2().Test2(TimeSpan.FromHours(0.7));
            await new Sample2().Test2(TimeSpan.FromHours(-4.4));
            await new Sample2().Test2(TimeSpan.FromHours(0));
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

            var testName = "n[Am]e'j)(i%2@2_h'''l @p1";
            items[0].SetName(testName);


            var testName2 = testName.Replace('_', 'f').Replace(']', '-');
            items[1].SetName(testName2);

            var currentTime = DateTimeOffset.Now;

            items[1].DateModified = currentTime - TimeSpan.FromDays(2);
            items[2].DateModified = currentTime;

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
                    var results2 = await this.SearchAsync(connection, $"{nameof(Record.FileName)} LIKE '%2_h%'");

                    Console.WriteLine("LIKE:");


                    foreach (var item in results2)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    Assert.AreEqual(4, results2.Length);

                    Assert.AreEqual(items[0].Id, results2[0].Id);
                    Assert.AreEqual(items[1].Id, results2[1].Id);
                    Assert.AreEqual(testName, results2[2].Id);
                    Assert.AreEqual(testName2, results2[3].Id);

                    var results3 = await this.SearchAsync(connection, $"{nameof(Record.FileName)} GLOB '*2_h*'");

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
                    var results2 = await this.SearchAsync(connection, $"{nameof(Record.FileName)} LIKE '%[am]%'");

                    Console.WriteLine("LIKE:");

                    foreach (var item in results2)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    Assert.AreEqual(2, results2.Length);
                    Assert.AreEqual(items[0].Id, results2[0].Id);
                    Assert.AreEqual(testName, results2[1].Id);

                    var results3 = await this.SearchAsync
                        (connection, $"lower({nameof(Record.FileName)}) GLOB '*[[]am]*'");

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

                    var results3 = await this.SearchAsync
                        (connection, $"{nameof(Record.FileName)} GLOB '* @p1*'");

                    Assert.AreEqual(4, results3.Length);
                }

                {
                    var sql = $"\\n \n \"uu\"u \r\n <4 >0?? '' ';\"' select * from table1 where Id== @p1*--";

                    var results3 = await this.SearchAsync
                        (connection, $"Id == '{items[0]}' OR {nameof(Record.FileName)} == '{sql.Replace("'", "''")}'");

                    Assert.AreEqual(1, results3.Length);

                    var results4 = await this.SearchAsync
                        (connection, $"{nameof(Record.FileName)} == '{sql.Replace("'", "''")}' OR Id == '{items[0]}'");

                    Assert.AreEqual(1, results4.Length);


                    Assert.AreEqual(results3[0].Id, results4[0].Id);
                }

                var results = await this.SearchAsync
                    (connection, $"{nameof(Record.FileName)} LIKE '{testName.Replace("'", "''")}'");
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

        public async Task Method2(TimeSpan offset)
        {
            //if (offset != DateTimeOffset.Now.Offset)
            {
                DatabaseFunction.SetDateOffset(offset);
            }

            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                await this.database.InitializeAsync(connection);
            }


            LibraryOwner.SetConfig(new LibraryConfiguration(""));
            
            var items = new[]
            {
                new Record(GetId()),
                new Record(GetId()),
                new Record(GetId()),
            };

            var testName = "n[Am]e'j)(i%2@2_h'''l @p1";
            items[0].SetName(testName);


            var testName2 = testName.Replace('_', 'f').Replace(']', '-');
            items[1].SetName(testName2);

            var currentTime = DateTimeOffset.Now.ToOffset(offset);

            items[1].DateModified = currentTime - TimeSpan.FromDays(2);
            items[2].DateModified = currentTime;

            await this.database.RequestTransactionAsync(async context =>
            {
                foreach (var item in items)
                {
                    await this.table1.AddAsync(item, context);
                }

                await this.table1.AddAsync(new Record(testName), context);
                await this.table1.AddAsync(new Record(testName2), context);
            });
            

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
                Assert.AreEqual(((offset.TotalSeconds != 0) ? (-offset.TotalSeconds) : offset.TotalSeconds).ToString(),
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
                var today = DateTimeOffset.Now.ToOffset(offset).ToDate();


                var mods = this.table1.AsQueryable(connection)
                    .Where(DatabaseExpression.AreEqual
                    (DatabaseFunction.GetDate(nameof(Record.DateModified)),
                    new DatabaseReference(UnixTime.FromDateTime(today).ToString())))
                    .Select<DateTimeOffset>($"{nameof(Record.DateModified)}")
                    .Take(2)
                    .ToArray();

                Console.WriteLine("e");
                Assert.AreEqual(1, mods.Length);

                Console.WriteLine("f");
                this.AreEqual(new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day,
                    currentTime.Hour, currentTime.Minute, currentTime.Second, offset),
                    mods[0]);

                Console.WriteLine("g");
            }
        }

        public async Task NameTest()
        {

            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);
                await this.database.InitializeAsync(connection);
            }


            LibraryOwner.SetConfig(new LibraryConfiguration(""));

            var path = new string[]{
                @"I:\f2\metro\Éclair\Windows-8-Wallpaper-Tile-Room-Blue_2.jpg",
                @"I:\f2\metro\ＡｂＣ\Windows-8-Wallpaper-Tile-Room-Blue_2.jpg",
                @"I:\f2\metro\ａｂｃ\Windows-8-Wallpaper-Tile-Room-Blue_2.jpg",
            };

            var items = path.Select(x => new Record(x)).ToArray();

            var currentTime = DateTimeOffset.Now;

            await this.database.RequestTransactionAsync(async context =>
            {
                foreach (var item in items)
                {
                    await this.table1.AddAsync(item, context);
                }
            });


            using (var connection = this.database.Connect())
            {
                var first = this.table1.AsQueryable(connection).FirstOrDefault();

                Assert.IsNotNull(first);
                Assert.AreEqual(items[0].Id, first.Id);
                Assert.AreEqual(path[0], first.FullPath);


                Console.WriteLine("Id: {0}", first.Id);
                Console.WriteLine("Name: {0}", first.FileName);
            }
            for (int i = 0; i < path.Length; i++)
            {
                using (var connection = this.database.Connect())
                {
                    var res = this.table1
                        .AsQueryable(connection)
                        .Where(FileProperty.FullPath.ToSearch(path[i], CompareMode.Equal))
                        .ToArray();

                    Assert.AreEqual(1, res.Length);
                    var first = res[0];
                    Assert.AreEqual(items[i].Id, first.Id);
                    Assert.AreEqual(path[i], first.FullPath);

                    Console.WriteLine("Id: {0}", first.Id);
                    Console.WriteLine("Name: {0}", first.FileName);
                }
            }
        }


        private async Task<Record[]> SearchAsync(IDbConnection connection,string sql)
        {
            return (await this.table1.QueryAsync<Record>(connection,
                $"SELECT * FROM {this.table1.Name} WHERE {sql}",new { p1 = 123 }))
                .ToArray();
        }

        private void AreEqual<T>(T a, T b)
        {
            Console.WriteLine((a?.ToString() ?? "null") + ", " + (b?.ToString() ?? "null"));
            Assert.AreEqual(a, b);
        }
        private void AreNotEqual<T>(T a, T b)
        {
            Console.WriteLine((a?.ToString() ?? "null") + ", " + (b?.ToString() ?? "null"));
            Assert.AreNotEqual(a, b);
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
        }

        private string GetId() => Guid.NewGuid().ToString();

        public async Task Test1(TimeSpan offset)
        {
            //if (offset != DateTimeOffset.Now.Offset)
            {
                DatabaseFunction.SetDateOffset(offset);
            }
            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                await this.database.InitializeAsync(connection);
            }
            using (var connection = this.database.Connect())
            {
                Debug.WriteLine("a");
                var date = DateTimeOffset.Now;
                await this.TestUnixDate(date, connection, offset);

                Debug.WriteLine("b");
                var d2 = new DateTimeOffset(date.Year, date.Month, date.Day,
                    date.Hour, date.Minute, date.Second, TimeSpan.FromHours(-10.5));
                await this.TestUnixDate(d2, connection, offset);

                Debug.WriteLine("c");
                var d3 = new DateTimeOffset(date.Year, date.Month, date.Day,
                    date.Hour, date.Minute, date.Second, TimeSpan.FromHours(0));
                await this.TestUnixDate(d3, connection, offset);

            }
        }

        private async Task TestUnixDate(DateTimeOffset date, IDbConnection connection, TimeSpan offset)
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

                if (DateTimeOffset.Now.Offset == offset)
                {
                    this.AreEqual(DatabaseReference.DateOffsetReference(date).ToString(), dateNum.FirstOrDefault().ToString());
                }
                else
                {
                    this.AreEqual(DatabaseReference.DateOffsetReference(date, offset).ToString(), dateNum.FirstOrDefault().ToString());
                }
            }
            {
                var unixTime = $"strftime('%s','{date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}')";
                var unixDate = DatabaseFunction.GetDate(unixTime);

                var dateNum = await table1.QueryAsync<long>
                    (connection, $"Select {unixDate}");

                if (DateTimeOffset.Now.Offset == offset)
                {
                    this.AreEqual(DatabaseReference.DateOffsetReference(date.ToUniversalTime()).ToString(), dateNum.FirstOrDefault().ToString());
                }
                else
                {
                    this.AreEqual(DatabaseReference.DateOffsetReference(date.ToUniversalTime(), offset).ToString(), dateNum.FirstOrDefault().ToString());
                }
            }
            {
                var container = new Table1 { DateOffset = date };

                using (var tr = connection.BeginTransaction())
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


        public async Task Test2(TimeSpan offset)
        {
            //if (offset != DateTimeOffset.Now.Offset)
            {
                DatabaseFunction.SetDateOffset(offset);
            }
            using (var connection = this.database.Connect())
            {
                this.table1.Drop(connection);

                await this.database.InitializeAsync(connection);
            }

            //同じ時間が復元されるか
            //異なるオフセット
            //localタイムゾーンで取り出して同じ時間を指しているか
            //1970年より前
            using (var connection = this.database.Connect())
            {
                var record = new Table1
                {
                    Id = Guid.NewGuid().ToString(),
                    DateOffset = new DateTimeOffset(1800, 1, 2, 3, 4, 5, TimeSpan.FromHours(-3.5)),
                    Date = new DateTime(1800, 6, 7, 8, 9, 10, DateTimeKind.Local),
                    DateOffsetNull = new DateTimeOffset(1800, 1, 2, 3, 4, 5, TimeSpan.FromHours(-13)),
                    DateNull = new DateTime(1800, 6, 7, 8, 9, 10, DateTimeKind.Local),
                };

                using (var tr = connection.BeginTransaction())
                {
                    await table1.ReplaceAsync(record, connection, tr);
                    tr.Commit();
                }

                var r2 = await table1.GetRecordFromKeyAsync(connection, record.Id);

                this.AreEqual(record.DateOffset.ToLocalTime().ToString(), r2.DateOffset.ToString());
                this.AreEqual(record.Date.ToLocalTime().ToString(), r2.Date.ToString());
                this.AreEqual(record.DateOffsetNull?.ToLocalTime().ToString(), r2.DateOffsetNull?.ToString());
                this.AreEqual(record.DateNull?.ToLocalTime().ToString(), r2.DateNull.ToString());

            }

            //3000年
            using (var connection = this.database.Connect())
            {
                var record = new Table1
                {
                    Id = Guid.NewGuid().ToString(),
                    DateOffset = new DateTimeOffset(3900, 1, 2, 3, 4, 5, TimeSpan.FromHours(3.5)),
                    Date = new DateTime(3900, 6, 7, 8, 9, 10, DateTimeKind.Local),
                    DateOffsetNull = new DateTimeOffset(3900, 11, 12, 13, 14, 15, TimeSpan.FromHours(14)),
                    DateNull = new DateTime(3900, 6, 17, 18, 19, 20, DateTimeKind.Local),
                };

                using (var tr = connection.BeginTransaction())
                {
                    await table1.ReplaceAsync(record, connection, tr);
                    tr.Commit();
                }

                var r2 = await table1.GetRecordFromKeyAsync(connection, record.Id);

                this.AreEqual(record.DateOffset.ToLocalTime().ToString(), r2.DateOffset.ToString());
                this.AreEqual(record.Date.ToLocalTime().ToString(), r2.Date.ToString());
                this.AreEqual(record.DateOffsetNull?.ToLocalTime().ToString(), r2.DateOffsetNull?.ToString());
                this.AreEqual(record.DateNull?.ToLocalTime().ToString(), r2.DateNull.ToString());

            }

            //null
            //recordで入れてrecordで取り出す
            //recordで入れてlong?で取り出す
            using (var connection = this.database.Connect())
            {
                var record = new Table1
                {
                    Id = Guid.NewGuid().ToString(),
                    DateOffsetNull = null,
                    DateNull = null,
                };

                using (var tr = connection.BeginTransaction())
                {
                    await table1.ReplaceAsync(record, connection, tr);
                    tr.Commit();
                }

                var r2 = await table1.GetRecordFromKeyAsync(connection, record.Id);

                this.AreEqual(record.DateOffset.ToLocalTime().ToString(), r2.DateOffset.ToString());
                this.AreEqual(record.Date.ToLocalTime().ToString(), r2.Date.ToString());
                Assert.IsNull(r2.DateOffsetNull);
                Assert.IsNull(r2.DateNull);

                this.AreEqual(0L, (await table1.QueryAsync<long>
                    (connection, $"Select DateOffset FROM {table1.Name} WHERE Id=='{record.Id}'")).First());
                this.AreEqual(0L, (await table1.QueryAsync<long>
                    (connection, $"Select Date FROM {table1.Name} WHERE Id=='{record.Id}'")).First());
                Assert.IsNull((await table1.QueryAsync<long?>
                    (connection, $"Select DateOffsetNull FROM {table1.Name} WHERE Id=='{record.Id}'")).First());
                Assert.IsNull((await table1.QueryAsync<long?>
                    (connection, $"Select DateNull FROM {table1.Name} WHERE Id=='{record.Id}'")).First());


            }


            //現在地の日付で検索
            //前日23:59 当日0:00 3:00 12:00 20:00 23:59 翌日0:00
            using (var connection = this.database.Connect())
            {
                //var offset = DateTimeOffset.Now.Offset;
                var records = new[] {
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 4, 30, 23, 59, 59, offset) },
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 5, 1, 0, 0, 0, offset) },
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 5, 1, 3, 30, 10, offset) },
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 5, 1, 12, 55, 30, offset) },
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 5, 1, 20, 44, 2, offset) },
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 5, 1, 23, 59, 59, offset) },
                    new Table1 { DateOffset =  new DateTimeOffset(2200, 5, 2, 0, 0, 0, offset) },
                }
                .Select(x =>
                {
                    x.Id = Guid.NewGuid().ToString();
                    return x;
                })
                .ToArray();

                using (var tr = connection.BeginTransaction())
                {
                    await table1.ReplaceRangeAsync(records, connection, tr);
                    tr.Commit();
                }

                var reference = (offset == DateTimeOffset.Now.Offset)
                    ? DatabaseReference.DateOffsetReference(new DateTimeOffset(2200, 5, 1, 1, 22, 3, offset))
                    : DatabaseReference.DateOffsetReference(new DateTimeOffset(2200, 5, 1, 1, 22, 3, offset), offset);

                var r2 = await table1.AsQueryable(connection)
                    .Where(DatabaseExpression.AreEqual(DatabaseFunction.GetDate("DateOffset"),reference))
                    .OrderBy("DateOffset")
                    .ToArrayAsync();

                this.AreEqual(records.Length - 2, r2.Length);

                foreach (var c in records.Skip(1).Zip(r2, (a, b) => new { a, b }))
                {

                    this.AreEqual(c.a.Id, c.b.Id);
                    this.AreEqual(c.a.DateOffset.ToString(), c.b.DateOffset.ToOffset(offset).ToString());
                }


            }

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
            public DateTimeOffset DateOffset { get; set; } = UnixTime.DefaultDateTimeOffsetLocal;
            [RecordMember]
            public DateTime Date { get; set; } = UnixTime.DefaultDateTimeUtc;
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

