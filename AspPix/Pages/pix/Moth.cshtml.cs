using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LinqToDB;
using LinqToDB.Async;
using Pixiv2 = AspPix.Fs.PixSql.Pixiv2;
using PixivTag = AspPix.Fs.PixSql.PixivTag;
using PixivTagHas = AspPix.Fs.PixSql.PixivTagHas;

namespace AspPix.Pages
{
    public class MothModel : PageModel
    {
        public IEnumerable<(string small, string big)> Scrs { get; set; }

        public DateTime Up { get; set; }

        public DateTime Down { get; set; }

        public int Span { get; set; }

        public string As(DateTime dateTime)
        {
            static string A(int n)
            {
                if (n < 10)
                {
                    return $"0{n}";
                }
                else
                {
                    return n.ToString();
                }
            }

            return $"{dateTime.Year}-{A(dateTime.Month)}-{A(dateTime.Day)}";
        }

        public async Task OnGetAsync(DateTime? date, int span)
        {
            DateTime left, right;
            DateTime dateTime;

            Span = span;

            if (date is null)
            {
                dateTime = DateTime.Now;    
            }
            else
            {
                dateTime = date.Value;
            }


            Up = dateTime.AddDays(-span);

            Down = dateTime.AddDays(span);


            left = Up;

            right = dateTime;


            using var db = Info.DbCreateFunc();


           var items = await  db.GetTable<Pixiv2>()
                .Where(p => p.Date > left && p.Date <= right)
                .OrderByDescending(p => p.Mark)
                .Take(150)
                .ToListAsync();




            Scrs = items.Select(item => (IndexModel.CreateQueryString(item), "/pix/viewimg?id=" + item.Id));

        }
    }
}
