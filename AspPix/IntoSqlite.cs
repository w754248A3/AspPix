using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;

namespace AspPix
{
    public class IntoSqlite
    {

        readonly AppDataConnection _db;

        readonly ILogger _logger;

        public IntoSqlite(AppDataConnection db, ILogger<IntoSqlite> logger)
        {
            _db = db;
            _logger = logger;
        }

        static TempTable<T> CreateTemp<T>(DataConnection db, string name) where T: class
        {

            db.DropTable<T>(tableName:name, tableOptions: TableOptions.DropIfExists);

            return db.CreateTempTable<T>(tableName: name);
        }

        static void Into<T>(IQueryable<T> query, DataConnection db, Action<TempTable<T>> action) where T : class
        {
            using var temp = CreateTemp<T>(db, "3828A8BD-85C0-4BFB-B41E-F9934D4013C4");


            temp.BulkCopy(query);


            action(temp);

        }

        static void InsertData(List<PixivHtml> vs, DataConnection db)
        {
          
           
            using var temp = CreateTemp<PixivData>(db, "A004C88C-0518-4A21-B8CC-6316C8523439");

            temp.BulkCopy(vs.Select(p => (PixivData)p));

            var query = temp
                .LeftJoin(db.GetTable<PixivData>(), (a, b) => a.Id == b.Id, (a, b) => new { a, b });

            Into(
                query.Where(p => p.b == null).Select(p => p.a),
                db,
                p => p.Insert(db.GetTable<PixivData>(), p => p));

            Into(
                query.Where(p => p.b != null).Select(p => p.a),
                db,
                p =>
                {
                    p.InnerJoin(db.GetTable<PixivData>(), (a, b) => a.Id == b.Id, (a, b) => a)
                    .Update(db.GetTable<PixivData>(), p => p);


                });

            

        }
      
        static void InsertTag(List<PixivHtml> vs, DataConnection db)
        {
            var dic = new Dictionary<string, int>();

            var ie = vs.SelectMany(p =>
            {
                return p.Tags.Select(tag =>
                {
                    if (!dic.TryGetValue(tag, out var tagID))
                    {
                        tagID = StaticFunction.GetTagHash(tag);

                        dic[tag] = tagID;

                    }

                    return new PixivTagMap { ItemId = p.Id, TagId = tagID };

                });
            });


            void InMap()
            {
                using var temp = CreateTemp<PixivTagMap>(db, "18A7579E-A10A-4BC2-A698-F61BAEA36F7F");


                temp.BulkCopy(ie);

                var query = temp.LeftJoin(
                    db.GetTable<PixivTagMap>(),
                    (a, b) => a.ItemId == b.ItemId && a.TagId == b.TagId,
                    (a, b) => new { a, b })
                    .Where(p => p.b == null)
                    .Select(p => p.a);



                Into(query, db, p => p.Insert(db.GetTable<PixivTagMap>(), p => p));


            }

            void InTag()
            {
                using var temp = CreateTemp<PixivTag>(db, "18A7579E-A10A-4BC2-A698-F61BAEA36F7F");


                temp.BulkCopy(dic.Select(p => new PixivTag { Id = p.Value, Tag = p.Key }));

                var query = temp.LeftJoin(
                    db.GetTable<PixivTag>(),
                    (a, b) => a.Id == b.Id,
                    (a, b) => new { a, b })
                    .Where(p => p.b == null)
                    .Select(p => p.a);



                Into(query, db, p => p.Insert(db.GetTable<PixivTag>(), p => p));


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

        public void Run(int count,ChannelReader<PixivHtml> reader)
        {
            var vs = new List<PixivHtml>();

            while (true)
            {
                if (reader.TryRead(out var v))
                {
                    vs.Add(v);


                    if (vs.Count >= count)
                    {
                        Insert(vs, _db);

                        vs = new List<PixivHtml>();
                    }

                }
                else
                {
                    Thread.Sleep(new TimeSpan(0, 0, 5));
                }
            }
        }

       

    }
}