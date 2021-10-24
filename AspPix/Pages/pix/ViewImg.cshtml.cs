using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LinqToDB;
using LinqToDB.Mapping;
using Pixiv2 = AspPix.PixCaling.Pixiv2;
using PixivTag = AspPix.PixCaling.PixivTag;
using PixivTagHas = AspPix.PixCaling.PixivTagHas;

namespace AspPix.Pages
{
    public class ViewImgModel : PageModel
    {
        public string BigUri { get; set; }

        public IEnumerable<(string tag, string uri)> Src { get; set; }

        public string Mesagge { get; set; }

        static string CreateBigUri(string host, string s)
        {
            return host.TrimEnd('/') + "/" + s.TrimStart('/');
        }

        public async Task OnGetAsync(int id)
        {
            const string HOST = "https://morning-bird-d5a7.sparkling-night-bc75.workers.dev/";

            using var db = Info.DbCreateFunc();

            var item = await db.GetTable<Pixiv2>().FirstAsync(p => p.Id == id);

            BigUri = CreateBigUri(HOST, PixCaling.AsUriFromDateTimeId(item));

            var tags = await db.GetTable<PixivTagHas>().Where(p => p.ItemId == id).Select(p => p.TagId)
                .InnerJoin(db.GetTable<PixivTag>(), (left, right) => left == right.Id, (left, right) => right.Tag)
                .ToArrayAsync();

            static string func(string s)
            {
                var tag = Uri.EscapeDataString(s);

                return $"/pix/index?select=&tag={tag}&down=0";
            }

            Mesagge = $"{item.Id} {item.Mark} {item.Date}";

            Src = tags.Select(p => (p, func(p)));
        }
    }
}