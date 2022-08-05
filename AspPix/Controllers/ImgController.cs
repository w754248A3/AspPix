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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspPix.Controllers
{



    [Route("/pix/api/[controller]")]
    [ApiController]
    public class ImgController : ControllerBase
    {
        readonly PixImgGetHttp _http;

        readonly IConfiguration _con;

        readonly AppDataConnection _db;

        public ImgController(PixImgGetHttp http, IConfiguration con, AppDataConnection db)
        {
            _http = http;
            _con = con;
            _db = db;
        }


        [FromQuery]
        public string Path { get; set; }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            //"https://i.pximg.net/c/540x540_70/img-master/img/2020/04/24/22/48/16/81033008_p0_master1200.jpg"


            var args = JsonSerializer.Deserialize<SmallImgUriArray>(
                StaticFunction.Base64Decode(Path));

            var info = _con.GetAspPixInfo();

            var img = await _db.GetTable<PixImg>().Where(p => p.Id == args.Id).FirstOrDefaultAsync();

            if (img is not null)
            {
                return new FileContentResult(img.Img, MediaTypeNames.Image.Jpeg);
            }

            foreach (var item in args.Uris.Select(p=> new Uri(info.CLOUDFLARE_HOST, p)))
            {
                using var res = await _http.Http.GetAsync(item, HttpCompletionOption.ResponseHeadersRead);

                if (res.IsSuccessStatusCode)
                {
                    var by = await res.Content.ReadAsByteArrayAsync();

                    _db.InsertOrReplace(new PixImg { Id = args.Id, Img = by });

                    return new FileContentResult(by, MediaTypeNames.Image.Jpeg);
                }
              
            }


            return NotFound();
        }
    }
}
