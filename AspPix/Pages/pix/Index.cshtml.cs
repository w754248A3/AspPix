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

        public IEnumerable<string> Tags { get; set; }

        public string Tag { get; set; }
        
        public string Date { get; set; }

        public string Date2 { get; set; }

        static IQueryable<Pixiv2> CreateExQuery(LinqToDB.Data.DataConnection db, IQueryable<Pixiv2> query, int tagid)
        {
            return db.GetTable<PixivTagHas>().Where(p => p.TagId == tagid)
              .RightJoin(query, (left, right) => left.ItemId == right.Id, (left, right) => new { left, right })
              .Where(p => p.left == null)
              .Select(p => p.right);
        }

        static IQueryable<Pixiv2> CreateQuery(LinqToDB.Data.DataConnection db, IQueryable<Pixiv2> pix, int tagid)
        {
            var has = db.GetTable<PixivTagHas>().Where(p => p.TagId == tagid).Select(p => p.ItemId);

            return pix.InnerJoin(has, (left, right) => left.Id == right, (left, right) => left);
        }

        public async Task OnGetAsync(string tag, uint down, string date, string date2)
        {
            Tags = Info.Tags;

            Down = down;
           
            Date = date ?? "";

            Date2 = date2 ?? "";

            Tag = tag ?? "";

            using var db = Info.DbCreateFunc();

            IQueryable<Pixiv2> query = db.GetTable<Pixiv2>();

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
                var id = PixCaling.CreateGetHashCode()(tag);

                query = CreateQuery(db, query, id);
            }

            var items = await query
                .OrderByDescending(item => item.Mark)
                .Skip((int)(Down * ConstValue.TAKE_SMALL_IMAGE))
                .Take(ConstValue.TAKE_SMALL_IMAGE)
                .ToArrayAsync();

            static string CreateQueryString(Pixiv2 p)
            {
                return $"/pix/api/img?id={p.Id}&path={Info.Base64Encode(PixCaling.AsUriFromDateTimeIdSmall(p, false))}&path2={Info.Base64Encode(PixCaling.AsUriFromDateTimeIdSmall(p, true))}";
            }

            Scrs = items.Select(item => (CreateQueryString(item), "/pix/viewimg?id=" + item.Id));
        }
    }
}