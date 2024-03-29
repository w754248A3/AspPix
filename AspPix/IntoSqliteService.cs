﻿using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;

namespace AspPix
{
    public class IntoSqliteService
    {

        readonly AppDataConnection _db;

        readonly ILogger _logger;

        public IntoSqliteService(AppDataConnection db, ILogger<IntoSqliteService> logger)
        {
            _db = db;
            _logger = logger;
        }

        static TempTable<T> CreateTemp<T>(DataConnection db, string name) where T: class
        {

            db.DropTable<T>(tableName:name, tableOptions: TableOptions.DropIfExists);

            return db.CreateTempTable<T>(tableName: name);
        }

        static void Into<T>(string name, DataConnection db, IEnumerable<T> query, Action<TempTable<T>> action) where T : class
        {
            using var temp = CreateTemp<T>(db, name);


            temp.BulkCopy(query);


            action(temp);

        }

        static void InsertData(List<PixivHtml> vs, DataConnection db)
        {

            IEnumerable<PixivData> Select重复()
            {
                var set = new HashSet<int>();

                foreach (var item in vs)
                {
                    if (!set.Contains(item.Id))
                    {
                        set.Add(item.Id);

                        yield return item;
                    }
                }

                
            }

            Into("A004C88C-0518-4A21-B8CC-6316C8523439", db,
                Select重复(),
                tempTable =>
                {
                    var table = db.GetTable<PixivData>();

                    var query = tempTable
                        .LeftJoin(table, (a, b) => a.Id == b.Id, (a, b) => new { a, b });

                    Into("1727135B-0ED3-447B-A343-7538AC347DD9", db,
                        query.Where(p => p.b == null).Select(p => p.a),
                        p => p.Insert(table, p => p));

                    Into("C81D978B-5926-411A-BE04-F4D2470DBEAF", db,
                        query.Where(p => p.b != null).Select(p => p.a),
                        p =>
                        {
                            p.InnerJoin(table, (a, b) => a.Id == b.Id, (a, b) => a)
                            .Update(table, p => p);
                        });
                });     
        }
      
        static void InsertTag(List<PixivHtml> vs, DataConnection db)
        {
            var dic = new Dictionary<int, string>();

            var ie = vs.SelectMany(p =>
            {
                return p.Tags.Select(tag =>
                {
                    var tagID = StaticFunction.GetTagHash(tag);
                    if (dic.TryGetValue(tagID, out var tag_2))
                    {


                        dic[tagID] = $"{tag} + {tag_2}";

                    }
                    else
                    {
                        dic[tagID] = tag;
                    }

                    return (ItemId: p.Id, TagId: tagID);

                });
            }).ToHashSet();


            void InMap()
            {
                Into("18A7579E-A10A-4BC2-A698-F61BAEA36F7F", db,
                    ie.Select((p) => new PixivTagMap { ItemId = p.ItemId, TagId= p.TagId }),
                    tempTable =>
                    {
                        var table = db.GetTable<PixivTagMap>();

                        var query = tempTable.LeftJoin(
                            table,
                            (a, b) => a.ItemId == b.ItemId && a.TagId == b.TagId,
                            (a, b) => new { a, b })
                            .Where(p => p.b == null)
                            .Select(p => p.a);



                        Into("C270FF02-14E9-430D-ADDD-F52A6E44828D", db,
                            query,
                            p => p.Insert(table, p => p));


                    });


            }

            void InTag()
            {
                Into("FB5D3796-6420-418D-AF5A-E77972D4F16D", db,
                    dic.Select(p => new PixivTag { Id = p.Key, Tag = p.Value }),
                    tempTable =>
                    {

                        var table = db.GetTable<PixivTag>();

                        var query =tempTable.LeftJoin(
                            table,
                            (a, b) => a.Id == b.Id,
                            (a, b) => new { a, b })
                            .Where(p => p.b == null)
                            .Select(p => p.a);



                        Into("9585DDCB-86F0-4772-A2F4-8534E670DD9B", db,
                            query,
                            p => p.Insert(table, p => p));


                    });


            }

            InMap();

            InTag();

        }

        static void Insert(List<PixivHtml> vs, DataConnection db)
        {
            using var ts = db.BeginTransaction();

            InsertData(vs, db);

            InsertTag(vs, db);

            ts.Commit();
        }

        public void Run(int count, ChannelReader<PixivHtml> reader_data, ChannelReader<PixImg> reader_img)
        {
            var data_vs = new List<PixivHtml>();

            bool InsertData()
            {
                if (reader_data.TryRead(out var v))
                {
                    data_vs.Add(v);

                    if (data_vs.Count >= count)
                    {
                        Insert(data_vs, _db);

                        data_vs = new List<PixivHtml>();
                    }

                    return true;
                }
                else
                {
                    return false;
                }

            }

            var img_vs = new List<PixImg>();

            bool InsertImg()
            {
                if (reader_img.TryRead(out var pixImg))
                {

                    img_vs.Add(pixImg);

                    if (img_vs.Count >= 10)
                    {


                        using var tc = _db.BeginTransaction();

                        Array.ForEach(img_vs.ToArray(), (p) => _db.InsertOrReplace(p));

                        tc.Commit();

                        img_vs = new List<PixImg>();
                        
                    }

                    return true;
                }
                else
                {
                    return false;
                }
                
            }

            LogExit.OnErrorExit(nameof(IntoSqliteService), _logger, () =>
            {
                
                while (true)
                {
                    if(InsertImg() ==false && InsertData() == false)
                    {
                        Thread.Sleep(new TimeSpan(0, 0, 5));
                    }
                  
                }


            });
        }
    }
}