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

        static Func<Pixiv2, string> CreateLoopHostsGet(Func<Pixiv2, string> func, string[] hosts)
        {
            IEnumerator<string> CreateIeor()
            {
                while (true)
                {
                    foreach (var item in hosts)
                    {
                        yield return item;
                    }
                }
            }


            var ir = CreateIeor();

            return (p) =>
            {
                ir.MoveNext();

                var s = ir.Current;

                return s.TrimEnd('/') + "/" + func(p).TrimStart('/');

            };
        }


        public string BigUri { get; set; }

        public (string tag, string uri)[] Src { get; set; }

        public async Task OnGetAsync(int id)
        {

            var bighosts = new string[] {
                    "https://round-field-df70.leikaifeng.workers.dev/",
                    "https://morning-bird-d5a7.sparkling-night-bc75.workers.dev/"
                    };


            var big = CreateLoopHostsGet(PixCaling.AsUriFromDateTimeId, bighosts);

            using var db = Info.DbCreateFunc();

            var item = await db.GetTable<Pixiv2>().FirstAsync(p => p.Id == id);


            BigUri = big(item);


            var tags = await db.GetTable<PixivTagHas>().Where(p => p.ItemId == id).Select(p => p.TagId)
                .InnerJoin(db.GetTable<PixivTag>(), (left, right) => left == right.Id, (left, right) => right.Tag)
                .ToArrayAsync();

            Func<string, string> func = (s) =>
            {
                var tag = Uri.EscapeDataString(s);

                return $"/pix/index?select={tag}&tag={tag}&down=0";
            };

            Src = tags.Select(p => (p, func(p))).ToArray();


        }
    }
}