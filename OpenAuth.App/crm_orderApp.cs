using System;
using System.Linq;
using Infrastructure;
using OpenAuth.App.Interface;
using OpenAuth.App.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository.Domain;
using OpenAuth.Repository.Interface;


namespace OpenAuth.App
{
    public class crm_orderApp : BaseApp<crm_order>, ICustomerForm
    {
        private RevelanceManagerApp _revelanceApp;

        /// <summary>
        /// 加载列表
        /// </summary>
        public TableData Load(Querycrm_orderReq request)
        {
             return new TableData
            {
                count = Repository.GetCount(null),
                data = Repository.Find(request.page, request.limit, "Id desc")
            };
        }

        public void Add(crm_order obj)
        {
            Repository.Add(obj);
        }
        
        public void Update(crm_order obj)
        {
            UnitWork.Update<crm_order>(u => u.Id == obj.Id, u => new crm_order
            {
               //todo:要修改的字段赋值
            });

        }

        public crm_orderApp(IUnitWork unitWork, IRepository<crm_order> repository,
            RevelanceManagerApp app) : base(unitWork, repository)
        {
            _revelanceApp = app;
        }

        public void Add(string flowInstanceId, string frmData)
        {
            var req = JsonHelper.Instance.Deserialize<crm_order>(frmData);
            //req.FlowInstanceId = flowInstanceId;
            Add(req);
        }
    }
}