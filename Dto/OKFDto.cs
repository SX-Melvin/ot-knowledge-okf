namespace OTKnowledgeOKF.Dto
{
    public class OKFDto
    {
    
    }
    public class OKFHeaderConfig
    {
        public string Id { get; set; }
        public OKFHeaderType Type { get; set; }
        public string Product { get; set; }
        public string Module { get; set; }
        public string IssueType { get; set; }
        public List<string> Tags { get; set; }
        public OKFHeaderConfidence Confidence { get; set; }
        public OKFHeaderStatus Status { get; set; }
        public OKFHeaderSensitivity Sensitivity { get; set; }
        public List<string> Related { get; set; }
    }
    public enum OKFHeaderConfidence
    {
        Probable
    }
    public enum OKFHeaderType
    {
        KnowledgeBaseArticle,
        SupportCaseThread
    }
    public enum OKFHeaderStatus
    {
        Process,
        Solved
    }
    public enum OKFHeaderSensitivity
    {
        Public,
        Internal,
        Confidential
    }
}
