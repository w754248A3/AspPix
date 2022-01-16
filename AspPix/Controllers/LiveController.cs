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

namespace AspPix.Controllers
{
   
    [Route("/pix/api/[controller]")]
    [ApiController]
    public class LiveController : ControllerBase
    {

        private readonly PixImgGetHttp _http;

        public LiveController(PixImgGetHttp http)
        {
            _http = http;
        }

        static string CreateBigUri(string host, string s)
        {
            return host.TrimEnd('/') + "/" + s.TrimStart('/');
        }


        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int id)
        {
            //"https://i.pximg.net/c/540x540_70/img-master/img/2020/04/24/22/48/16/81033008_p0_master1200.jpg"

            const string HOST = "https://morning-bird-d5a7.sparkling-night-bc75.workers.dev/";


            using var db = Info.DbCreateFunc();

            var item = db.GetTable<AspPix.Fs.PixSql.PixivData>().Where(p => p.Id == id).First();


            var bigUri = CreateBigUri(HOST, Fs.PixParse.getImgUri(item.Date, item.Id, item.Flags));
            
            var by = await Info.GetImg(_http.Http, bigUri, null);


            db.InsertOrReplace(new Fs.PixSql.PixLive(item.Id, by));

            return new FileContentResult(by, MediaTypeNames.Image.Jpeg);
        }
    }
}
