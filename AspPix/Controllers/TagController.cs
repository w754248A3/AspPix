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
    public class TagController : ControllerBase
    {

        readonly IConfiguration _con;

        readonly AppDataConnection _db;

        public TagController(IConfiguration con, AppDataConnection db)
        {
   
            _con = con;
            _db = db;
        }


        [HttpGet]
        public async Task<string[]> Get([FromQuery] string name)
        {
            //"https://i.pximg.net/c/540x540_70/img-master/img/2020/04/24/22/48/16/81033008_p0_master1200.jpg"

            var vs = await _db.PixTag.Where(p=> p.Tag.Contains(name)).Select(p=> p.Tag).Take(30).ToArrayAsync();

            return vs;
        }
    }
}
