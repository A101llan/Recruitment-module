using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HR.Web.Services
{
    /// <summary>
    /// Simple stub implementation of MCPService for compatibility
    /// </summary>
    public class MCPService
    {
        /// <summary>
        /// Stub implementation of CallToolAsync
        /// </summary>
        public async Task<MCPResult> CallToolAsync(string toolName, object parameters)
        {
            // Return a default result for all tool calls
            await Task.Delay(100); // Simulate some processing time
            
            // Create a stub response that matches expected structure
            var stubResponse = new MCPResultData
            {
                contents = new[]
                {
                    new MCPContent
                    {
                        text = JsonConvert.SerializeObject(new AnswerEvaluationResponse
                        {
                            score = 5, // Default middle score
                            reasoning = "MCPService stub - AI evaluation not available",
                            confidence = 0.5m // Use 'm' suffix for decimal
                        })
                    }
                }
            };
            
            return new MCPResult
            {
                Success = true,
                Message = $"MCPService stub response for tool '{toolName}'",
                Data = null,
                Result = stubResponse
            };
        }
    }

    /// <summary>
    /// Result class for MCP service calls
    /// </summary>
    public class MCPResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public MCPResultData Result { get; set; }
    }

    /// <summary>
    /// Data structure for MCP result
    /// </summary>
    public class MCPResultData
    {
        public MCPContent[] contents { get; set; }
    }

    /// <summary>
    /// Content structure for MCP result
    /// </summary>
    public class MCPContent
    {
        public string text { get; set; }
    }
}
