namespace OTKnowledgeOKF.Dto
{
    public class OllamaDto
    {
        public class OllamaChatResponse
        {
            public OllamaMessage? Message { get; set; }
        }

        public class OllamaMessage
        {
            public string? Role { get; set; }

            public string? Content { get; set; }
        }
    }
}
