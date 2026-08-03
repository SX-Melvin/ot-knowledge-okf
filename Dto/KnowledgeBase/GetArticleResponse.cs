using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OTKnowledgeOKF.Dto.KnowledgeBase
{
    public class GetArticleResponse
    {
        public GetArticleKbResult? Result { get; set; }
    }

    public class GetArticleKbResult
    {
        public List<GetArticleContainer>? Containers { get; set; }
    }

    public class GetArticleContainer
    {
        public List<GetArticleRow>? Rows { get; set; }
    }

    public class GetArticleRow
    {
        public List<GetArticleColumn>? Columns { get; set; }
    }

    public class GetArticleColumn
    {
        public List<GetArticleWidgets>? Widgets { get; set; }
    }

    public class GetArticleWidgets
    {
        public GetArticleWidgetData? Widget { get; set; }
    }

    public class GetArticleWidgetData
    {
        public GetArticleKbContent? Data { get; set; }
    }

    public class GetArticleKbContent
    {
        public List<GetArticleKbContentDataBreadCrumb>? BreadCrumb { get; set; }
        public string? ShortDesc { get; set; }
    }

    public class GetArticleKbContentDataBreadCrumb
    {
        public KnowledgeType Type { get; set; }
        public string? Label { get; set; }
    }

    public enum KnowledgeType
    {
        [EnumMember(Value = "kb_knowledge_base")]
        KnowledgeBase,

        [EnumMember(Value = "kb_category")]
        Category
    }
}
