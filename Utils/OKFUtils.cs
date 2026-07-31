using OTKnowledgeOKF.Dto;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OTKnowledgeOKF.Utils
{
    public static class OKFUtils
    {
        public static string GenerateHeader(OKFHeaderConfig config)
        {
            var frontmatter = $"---\nprofile: \"{Yaml(config.Profile)}\"\nname: \"{Yaml(config.Name)}\"\ntitle: \"{Yaml(config.Title)}\"\ndescription: \"{Yaml(Regex.Replace(config.Description, @"\s+", " ").Trim())}\"\ncreated: \"{DateTimeOffset.Now:O}\"\n---";
            return frontmatter;
        }
    }
}
