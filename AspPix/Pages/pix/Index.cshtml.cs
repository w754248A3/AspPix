using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pixiv2 = AspPix.PixCaling.Pixiv2;
using PixivTag = AspPix.PixCaling.PixivTag;
using PixivTagHas = AspPix.PixCaling.PixivTagHas;

namespace AspPix.Pages
{


    public class IndexModel : PageModel
    {
        public IEnumerable<(string small, string big)> Scrs { get; set; }

        public uint Down { get; set; }

        public string Select { get; set; }

        public IEnumerable<string> Tags { get; set; }

        public string Tag { get; set; }
        
        static IQueryable<Pixiv2> CreateQuery(LinqToDB.Data.DataConnection db, int tagid)
        {

            return db.GetTable<PixivTagHas>().Where(p => p.TagId == tagid).Select(p => p.ItemId)
              .InnerJoin(db.GetTable<Pixiv2>(), (left, right) => left == right.Id, (left, right) => right);
        }

        public async Task OnGetAsync(string select, string tag, uint? down)
        {
            Tags = Info.Tags;

            Down = down ?? 0;

            Select = select ?? "";

            Tag = tag ?? "";


            if (Select != Tag) 
            {
                Tag = Select;

                Down = 0;
            }
            
            using var db = Info.DbCreateFunc();


            IQueryable<Pixiv2> query;

            if (string.IsNullOrWhiteSpace(Select))
            {
                query = db.GetTable<Pixiv2>()
                    .OrderByDescending(item => item.Mark);
            }
            else
            {


                var tagid = db.GetTable<PixivTag>().Where(p => p.Tag == Select).FirstOrDefault();

                if (tagid is null)
                {
                    throw new ArgumentException();
                }
                else
                {
                    if (!Tags.Any(p => p == Select))
                    {
                        Tags = Tags.Append(Select);
                    }

                    query = CreateQuery(db, tagid.Id)
                        .OrderByDescending(item => item.Mark);
                }       
            }
           


            var items = await query
                .Skip((int)(Down * ConstValue.TAKE_SMALL_IMAGE))
                .Take(ConstValue.TAKE_SMALL_IMAGE)
                .ToArrayAsync();


            Func<Pixiv2, string> func = (p) => $"/pix/api/img?id={p.Id}&path={Info.Base64Encode(PixCaling.AsUriFromDateTimeIdSmall(p, false))}&path2={Info.Base64Encode(PixCaling.AsUriFromDateTimeIdSmall(p, true))}";

            Scrs = items.Select(item => (func(item), "/pix/viewimg?id=" + item.Id));
        }

    }
}
