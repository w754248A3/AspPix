using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using PixivData = AspPix.Fs.PixSql.PixivData;
using PixivTag = AspPix.Fs.PixSql.PixivTag;
using PixivTagMap = AspPix.Fs.PixSql.PixivTagMap;

namespace AspPix.Pages
{


    public class IndexModel : PageModel
    {

        IConfiguration _con;

        public IndexModel(IConfiguration configuration)
        {
            _con = configuration;
        }

        public IEnumerable<(string small, string big)> Scrs { get; set; }

        public uint Down { get; set; }

        public IEnumerable<string> Tags { get; set; }

        public string Tag { get; set; }
        
        public string Date { get; set; }

        public string Date2 { get; set; }

        public string OnlyLive { get; set; }

        
        public static string CreateQueryString(PixivData p)
        {
            //QueryHelpers.AddQueryString

            return $"/pix/api/img?id={p.Id}&path={Fs.PixFunc.base64Encode(Fs.PixParse.getImgUriSmall(p.Date, p.Id, false))}&path2={Fs.PixFunc.base64Encode(Fs.PixParse.getImgUriSmall(p.Date, p.Id, true))}";
        }

        static IQueryable<PixivData> CreateQuery(LinqToDB.Data.DataConnection db, IQueryable<PixivData> pix, int tagid)
        {
            var has = db.GetTable<PixivTagMap>().Where(p => p.TagId == tagid).Select(p => p.ItemId);

            return pix.InnerJoin(has, (left, right) => left.Id == right, (left, right) => left);
        }

        public async Task OnGetAsync(string tag, uint down, string date, string date2, string onlylive)
        {

            Tags = Array.Empty<string>();

            Down = down;
           
            Date = date ?? "";

            Date2 = date2 ?? "";

            Tag = tag ?? "";

            using var db = Info.DbCreateFunc();

            IQueryable<PixivData> query;
            if (onlylive == "on")
            {

                query = db.GetTable<PixivData>()
                    .InnerJoin(db.GetTable<Fs.PixSql.PixLive>(), (a, b) => a.Id == b.Id, (a, b) => a);

            }
            else
            {
                query = db.GetTable<PixivData>();
            }
             

            if (DateTime.TryParse(date, out var d))
            {          
                query = query.Where(p => p.Date >= d);
            }

            if (DateTime.TryParse(date2, out var d2)) 
            {
                query = query.Where(p => p.Date <= d2);
            }


            if (!string.IsNullOrWhiteSpace(Tag))
            {
                var id = Fs.PixSql.getTagHash(tag);

                query = CreateQuery(db, query, id);
            }

            var info = _con.GetSection(AspPixInfo.Key_Name).Get<AspPixInfo>();

            var items = await query
                .OrderByDescending(item => item.Mark)
                .Skip((int)(Down * info.TAKE_SMALL_IMAGE))
                .Take(info.TAKE_SMALL_IMAGE)
                .ToArrayAsync();

            OnlyLive = onlylive;

            Scrs = items.Select(item => (CreateQueryString(item), "/pix/viewimg?id=" + item.Id));
        }
    }
}