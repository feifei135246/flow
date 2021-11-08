namespace OpenAuth.App.Request
{
    public class QueryFlowInstanceHistoryReq : PageReq
    {
        /// <summary>
        /// 流程实体名称
        /// </summary>
        public string FlowInstanceId { get; set; }

        /// <summary>
        /// 外联id
        /// </summary>
        public string UninId { get; set; }
    }
}
