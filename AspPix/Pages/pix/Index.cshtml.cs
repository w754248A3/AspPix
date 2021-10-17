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
        
        static IQueryable<Pixiv2> CreateExQuery(LinqToDB.Data.DataConnection db, IQueryable<Pixiv2> query, int tagid)
        {
            return db.GetTable<PixivTagHas>().Where(p => p.TagId == tagid)
              .RightJoin(query, (left, right) => left.ItemId == right.Id, (left, right) => new { left, right })
              .Where(p => p.left == null)
              .Select(p => p.right);
        }

        static IQueryable<Pixiv2> CreateQuery(LinqToDB.Data.DataConnection db, IQueryable<Pixiv2> query, int tagid)
        {
            return db.GetTable<PixivTagHas>().Where(p => p.TagId == tagid).Select(p => p.ItemId)
              .InnerJoin(query, (left, right) => left == right.Id, (left, right) => right);
        }

        public async Task OnGetAsync(string select, string tag, uint down)
        {
            Tags = Info.Tags;

            Down = down;

            string t;
            if (string.IsNullOrWhiteSpace(tag))
            {
                if (string.IsNullOrWhiteSpace(select))
                {
                    Select = "";

                    Tag = "";

                    t = "";
                }
                else
                {
                    Select = select;

                    Tag = "";

                    t = Select;
                }
            }
            else
            {
                Select = tag;

                Tag = tag;


                t = tag;
            }

         

            using var db = Info.DbCreateFunc();


            IQueryable<Pixiv2> query = db.GetTable<Pixiv2>();

            if (string.IsNullOrWhiteSpace(t))
            {
               
            }
            else
            {


                var tagid = db.GetTable<PixivTag>().Where(p => p.Tag == t).FirstOrDefault();

                if (tagid is null)
                {
                    throw new ArgumentException();
                }
                else
                {
                    query = CreateQuery(db, query, tagid.Id);
                     
                }       
            }


            var items = await query
                .OrderByDescending(item => item.Mark)
                .Skip((int)(Down * ConstValue.TAKE_SMALL_IMAGE))
                .Take(ConstValue.TAKE_SMALL_IMAGE)
                .ToArrayAsync();


            Func<Pixiv2, string> func = (p) => $"/pix/api/img?id={p.Id}&path={Info.Base64Encode(PixCaling.AsUriFromDateTimeIdSmall(p, false))}&path2={Info.Base64Encode(PixCaling.AsUriFromDateTimeIdSmall(p, true))}";

            Scrs = items.Select(item => (func(item), "/pix/viewimg?id=" + item.Id));
        }

    }
}
