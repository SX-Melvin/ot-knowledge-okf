using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace OTKnowledgeOKF.Dto.KnowledgeBase
{
    public class GetArticleResponse
    {
        public GetArticleKbResult Result { get; set; }
    }
    public class GetArticleKbResult
    {
        public GetArticleKbResultTheme Theme { get; set; }
    }
    public class GetArticleKbResultTheme
    {
        public List<GetArticleContainer> Containers { get; set; }
    }
    public class GetArticleContainer
    {
        public List<GetArticleRow> Rows { get; set; }
    }
    public class GetArticleRow
    {
        public List<GetArticleColumn> Columns { get; set; }
    }
    public class GetArticleColumn
    {
        public List<GetArticleWidgets> Widgets { get; set; }
    }
    public class GetArticleWidgets
    {
        public GetArticleWidgetData Widget { get; set; }
    }
    public class GetArticleWidgetData
    {
        public GetArticleKbContent Data { get; set; }
    }
    public class GetArticleKbContent
    {
        public GetArticleKbContentData KbContentData { get; set; }
        public List<GetArticleKbContentDataBreadCrumb> BreadCrumb { get; set; }
    }
    public class GetArticleKbContentDataBreadCrumb
    {
        public KnowledgeType Type { get; set; }
        public string Label { get; set; }
    }
    public class GetArticleKbContentData
    {
        public GetArticleKbContentDataData Data { get; set; }
    }
    public class GetArticleKbContentDataData
    {
        public string ShortDesc { get; set; }
    }

    public enum KnowledgeType
    {
        [JsonStringEnumMemberName("kb_knowledge_base")]
        KnowledgeBase,

        [JsonStringEnumMemberName("kb_category")]
        Category
    }
}
