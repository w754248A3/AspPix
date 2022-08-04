using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.Extensions.Configuration;

namespace AspPix.Pages
{
    public class ViewImgModel : PageModel
    {
        public const string PATH = "ViewImg";



        readonly IConfiguration _con;

        readonly AppDataConnection _db;

        public ViewImgModel(IConfiguration con, AppDataConnection db)
        {
            _con = con;
            _db = db;
        }

        public string BigUri { get; set; }

        public IEnumerable<(string tag, string uri)> Src { get; set; }

        public string Mesagge { get; set; }

        public string SourceUri { get; set; }

        public string LiveUri { get; set; }

        public string IsLive { get; set; }



        static string CreateBigUri(string host, string s)
        {
            return host.TrimEnd('/') + "/" + s.TrimStart('/');
        }

        public async Task OnGetAsync(int id)
        {
            const string HOST = "https://morning-bird-d5a7.sparkling-night-bc75.workers.dev/";

            var info = _con.GetAspPixInfo();

            var item = await _db.PixData.FirstAsync(p => p.Id == id);
            BigUri = JsonSerializer.Serialize(
             DataParse.GetImgUri(item, 50)
                .Select(p => CreateBigUri(HOST, p))
                .ToArray());

            var tags = await _db.PixTagMap.Where(p => p.ItemId == id).Select(p => p.TagId)
                .InnerJoin(_db.PixTag, (left, right) => left == right.Id, (left, right) => right.Tag)
                .ToArrayAsync();

            static string func(string s)
            {
                var tag = Uri.EscapeDataString(s);

                return $"/pix/index?select=&tag={tag}&down=0";
            }

            Mesagge = $"{item.Id} {item.Mark} {item.Date}";

            SourceUri = $"https://www.pixiv.net/artworks/{item.Id}";

            Src = tags.Select(p => (p, func(p)));

            var c  = await _db.GetTable<PixLive>().Where(p => p.Id == id).CountAsync();

            IsLive = c > 0 ? "ϲ��" : "��ϲ��";

            LiveUri = "/pix/api/live?id=" + id;

        }
    }
}