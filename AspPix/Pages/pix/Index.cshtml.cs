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
using AspPix.Controllers;
using PixivData = AspPix.Fs.PixSql.PixivData;
using PixivTag = AspPix.Fs.PixSql.PixivTag;
using PixivTagMap = AspPix.Fs.PixSql.PixivTagMap;

namespace AspPix.Pages
{


    

    public class IndexModel : PageModel
    {
        public const string PATH = "Index";

        public record PixImgUri(string Small, string Big);

        readonly IConfiguration _con;

        public IndexModel(IConfiguration con)
        {
            _con = con;
        }

        public IEnumerable<PixImgUri> ImgUris { get; set; }

        [FromQuery]
        public uint Pages { get; set; }

        [FromQuery]
        public string Tag { get; set; }

        [FromQuery]
        public string DateLeft { get; set; }

        [FromQuery]
        public string DateRight { get; set; }

        [FromQuery]
        public string OnlyLive { get; set; }

        
        public static string CreateSmallImgQueryString(PixivData p)
        {
           
            return QueryHelpers.AddQueryString("/pix/api/img",
                new KeyValuePair<string, string>[] {
                    KeyValuePair.Create(nameof(ImgController.Id), p.Id.ToString()),
                    KeyValuePair.Create(nameof(ImgController.Path), Fs.PixFunc.base64Encode(Fs.PixParse.getImgUriSmall(p.Date, p.Id, false))),
                    KeyValuePair.Create(nameof(ImgController.Path2), Fs.PixFunc.base64Encode(Fs.PixParse.getImgUriSmall(p.Date, p.Id, true))),
                });
        }

        public static string CreateViewPageQueryString(PixivData p)
        {
            return QueryHelpers.AddQueryString("/pix/viewimg",
               new KeyValuePair<string, string>[] {
                    KeyValuePair.Create("id", p.Id.ToString()),
               });
        }

        static IQueryable<PixivData> CreateQuery(LinqToDB.Data.DataConnection db, IQueryable<PixivData> pix, int tagid)
        {
            var has = db.GetTable<PixivTagMap>().Where(p => p.TagId == tagid).Select(p => p.ItemId);

            return pix.InnerJoin(has, (left, right) => left.Id == right, (left, right) => left);
        }

        public async Task OnGetAsync()
        {
            var info = _con.GetAspPixInfo();

            using var db = Info.CreateDbConnect(info.DATA_BASE_CONNECT_STRING);

            IQueryable<PixivData> query;
            if ("on".Equals(OnlyLive, StringComparison.OrdinalIgnoreCase))
            {
                query = db.GetTable<PixivData>()
                    .InnerJoin(db.GetTable<Fs.PixSql.PixLive>(), (a, b) => a.Id == b.Id, (a, b) => a);
            }
            else
            {
                query = db.GetTable<PixivData>();
            }
             

            if (DateTime.TryParse(DateLeft, out var d))
            {          
                query = query.Where(p => p.Date >= d);
            }

            if (DateTime.TryParse(DateRight, out var d2)) 
            {
                query = query.Where(p => p.Date <= d2);
            }


            if (!string.IsNullOrWhiteSpace(Tag))
            {
                var id = Fs.PixSql.getTagHash(Tag);

                query = CreateQuery(db, query, id);
            }

            var items = await query
                .OrderByDescending(item => item.Mark)
                .Skip(checked((int)(Pages * info.TAKE_SMALL_IMAGE)))
                .Take(info.TAKE_SMALL_IMAGE)
                .ToArrayAsync();

            ImgUris = items.Select(item => new PixImgUri(CreateSmallImgQueryString(item), CreateViewPageQueryString(item))); 
        }
    }
}