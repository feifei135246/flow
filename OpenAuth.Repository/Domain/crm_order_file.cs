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
	/// 订单附件（分离表单）
	/// </summary>
      [Table("crm_order_file")]
    public partial class crm_order_file : Entity
    {
        public crm_order_file()
        {
            this.order_id = string.Empty;
            this.file_type = 0;
            this.file_name = string.Empty;
            this.file_url = string.Empty;
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
        /// 资料类型(1.证照2.附件3.合同)
        /// </summary>
        [Description("资料类型(1.证照2.附件3.合同)")]
        public int file_type { get; set; }
        /// <summary>
        /// 文件名
        /// </summary>
        [Description("文件名")]
        public string file_name { get; set; }
        /// <summary>
        /// 资料路径
        /// </summary>
        [Description("资料路径")]
        public string file_url { get; set; }
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