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
    public class ImgController : ControllerBase
    {
        private readonly PixImgGetHttp _http;

        IConfiguration _con;

        public ImgController(PixImgGetHttp http, IConfiguration configuration)
        {
            _http = http;

            _con = configuration;
        }

        static Uri[] CreatePath(Uri host, string path, string path2)
        {
            return new Uri[]
            {
                new Uri(host, Fs.PixFunc.base64Decode(path)),

                new Uri(host, Fs.PixFunc.base64Decode(path2)),
            };
        }

        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] string path, [FromQuery] string path2, [FromQuery] int id)
        {
            //"https://i.pximg.net/c/540x540_70/img-master/img/2020/04/24/22/48/16/81033008_p0_master1200.jpg"


            var info = _con.GetAspPixInfo();


            using var db = Info.CreateDbConnect(info.DATA_BASE_CONNECT_STRING);

            var img = await db.GetTable<Fs.PixSql.PixImg>().Where(p => p.Id == id).FirstOrDefaultAsync();

            if (img is not null)
            {
                return new FileContentResult(img.Img, MediaTypeNames.Image.Jpeg);
            }

            foreach (var item in CreatePath(info.CLOUDFLARE_HOST, path, path2))
            {
                var res = await _http.Http.GetAsync(item, HttpCompletionOption.ResponseHeadersRead);

                if (res.IsSuccessStatusCode)
                {
                    var by = await res.Content.ReadAsByteArrayAsync();

                    db.InsertOrReplace(new Fs.PixSql.PixImg(id, by));

                    return new FileContentResult(by, MediaTypeNames.Image.Jpeg);
                }

               
            }


            return NotFound();
        }
    }
}
