using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using AspPix.Controllers;

namespace AspPix.Pages
{


    

    public class IndexModel : PageModel
    {
        public const string PATH = "Index";

        public record PixImgUri(string Small, string Big);

        readonly IConfiguration _con;

        readonly AppDataConnection _connection;

        public IndexModel(IConfiguration con, AppDataConnection connection)
        {
            _con = con;
            _connection = connection;
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

            SmallImgUriArray v = new()
            {
                Id = p.Id,

                Uris = new string[]
                {
                    DataParse.GetSmallImgUri(p, false),
                    DataParse.GetSmallImgUri(p, true)
                }
            };

            var json = JsonSerializer.Serialize(v);


            return QueryHelpers.AddQueryString("/pix/api/img",
                new KeyValuePair<string, string>[] {
                    KeyValuePair.Create(nameof(ImgController.Path), StaticFunction.Base64Encode(json))
                });
        }

        public static string CreateViewPageQueryString(PixivData p)
        {
            return QueryHelpers.AddQueryString("/pix/viewimg",
               new KeyValuePair<string, string>[] {
                    KeyValuePair.Create("id", p.Id.ToString()),
               });
        }

        static IQueryable<PixivData> CreateQuery(LinqToDB.Data.DataConnection _connection, IQueryable<PixivData> pix, int tagid)
        {
            var has = _connection.GetTable<PixivTagMap>().Where(p => p.TagId == tagid).Select(p => p.ItemId);

            return pix.InnerJoin(has, (left, right) => left.Id == right, (left, right) => left);
        }

        public async Task OnGetAsync()
        {
            var info = _con.GetAspPixInfo();

            IQueryable<PixivData> query;
            if ("on".Equals(OnlyLive, StringComparison.OrdinalIgnoreCase))
            {
                query = _connection.PixData
                    .InnerJoin(_connection.PixLive, (a, b) => a.Id == b.Id, (a, b) => a);
            }
            else
            {
                query = _connection.PixData;
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
                var id = StaticFunction.GetTagHash(Tag);

                query = CreateQuery(_connection, query, id);
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