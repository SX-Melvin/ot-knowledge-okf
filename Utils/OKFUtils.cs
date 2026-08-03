using OTKnowledgeOKF.Dto;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OTKnowledgeOKF.Utils
{
    public static class OKFUtils
    {
        private static string SanitizeString(string value) => Regex.Replace(value.Replace("\\", "\\\\").Replace("\"", "\\\""), @"\s+", " ").Trim();
        private static string SanitizeList(List<string> value) => string.Join(", ", value.Select(SanitizeString));

        public static string GenerateHeader(OKFHeaderConfig config)
        {
            return $"---\ntype: \"{SanitizeString(config.Type.ToString())}\"\nid: \"{SanitizeString(config.Id)}\"\nproduct: \"{SanitizeString(config.Product)}\"\nmodule: \"{SanitizeString(config.Module)}\"\nissue_type: \"{SanitizeString(config.IssueType)}\"\ntags: [{SanitizeList(config.Tags)}]\nconfidence: \"{SanitizeString(config.Confidence.ToString())}\"\nstatus: \"{SanitizeString(config.Status.ToString())}\"\nsensitivity: \"{SanitizeString(config.Sensitivity.ToString())}\"\nrelated: [{SanitizeList(config.Related)}]\n---";
        }
    }
}
