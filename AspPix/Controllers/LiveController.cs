using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using LinqToDB;
using Microsoft.Extensions.Configuration;

namespace AspPix.Controllers
{
   
    [Route("/pix/api/[controller]")]
    [ApiController]
    public class LiveController : ControllerBase
    {

        private readonly PixImgGetHttp _http;

        IConfiguration _con;

        public LiveController(PixImgGetHttp http, IConfiguration configuration)
        {
            _http = http;

            _con = configuration;
        }

        static Uri CreateBigUri(Uri host, string s)
        {

            return new Uri(host, s);

        }


        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int id)
        {
            //"https://i.pximg.net/c/540x540_70/img-master/img/2020/04/24/22/48/16/81033008_p0_master1200.jpg"

           
            using var db = Info.DbCreateFunc();

            var item = await db.GetTable<AspPix.Fs.PixSql.PixivData>().Where(p => p.Id == id).FirstAsync();


            var info = _con.GetSection(AspPixInfo.Key_Name).Get<AspPixInfo>();



            var bigUri = CreateBigUri(info.CLOUDFLARE_HOST, Fs.PixParse.getImgUri(item.Date, item.Id, item.Flags, 1).First());

            var res = await _http.Http.GetAsync(bigUri, HttpCompletionOption.ResponseHeadersRead);

            if (res.IsSuccessStatusCode)
            {
                var by = await res.Content.ReadAsByteArrayAsync();


                db.InsertOrReplace(new Fs.PixSql.PixLive(item.Id, by));

                return new FileContentResult(by, MediaTypeNames.Image.Jpeg);
            }
            else
            {
                return NotFound();
            }

           
        }
    }
}
