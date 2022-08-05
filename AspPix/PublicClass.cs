using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspPix
{
    public record SmallImgUriArray
    {
        public int Id { get; set; }

        public string[] Uris { get; set; }
    }
}
