namespace OpenAuth.App.Request
{
    public class QueryFlowListReq : PageReq
    {
        public string type { get; set; }
        public string uninId { get; set; }
    }
}
