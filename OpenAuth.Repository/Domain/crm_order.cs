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
	/// 订单（分离表单）
	/// </summary>
      [Table("crm_order")]
    public partial class crm_order : Entity
    {
        public crm_order()
        {
            this.customer_id = string.Empty;
            this.user_customer_id = string.Empty;
            this.docker = string.Empty;
            this.docker_tel = string.Empty;
            this.order_date = DateTime.Now;
            this.contract_type = string.Empty;
            this.contract_amount = 0;
            this.contract_sign_date = DateTime.Now;
            this.contract_no = string.Empty;
            this.service_type = string.Empty;
            this.service_duration = 0;
            this.promote_duration = 0;
            this.service_begin_date = DateTime.Now;
            this.service_end_date = DateTime.Now;
            this.cancel_type = string.Empty;
            this.cancel_reason = string.Empty;
            this.cancel_date = DateTime.Now;
            this.cancel_file_url = string.Empty;
            this.refund_status = 0;
            this.refund_amount = 0;
            this.refund_reason = string.Empty;
            this.refund_pay_type_id = string.Empty;
            this.refund_user = string.Empty;
            this.refund_account_no = string.Empty;
            this.refund_bank_name = string.Empty;
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
        /// 客户
        /// </summary>
        [Description("客户")]
        public string customer_id { get; set; }
        /// <summary>
        /// 我的客户
        /// </summary>
        [Description("我的客户")]
        public string user_customer_id { get; set; }
        /// <summary>
        /// 对接人
        /// </summary>
        [Description("对接人")]
        public string docker { get; set; }
        /// <summary>
        /// 对接人电话
        /// </summary>
        [Description("对接人电话")]
        public string docker_tel { get; set; }
        /// <summary>
        /// 提单日期
        /// </summary>
        [Description("提单日期")]
        public System.DateTime order_date { get; set; }
        /// <summary>
        /// 合同类型(关联字典表)
        /// </summary>
        [Description("合同类型(关联字典表)")]
        public string contract_type { get; set; }
        /// <summary>
        /// 合同金额
        /// </summary>
        [Description("合同金额")]
        public double contract_amount { get; set; }
        /// <summary>
        /// 合同签订日期
        /// </summary>
        [Description("合同签订日期")]
        public System.DateTime? contract_sign_date { get; set; }
        /// <summary>
        /// 合同编号
        /// </summary>
        [Description("合同编号")]
        public string contract_no { get; set; }
        /// <summary>
        /// 服务类型(关联字典表)
        /// </summary>
        [Description("服务类型(关联字典表)")]
        public string service_type { get; set; }
        /// <summary>
        /// 代账时长(可能要做可配)
        /// </summary>
        [Description("代账时长(可能要做可配)")]
        public int service_duration { get; set; }
        /// <summary>
        /// 促销时长
        /// </summary>
        [Description("促销时长")]
        public int promote_duration { get; set; }
        /// <summary>
        /// 服务开始日期
        /// </summary>
        [Description("服务开始日期")]
        public System.DateTime? service_begin_date { get; set; }
        /// <summary>
        /// 服务结束日期
        /// </summary>
        [Description("服务结束日期")]
        public System.DateTime? service_end_date { get; set; }
        /// <summary>
        /// 作废类型(关联字典表)
        /// </summary>
        [Description("作废类型(关联字典表)")]
        public string cancel_type { get; set; }
        /// <summary>
        /// 作废原因
        /// </summary>
        [Description("作废原因")]
        public string cancel_reason { get; set; }
        /// <summary>
        /// 作废日期
        /// </summary>
        [Description("作废日期")]
        public System.DateTime? cancel_date { get; set; }
        /// <summary>
        /// 作废三联合同路径
        /// </summary>
        [Description("作废三联合同路径")]
        public string cancel_file_url { get; set; }
        /// <summary>
        /// 是否退款(0.否1.是)
        /// </summary>
        [Description("是否退款(0.否1.是)")]
        public int refund_status { get; set; }
        /// <summary>
        /// 退款金额
        /// </summary>
        [Description("退款金额")]
        public double refund_amount { get; set; }
        /// <summary>
        /// 退款原因
        /// </summary>
        [Description("退款原因")]
        public string refund_reason { get; set; }
        /// <summary>
        /// 退款路径(关联字典表)
        /// </summary>
        [Description("退款路径(关联字典表)")]
        public string refund_pay_type_id { get; set; }
        /// <summary>
        /// 退款发起人
        /// </summary>
        [Description("退款发起人")]
        public string refund_user { get; set; }
        /// <summary>
        /// 退款账号(对公,对私,支付宝,微信)
        /// </summary>
        [Description("退款账号(对公,对私,支付宝,微信)")]
        public string refund_account_no { get; set; }
        /// <summary>
        /// 退款对公(私)账户开户行
        /// </summary>
        [Description("退款对公(私)账户开户行")]
        public string refund_bank_name { get; set; }
        /// <summary>
        /// 状态(-2.删除-1.回收站0.发起1.审批中2.生效(办结)3.作废)
        /// </summary>
        [Description("状态(-2.删除-1.回收站0.发起1.审批中2.生效(办结)3.作废)")]
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