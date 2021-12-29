using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LinqToDB;
using LinqToDB.Async;
using PixivData = AspPix.Fs.PixSql.PixivData;
using PixivTag = AspPix.Fs.PixSql.PixivTag;
using PixivTagMap = AspPix.Fs.PixSql.PixivTagMap;

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


           var items = await  db.GetTable<PixivData>()
                .Where(p => p.Date > left && p.Date <= right)
                .OrderByDescending(p => p.Mark)
                .Take(150)
                .ToListAsync();




            Scrs = items.Select(item => (IndexModel.CreateQueryString(item), "/pix/viewimg?id=" + item.Id));

        }
    }
}
