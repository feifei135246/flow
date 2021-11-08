﻿//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a CodeSmith Template.
//
//     DO NOT MODIFY contents of this file. Changes to this
//     file will be lost if the code is regenerated.
//     Author:Yubao Li
// </autogenerated>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAuth.Repository.Core;

namespace OpenAuth.Repository.Domain
{
    /// <summary>
	/// 工作流流程实例表
	/// </summary>
      [Table("fl_flowinstance_exlog")]
    public partial class FlowInstanceExLog : Entity
    {
        public FlowInstanceExLog()
        {
            this.ex_ip = "";
            this.ex_date = DateTime.Now;
            this.ex_cmd = "";
            this.ex_msg = "";
            this.ex_op_user_info = "";
        }

        /// <summary>
        /// 操作IP
        /// </summary>
        [Description("操作IP")]
        public string ex_ip { get; set; }
        /// <summary>
        /// 异常时间
        /// </summary>
        [Description("异常时间")]
        public DateTime ex_date { get; set; }
        /// <summary>
        /// 异常操作
        /// </summary>
        [Description("异常操作")]
        public string ex_cmd { get; set; }
        /// <summary>
        /// 异常描述
        /// </summary>
        [Description("异常描述")]
        public string ex_msg { get; set; }
        /// <summary>
        /// 操作用户信息
        /// </summary>
        [Description("操作用户信息")]
        public string ex_op_user_info { get; set; }
    }
}