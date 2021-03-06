//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a CodeSmith Template.
//
//     DO NOT MODIFY contents of this file. Changes to this
//     file will be lost if the code is regenerated.
//     Author:Yubao Li
// </autogenerated>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using OpenAuth.Repository.Core;

namespace OpenAuth.Repository.Domain
{
    /// <summary>
	/// 服务（分离表单）
	/// </summary>
      [Table("crm_order_service")]
    public partial class crm_order_service : Entity
    {
        public crm_order_service()
        {
            this.order_id = string.Empty;
            this.service_id = 0;
            this.amount = 0;
            this.remark = string.Empty;
            this.status = 0;
            this.create_dept_id = 0;
            this.create_user_id = string.Empty;
            this.create_user_name = string.Empty;
            this.create_date = DateTime.Now;
            this.update_user_id = string.Empty;
            this.update_user_name = string.Empty;
            this.update_date = DateTime.Now;
        }

        /// <summary>
        /// 订单
        /// </summary>
        [Description("订单")]
        public string order_id { get; set; }
        /// <summary>
        /// 服务明细ID(关联字典表)
        /// </summary>
        [Description("服务明细ID(关联字典表)")]
        public int service_id { get; set; }
        /// <summary>
        /// 金额
        /// </summary>
        [Description("金额")]
        public int amount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Description("备注")]
        public string remark { get; set; }
        
        /// <summary>
        /// 状态(-2.删除-1.回收站0.正常)
        /// </summary>
        [Description("状态(-2.删除-1.回收站0.正常)")]
        public int status { get; set; }
        /// <summary>
        /// 创建人机构ID
        /// </summary>
        [Description("创建人机构ID")]
        public int create_dept_id { get; set; }
        /// <summary>
        /// 创建用户主键
        /// </summary>
        [Description("创建用户主键")]
        public string create_user_id { get; set; }
        /// <summary>
	    /// 创建用户
	    /// </summary>
         [Description("创建用户")]
        public string create_user_name { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        [Description("创建时间")]
        public System.DateTime? create_date { get; set; }
        /// <summary>
        /// 最后编辑人ID
        /// </summary>
        [Description("最后编辑人ID")]
        public string update_user_id { get; set; }
        /// <summary>
        /// 创建用户
        /// </summary>
        [Description("最后编辑人姓名")]
        public string update_user_name { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        [Description("最后编辑时间")]
        public System.DateTime? update_date { get; set; }

    }
}