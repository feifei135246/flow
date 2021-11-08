// <copyright file="FlowInstancesController.cs" company="openauth.me">
// Copyright (c) 2019 openauth.me. All rights reserved.
// </copyright>
// <author>www.cnblogs.com/yubaolee</author>
// <date>2018-09-06</date>
// <summary>流程实例控制器</summary>

using System;
using System.Collections.Generic;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenAuth.App;
using OpenAuth.App.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository.Domain;

namespace OpenAuth.WebApi.Controllers
{
    /// <summary>
    /// 流程实例
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FlowInstancesController : ControllerBase
    {
        private readonly FlowInstanceApp _app;
        private readonly FlowInstanceExLogApp _log; 
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public Response<FlowVerificationResp> Get(string id)
        {
            var result = new Response<FlowVerificationResp>();
            try
            {
                var flowinstance = _app.GetByUninId(id);
                if (flowinstance == null)
                {
                    result.Code = 1000;
                    result.Message = "未找到相关流程";
                }
                else {
                    result.Result = flowinstance.MapTo<FlowVerificationResp>();
                    result.Code = 0;
                }
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 获取一个流程实例的操作历史记录
        /// </summary>
        [HttpGet]
        public Response<List<FlowInstanceOperationHistory>> QueryHistories([FromQuery]QueryFlowInstanceHistoryReq request)
        {
            var result = new Response<List<FlowInstanceOperationHistory>>();
            try
            {
                FlowInstance temp = _app.GetByUninId(request.UninId);
                if (temp != null)
                {
                    request.FlowInstanceId = temp.Id;
                    result.Result = _app.QueryHistories(request);
                }
                else {
                    result.Code = 200;
                    result.Message = "该未找到该提单信息，请核查";
                }
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return result;
        }


        ///// <summary>创建一个新的流程实例</summary>
        ///// <remarks> www.cnblogs.com/yubaolee, 2019-03-06. </remarks>
        //[HttpPost]
        //public Response Add([FromBody]AddFlowInstanceReq obj)
        //{
        //    var result = new Response();
        //    try
        //    {
        //        _app.CreateInstance(obj);
        //    }
        //    catch (Exception ex)
        //    {
        //        result.Code = 500;
        //        result.Message = ex.InnerException?.Message ?? ex.Message;
        //    }

        //    return result;
        //}

        /// <summary>创建一个新的流程实例</summary>
        /// <remarks> www.cnblogs.com/yubaolee, 2019-03-06. </remarks>
        [HttpPost]
        public Response AddInst([FromBody]AddFlowInstanceReq obj)
        {
            var result = new Response() { Code = 0 };
            try
            {
                _app.CreateInstance2(obj);
                return result;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
                if (ex.InnerException != null) {
                    result.Code = 110110;
                    result.Message = "流程内部异常，请联系管理员";
                    _log.Add(new FlowInstanceExLog() { ex_cmd = "创建流程实例", ex_msg = $"创建流程异常,异常描述：{ex.Message},提交表单信息：{JsonHelper.Instance.Serialize(obj)}" });
                }
                return result;
            }

        }


        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost]
        public Response Update(FlowInstance obj)
        {
            var result = new Response();
            try
            {
                _app.Update(obj);

            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
                _log.Add(new FlowInstanceExLog() { ex_cmd = "修改流程实例", ex_msg = $"修改流程异常,异常描述：{ex.Message},提交表单信息：{JsonHelper.Instance.Serialize(obj)}" });
            }

            return result;
        }
        /// <summary>
        /// 审批
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public Response<FlowInstance> Verification(VerificationReq request)
        {
            var response = new Response<FlowInstance>();
            try
            {
                if (!string.IsNullOrEmpty(request.UninId))
                {
                    var flowinstance = _app.GetByUninId(request.UninId);
                    request.FlowInstanceId = flowinstance.Id;
                }
                bool isOk = _app.Verification2(request);
                if (isOk) {
                    response.Result = _app.GetByUninId(request.UninId);
                }
                else {
                    response.Code = 10000;
                    response.Message = "非当前流程节点审批人";
                }
            }
            catch (Exception ex)
            {
                response.Code = 500;
                response.Message = ex.InnerException?.Message ?? ex.Message;
                _log.Add(new FlowInstanceExLog() { ex_cmd = "审批流程实例", ex_msg = $"审批流程异常,异常描述：{ex.Message},提交表单信息：{JsonHelper.Instance.Serialize(request)}" });
            }

            return response;
        }

        /// <summary>
        /// 加载列表
        /// </summary>
        [HttpGet]
        public TableData Load([FromQuery]QueryFlowInstanceListReq request)
        {
            return _app.GetList(request);
        }

        /// <summary>
        /// 加载列表
        /// </summary>
        [HttpGet]
        public TableData Load_Sim([FromQuery]QueryFlowInstanceListReq request)
        {
            return _app.GetList_Sim(request);
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        public Response Delete([FromBody]string[] ids)
        {
            var result = new Response();
            try
            {
                _app.Delete(ids);

            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return result;
        }

        public FlowInstancesController(FlowInstanceApp app, FlowInstanceExLogApp log) 
        {
            _app = app;
            _log = log;
        }
    }
}