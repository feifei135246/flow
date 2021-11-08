// ***********************************************************************
// Assembly         : OpenAuth.App
// Author           : 李玉宝
// Created          : 07-19-2018
//
// Last Modified By : 李玉宝
// Last Modified On : 07-19-2018
// ***********************************************************************
// <copyright file="FlowInstanceApp.cs" company="OpenAuth.App">
//     Copyright (c) http://www.openauth.me. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using Infrastructure;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using OpenAuth.App.Flow;
using OpenAuth.App.Interface;
using OpenAuth.App.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository.Domain;
using OpenAuth.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace OpenAuth.App
{
    /// <summary>
    /// 工作流实例表操作
    /// </summary>
    public class FlowInstanceExLogApp:BaseApp<FlowInstanceExLog>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value>The unit work.</value>
        protected IUnitWork UnitWork;
        private IAuth _auth;

        public FlowInstanceExLogApp(IUnitWork unitWork, IRepository<FlowInstanceExLog> repository,
            RevelanceManagerApp app, IAuth auth) : base(unitWork, repository)
        {
            _auth = auth;
            UnitWork = unitWork;
        }

        public void Add(FlowInstanceExLog exlog)
        {
            var tag = _auth.GetLoginUser();
            exlog.ex_op_user_info = tag;
            UnitWork.Add(exlog);
            UnitWork.Save();
        }

    }
}