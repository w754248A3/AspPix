using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace AspPix.TagHelpers
{
    public class MyImgTagHelper : TagHelper
    {
        public string ImgUri { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            
           
           

            output.TagName = "img";

            output.Attributes.SetAttribute("height", 540);

            output.Attributes.SetAttribute("src", ImgUri);


        }
    }
}
