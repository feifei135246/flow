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
    public class FlowInstanceApp : BaseApp<FlowInstance>
    {
        private RevelanceManagerApp _revelanceApp;
        private FlowSchemeApp _flowSchemeApp;
        private FormApp _formApp;
        private IHttpClientFactory _httpClientFactory;

        private IAuth _auth;

        #region 流程处理API

        public FlowInstance GetByUninId(string unid) {

            return UnitWork.FindSingle<FlowInstance>(o => o.UninId == unid);
        }

        /// <summary>
        /// 创建一个实例
        /// </summary>
        /// <returns></returns>
        public bool CreateInstance2(AddFlowInstanceReq addFlowInstanceReq)
        {
            FlowScheme scheme = null;
            if (!string.IsNullOrEmpty(addFlowInstanceReq.Type)) {
                scheme = UnitWork.Find<FlowScheme>(1, 5, "CreateDate descending",o =>o.SchemeType== addFlowInstanceReq.Type).FirstOrDefault();
                //addFlowInstanceReq.SchemeId = Define.ORDER;
            }
            
            if ((scheme == null) && !string.IsNullOrEmpty(addFlowInstanceReq.SchemeCode))
            {
                scheme = _flowSchemeApp.FindByCode(addFlowInstanceReq.SchemeCode);
            }

            if (scheme == null)
            {
                throw new Exception("该流程模板已不存在，请重新设计流程");
            }
            FlowInstance sigT = UnitWork.FindSingle<FlowInstance>(o => o.UninId == addFlowInstanceReq.UninId && o.SchemeType == addFlowInstanceReq.Type && (o.IsFinish ==0));
            if (sigT != null)
            {
                throw new Exception("流程已在审批中，请勿重复提交");
            }
            //if (!string.IsNullOrEmpty(addFlowInstanceReq.SchemeId))
            //{
            //    scheme = _flowSchemeApp.Get(addFlowInstanceReq.SchemeId);
            //}
            FlowInstance temp = UnitWork.FindSingle<FlowInstance>(o => o.UninId == addFlowInstanceReq.UninId);
            addFlowInstanceReq.SchemeContent = scheme.SchemeContent;

            var form = _formApp.FindSingle(scheme.FrmId);
            //20191204 可不添加模板
            if (form != null)
            {
                //throw new Exception("该流程模板对应的表单已不存在，请重新设计流程");
                addFlowInstanceReq.FrmContentData = form.ContentData;
                addFlowInstanceReq.FrmContentParse = form.FrmType == 1 ? form.Content : form.ContentParse;
                addFlowInstanceReq.FrmType = form.FrmType;
                addFlowInstanceReq.FrmId = form.Id;
            }
            FlowInstance flowInstance = addFlowInstanceReq.MapTo<FlowInstance>();

            //创建运行实例
            var wfruntime = new FlowRuntime(flowInstance);

            //获取用户信息
            var tag = _auth.GetLoginUser();
            dynamic user = JsonHelper.Instance.Deserialize<object>(tag);
            #region 根据运行实例改变当前节点状态
            flowInstance.Code = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            flowInstance.CustomName = "OR" + flowInstance.Code;
            flowInstance.ActivityId = wfruntime.nextNodeId;
            flowInstance.ActivityType = wfruntime.GetNextNodeType();
            flowInstance.ActivityName = wfruntime.nextNode.name;
            flowInstance.PreviousId = wfruntime.currentNodeId;
            flowInstance.CreateUserId = user.employee_id;
            flowInstance.CreateUserName = user.employee_name;
            flowInstance.CreateCompanyId = user.organ_id;
            flowInstance.CreateDeptId = user.department_id;
            flowInstance.MakerList = (wfruntime.GetNextNodeType() != 4 ? GetNextMakers2(wfruntime, flowInstance.CreateUserId, flowInstance.CreateCompanyId) : "");
            flowInstance.IsFinish = (wfruntime.GetNextNodeType() == 4 ? 1 : 0);
            flowInstance.SchemeType = scheme.SchemeType;
            flowInstance.SchemeId = scheme.Id;
            if (temp != null)
            {
                temp.ActivityId =flowInstance.ActivityId;
                temp.ActivityType =flowInstance.ActivityType;
                temp.ActivityName =flowInstance.ActivityName;
                temp.PreviousId = flowInstance.PreviousId;
                temp.MakerList = flowInstance.MakerList;
                temp.SchemeId = flowInstance.SchemeId;
                temp.SchemeType = flowInstance.SchemeType;
                temp.SchemeContent = flowInstance.SchemeContent;
                UnitWork.Update(temp);
                #region 流程操作记录
                //设置审批记录
                FlowInstanceOperationHistory processOperationHistoryEntity2 = new FlowInstanceOperationHistory
                {
                    InstanceId = temp.Id,
                    CreateUserId = user.employee_id,
                    CreateUserName = user.employee_name,
                    CreateUserJob = Convert.ToString(user.job_name),
                    CreateDate = DateTime.Now,
                    Opinion = "",
                    NodeId = wfruntime.currentNode.id,
                    NodeName = wfruntime.currentNode.name,
                    BeginTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    Content = "【重新发起流程】,【"
                              + user.employee_name + "-" + Convert.ToString(user.job_name) + "】"
                };
                FlowInstanceOperationHistory NextNodeHistoryEntity2 = new FlowInstanceOperationHistory
                {
                    InstanceId = temp.Id,
                    //CreateUserId = user.id,
                    //CreateUserName = user.name,
                    //CreateUserJob = user.job_name,
                    CreateDate = DateTime.Now,
                    Opinion = "",
                    NodeId = wfruntime.nextNode.id,
                    NodeName = wfruntime.nextNode.name,
                    BeginTime = processOperationHistoryEntity2.EndTime,
                    OperateList = JsonHelper.Instance.Serialize(wfruntime.nextNode.setInfo)
                    //EndTime = DateTime.Now,
                    //Content = "【发起流程】,【"
                    //          + user.name + "-" + user.job_name + "】"
                };
                UnitWork.Add(processOperationHistoryEntity2);
                UnitWork.Add(NextNodeHistoryEntity2);
                #endregion 流程操作记录

                AddTransHistory2(wfruntime);
                UnitWork.Save();
                return true;
                //throw new Exception("该订单流程已存在");
            }

            UnitWork.Add(flowInstance);
            wfruntime.flowInstanceId = flowInstance.Id;

            if (flowInstance.FrmType == 1)//内置表单库表
            {
                //var t = Type.GetType("OpenAuth.App."+ flowInstance.DbName +"App");
                //ICustomerForm icf = (ICustomerForm)AutofacExt.GetFromFac(t);
                //icf.Add(flowInstance.Id, flowInstance.FrmData);
            }

            #endregion 根据运行实例改变当前节点状态

            #region 流程操作记录
            FlowNode nnode = wfruntime.GetNextNode(wfruntime.currentNode.id);
            FlowNode nnnode = wfruntime.GetNextNode(wfruntime.nextNode.id);
            //设置审批记录
            FlowInstanceOperationHistory processOperationHistoryEntity = new FlowInstanceOperationHistory
            {
                InstanceId = flowInstance.Id,
                CreateUserId = user.employee_id,
                CreateUserName = user.employee_name,
                CreateUserJob = Convert.ToString(user.job_name),
                CreateDate = DateTime.Now,
                Opinion="",
                NodeId = wfruntime.currentNode.id,
                NodeName = wfruntime.currentNode.name,
                BeginTime = DateTime.Now,
                EndTime = DateTime.Now,
                NextNodeId = nnode==null?"": nnode.id,
                NextNodeName = nnode == null ? "" : nnode.name,
                Content = "【发起流程】,【"
                          + user.employee_name + "-"+ Convert.ToString(user.job_name) + "】"
            };
            FlowInstanceOperationHistory NextNodeHistoryEntity = new FlowInstanceOperationHistory
            {
                InstanceId = flowInstance.Id,
                //CreateUserId = user.id,
                //CreateUserName = user.name,
                //CreateUserJob = user.job_name,
                CreateDate = DateTime.Now,
                Opinion = "",
                NodeId = wfruntime.nextNode.id,
                NodeName = wfruntime.nextNode.name,
                BeginTime = processOperationHistoryEntity.EndTime,
                OperateList = JsonHelper.Instance.Serialize(wfruntime.nextNode.setInfo),
                NextNodeId = nnnode==null?"": nnnode.id,
                NextNodeName = nnnode == null ? "" : nnnode.name
                //EndTime = DateTime.Now,
                //Content = "【发起流程】,【"
                //          + user.name + "-" + user.job_name + "】"
            };
            UnitWork.Add(processOperationHistoryEntity);
            UnitWork.Add(NextNodeHistoryEntity);
            #endregion 流程操作记录

            AddTransHistory2(wfruntime);
            UnitWork.Save();
            return true;
        }

        /// <summary>
        /// 创建一个实例
        /// </summary>
        /// <returns></returns>
        public bool CreateInstance(AddFlowInstanceReq addFlowInstanceReq)
        {
            FlowScheme scheme = null;
            if (!string.IsNullOrEmpty(addFlowInstanceReq.SchemeId))
            {
                scheme = _flowSchemeApp.Get(addFlowInstanceReq.SchemeId);
            }

            if ((scheme == null) && !string.IsNullOrEmpty(addFlowInstanceReq.SchemeCode))
            {
                scheme = _flowSchemeApp.FindByCode(addFlowInstanceReq.SchemeCode);
            }

            if (scheme == null)
            {
                throw new Exception("该流程模板已不存在，请重新设计流程");
            }

            addFlowInstanceReq.SchemeContent = scheme.SchemeContent;

            var form = _formApp.FindSingle(scheme.FrmId);
            //20191204 可不添加模板
            if (form != null)
            {
                //throw new Exception("该流程模板对应的表单已不存在，请重新设计流程");
                addFlowInstanceReq.FrmContentData = form.ContentData;
                addFlowInstanceReq.FrmContentParse = form.FrmType == 1 ? form.Content : form.ContentParse;
                addFlowInstanceReq.FrmType = form.FrmType;
                addFlowInstanceReq.FrmId = form.Id;
            }
            var flowInstance = addFlowInstanceReq.MapTo<FlowInstance>();

            //创建运行实例
            var wfruntime = new FlowRuntime(flowInstance);
            var user = _auth.GetCurrentUser();

            #region 根据运行实例改变当前节点状态

            flowInstance.ActivityId = wfruntime.nextNodeId;
            flowInstance.ActivityType = wfruntime.GetNextNodeType();
            flowInstance.ActivityName = wfruntime.nextNode.name;
            flowInstance.PreviousId = wfruntime.currentNodeId;
            flowInstance.CreateUserId = user.User.Id;
            flowInstance.CreateUserName = user.User.Account;
            flowInstance.MakerList = (wfruntime.GetNextNodeType() != 4 ? GetNextMakers(wfruntime) : "");
            flowInstance.IsFinish = (wfruntime.GetNextNodeType() == 4 ? 1 : 0);

            UnitWork.Add(flowInstance);
            wfruntime.flowInstanceId = flowInstance.Id;

            if (flowInstance.FrmType == 1)//内置表单库表
            {
                //var t = Type.GetType("OpenAuth.App."+ flowInstance.DbName +"App");
                //ICustomerForm icf = (ICustomerForm)AutofacExt.GetFromFac(t);
                //icf.Add(flowInstance.Id, flowInstance.FrmData);
            }

            #endregion 根据运行实例改变当前节点状态

            #region 流程操作记录

            FlowInstanceOperationHistory processOperationHistoryEntity = new FlowInstanceOperationHistory
            {
                InstanceId = flowInstance.Id,
                CreateUserId = user.User.Id,
                CreateUserName = user.User.Name,
                CreateDate = DateTime.Now,
                Content = "【创建】"
                          + user.User.Name
                          + "创建了一个流程进程【"
                          + addFlowInstanceReq.Code + "/"
                          + addFlowInstanceReq.CustomName + "】"
            };
            UnitWork.Add(processOperationHistoryEntity);

            #endregion 流程操作记录

            AddTransHistory(wfruntime);
            UnitWork.Save();
            return true;
        }

        /// <summary>
        /// 节点审核
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public bool NodeVerification(VerificationReq request, Tag tag)
        {
            FlowInstance flowInstance = Get(request.FlowInstanceId);//获取当前流程信息
            FlowInstanceOperationHistory flowInstanceOperationHistory = new FlowInstanceOperationHistory
            {
                InstanceId = request.FlowInstanceId,
                CreateUserId = tag.UserId,
                CreateUserName = tag.UserName,
                CreateDate = DateTime.Now
            };//操作记录
            FlowRuntime wfruntime = new FlowRuntime(flowInstance);

            #region 会签

            if (flowInstance.ActivityType == 0)//当前节点是会签节点
            {
                //TODO: 标记会签节点的状态，这个地方感觉怪怪的
                wfruntime.MakeTagNode(wfruntime.currentNodeId, tag);

                string canCheckId = ""; //寻找当前登录用户可审核的节点Id
                foreach (string nodeId in wfruntime.FromNodeLines[wfruntime.currentNodeId].Select(u => u.to))
                {
                    var makerList = GetNodeMakers(wfruntime.Nodes[nodeId]);
                    if (string.IsNullOrEmpty(makerList)) continue;

                    if (makerList.Split(',').Any(one => tag.UserId == one))
                    {
                        canCheckId = nodeId;
                    }
                }

                if (canCheckId == "")
                {
                    throw (new Exception("审核异常,找不到审核节点"));
                }

                flowInstanceOperationHistory.Content = "【" + wfruntime.Nodes[canCheckId].name
                                                           + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                                                           + "】" + (tag.Taged == 1 ? "同意" : "不同意") + ",备注："
                                                           + tag.Description;

                wfruntime.MakeTagNode(canCheckId, tag); //标记审核节点状态
                string res = wfruntime.NodeConfluence(canCheckId, tag);
                if (res == TagState.No.ToString("D"))
                {
                    flowInstance.IsFinish = 3;
                }
                else if(!string.IsNullOrEmpty(res))
                {
                    flowInstance.PreviousId = flowInstance.ActivityId;
                    flowInstance.ActivityId = wfruntime.nextNodeId;
                    flowInstance.ActivityType = wfruntime.nextNodeType;
                    flowInstance.ActivityName = wfruntime.nextNode.name;
                    flowInstance.IsFinish = (wfruntime.nextNodeType == 4 ? 1 : 0);
                    flowInstance.MakerList =
                        (wfruntime.nextNodeType == 4 ? "" : GetNextMakers(wfruntime));

                    AddTransHistory(wfruntime);
                }
              
            }
            #endregion 会签

            #region 一般审核

            else
            {
                if (tag.Taged == (int) TagState.Ok)//审核通过
                {
                    wfruntime.MakeTagNode(wfruntime.currentNodeId, tag);
                    flowInstance.PreviousId = flowInstance.ActivityId;
                    flowInstance.ActivityId = wfruntime.nextNodeId;
                    flowInstance.ActivityType = wfruntime.nextNodeType;
                    flowInstance.ActivityName = wfruntime.nextNode.name;
                    flowInstance.MakerList = string.IsNullOrEmpty(request.MakerList) ? (wfruntime.nextNodeType == 4 ? "" : GetNextMakers(wfruntime)): request.MakerList;
                    flowInstance.IsFinish = (wfruntime.nextNodeType == 4 ? 1 : 0);
                    AddTransHistory(wfruntime);
                }
                else//审核失败（滞留当前节点）
                {
                    flowInstance.IsFinish = 3; //表示该节点不同意
                }
                flowInstanceOperationHistory.Content = "【" + wfruntime.currentNode.name
                                                           + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                                                           + "】" + (tag.Taged == 1 ? "同意" : "不同意") + ",备注："
                                                           + tag.Description;
            }

            #endregion 一般审核

            flowInstance.SchemeContent = JsonHelper.Instance.Serialize(wfruntime.ToSchemeObj());

            UnitWork.Update(flowInstance);
            UnitWork.Add(flowInstanceOperationHistory);
            UnitWork.Save();

            wfruntime.NotifyThirdParty(_httpClientFactory.CreateClient(), tag);
            return true;
        }

        /// <summary>
        /// 节点审核
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public bool NodeVerification2(VerificationReq request, Tag tag)
        {
            FlowInstance flowInstance = Get(request.FlowInstanceId);//获取当前流程信息
            FlowRuntime wfruntime = new FlowRuntime(flowInstance);
            FlowInstanceOperationHistory currNodeHistory = UnitWork.Find<FlowInstanceOperationHistory>(1,1,"CreateDate descending", o =>
             o.NodeId == wfruntime.currentNodeId && o.InstanceId == flowInstance.Id).FirstOrDefault();
            //记录当前节点审核记录
            FlowInstanceLog log = new FlowInstanceLog()
            {
                Id = Guid.NewGuid().ToString(),
                FlowinstanceId = flowInstance.Id,
                UninId = flowInstance.UninId,
                ActivityId = flowInstance.ActivityId,
                ActivityType = flowInstance.ActivityType,
                ActivityName = flowInstance.ActivityName,
                PreviousId = flowInstance.PreviousId,
                MakerList = flowInstance.MakerList,
                MakerNameList = "",
                CreateCompanyId = flowInstance.CreateCompanyId,
                CreateDeptId = flowInstance.CreateDeptId,
                CreateDate = DateTime.Now,
                CreateUserId = tag.UserId,
                CreateUserName = tag.UserName,
                CreateCompanyName = "",
                CreateDeptName = ""
            };
            //设置审批记录
            if (currNodeHistory == null)
            {
                currNodeHistory = new FlowInstanceOperationHistory()
                {
                    Id=string.Empty,
                    Result = tag.Taged.ToString(),
                    CreateUserId = tag.UserId,
                    CreateUserName = tag.UserName,
                    CreateDate = DateTime.Now,
                    Opinion = tag.Description,
                    CreateUserJob = tag.UserJob,
                    NodeId = wfruntime.currentNode.id,
                    NodeName = wfruntime.currentNode.name,
                    BeginTime =  DateTime.Now,
                    EndTime = DateTime.Now,
                    NextNodeId = wfruntime.nextNode.id,
                    NextNodeName = wfruntime.nextNode.name,
                };
            }
            else {
                currNodeHistory.Result = tag.Taged.ToString();
                currNodeHistory.CreateUserId = tag.UserId;
                currNodeHistory.CreateUserName = tag.UserName;
                currNodeHistory.CreateDate = DateTime.Now;
                currNodeHistory.Opinion = tag.Description;
                currNodeHistory.CreateUserJob = tag.UserJob;
                currNodeHistory.NodeId = wfruntime.currentNode.id;
                currNodeHistory.NodeName = wfruntime.currentNode.name;
                currNodeHistory.EndTime = DateTime.Now;
                currNodeHistory.NextNodeId = wfruntime.nextNode.id;
                currNodeHistory.NextNodeName = wfruntime.nextNode.name;
            }

            #region 会签
            if (flowInstance.ActivityType == 0)//当前节点是会签节点
            {
                //TODO: 标记会签节点的状态，这个地方感觉怪怪的
                wfruntime.MakeTagNode(wfruntime.currentNodeId, tag);

                string canCheckId = ""; //寻找当前登录用户可审核的节点Id
                foreach (string nodeId in wfruntime.FromNodeLines[wfruntime.currentNodeId].Select(u => u.to))
                {
                    var makerList = GetNodeMakers2(wfruntime.Nodes[nodeId], flowInstance.CreateUserId, tag.UserOrganId);
                    if (string.IsNullOrEmpty(makerList)) continue;

                    if (makerList.Split(',').Any(one => tag.UserId == one))
                    {
                        canCheckId = nodeId;
                    }
                }

                if (canCheckId == "")
                {
                    throw (new Exception("审核异常,找不到审核节点"));
                }

                currNodeHistory.Content = "【" + (tag.Taged == 1 ? "批准" : "驳回") + "】【" +
                    tag.UserName + "-" + tag.UserJob + "】" +
                    ",审批意见：" + tag.Description;

                wfruntime.MakeTagNode(canCheckId, tag); //标记审核节点状态
                string res = wfruntime.NodeConfluence(canCheckId, tag);
                if (res == TagState.No.ToString("D"))
                {
                    flowInstance.IsFinish = 3;
                }
                else if (!string.IsNullOrEmpty(res))
                {
                    flowInstance.PreviousId = flowInstance.ActivityId;
                    flowInstance.ActivityId = wfruntime.nextNodeId;
                    flowInstance.ActivityType = wfruntime.nextNodeType;
                    flowInstance.ActivityName = wfruntime.nextNode.name;
                    flowInstance.IsFinish = (wfruntime.nextNodeType == 4 ? 1 : 0);
                    flowInstance.MakerList =
                        (wfruntime.nextNodeType == 4 ? "" : GetNextMakers(wfruntime));

                    AddTransHistory2(wfruntime);
                }

            }
            #endregion 会签
            #region 一般审核
            else
            {
                if (tag.Taged == (int)TagState.Ok)//审核通过
                {
                    wfruntime.MakeTagNode(wfruntime.currentNodeId, tag);
                    flowInstance.PreviousId = flowInstance.ActivityId;
                    flowInstance.ActivityId = wfruntime.nextNodeId;
                    flowInstance.ActivityType = wfruntime.nextNodeType;
                    flowInstance.ActivityName = wfruntime.nextNode.name;
                    flowInstance.MakerList = string.IsNullOrEmpty(request.MakerList) ? (wfruntime.nextNodeType == 4 ? "" : GetNextMakers2(wfruntime, flowInstance.CreateUserId,tag.UserOrganId)) : request.MakerList;
                    flowInstance.IsFinish = (wfruntime.nextNodeType == 4 ? 1 : 0);
                    AddTransHistory2(wfruntime);
                }
                else//审核失败（滞留当前节点）
                {
                    flowInstance.IsFinish = 3; //表示该节点不同意
                }
                currNodeHistory.Content = "【" + (tag.Taged == 1 ? "批准" : "驳回") + "】【" +
                    tag.UserName + "-" + tag.UserJob + "】" +
                    ",审批意见：" + tag.Description;
            }
            #endregion 一般审核

            flowInstance.SchemeContent = JsonHelper.Instance.Serialize(wfruntime.ToSchemeObj());
            FlowNode nnnode = wfruntime.GetNextNode(wfruntime.nextNode.id);
            //设置审批记录、下一节点
            FlowInstanceOperationHistory NextNodeHistoryEntity = new FlowInstanceOperationHistory {
                InstanceId = flowInstance.Id,
                CreateDate = DateTime.Now,
                Opinion = "",
                NodeId = wfruntime.nextNode.id,
                NodeName = wfruntime.nextNode.name,
                NextNodeId = nnnode == null ? "" : nnnode.id,
                NextNodeName = nnnode == null ? "" : nnnode.name,
                BeginTime = currNodeHistory.EndTime,
                OperateList = JsonHelper.Instance.Serialize(wfruntime.nextNode.setInfo)
            };
            UnitWork.Add(log);

            UnitWork.Update(flowInstance);
            if (string.IsNullOrEmpty(currNodeHistory.Id)) {
                currNodeHistory.Id = Guid.NewGuid().ToString();
                UnitWork.Add(currNodeHistory);
            } 
            else
                UnitWork.Update(currNodeHistory);
            UnitWork.Add(NextNodeHistoryEntity);
            UnitWork.Save();

            wfruntime.NotifyThirdParty(_httpClientFactory.CreateClient(), tag);
            return true;
        }


        /// <summary>
        /// 驳回
        /// </summary>
        /// <returns></returns>
        public bool NodeReject(VerificationReq reqest)
        {
            var user = _auth.GetCurrentUser().User;

            FlowInstance flowInstance = Get(reqest.FlowInstanceId);

            FlowRuntime wfruntime = new FlowRuntime(flowInstance);

            string resnode = "";
            //获取驳回节点
            if (reqest.VerificationFinally == Convert.ToInt32(TagState.RejectTop).ToString())
                resnode = wfruntime.RejectNode(reqest.VerificationFinally);
            else
                resnode = string.IsNullOrEmpty(reqest.NodeRejectStep) ? wfruntime.RejectNode() : reqest.NodeRejectStep;

            var tag = new Tag
            {
                Description = reqest.VerificationOpinion,
                Taged = (int) TagState.Reject,
                UserId = user.Id,
                UserName = user.Name
            };

            wfruntime.MakeTagNode(wfruntime.currentNodeId, tag);
            flowInstance.IsFinish = 4;//4表示驳回（需要申请者重新提交表单）
            if (resnode != "")
            {
                flowInstance.PreviousId = flowInstance.ActivityId;
                flowInstance.ActivityId = resnode;
                flowInstance.ActivityType = wfruntime.GetNodeType(resnode);
                flowInstance.ActivityName = wfruntime.Nodes[resnode].name;
                flowInstance.MakerList = GetNodeMakers(wfruntime.Nodes[resnode]);//当前节点可执行的人信息

                AddTransHistory(wfruntime);
            }

            UnitWork.Update(flowInstance);

            UnitWork.Add(new FlowInstanceOperationHistory
            {
                InstanceId = reqest.FlowInstanceId
                ,
                CreateUserId = user.Id
                ,
                CreateUserName = user.Name
                ,
                CreateDate = DateTime.Now
                ,
                Content = "【"
                          + wfruntime.currentNode.name
                          + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "】驳回,备注："
                          + reqest.VerificationOpinion
            });

            UnitWork.Save();

            wfruntime.NotifyThirdParty(_httpClientFactory.CreateClient(), tag);

            return true;
        }

        /// <summary>
        /// 驳回
        /// </summary>
        /// <returns></returns>
        public bool NodeReject2(VerificationReq reqest)
        {
            dynamic user = JsonHelper.Instance.Deserialize<object>(_auth.GetLoginUser());

            FlowInstance flowInstance = Get(reqest.FlowInstanceId);
            FlowInstanceLog log = UnitWork.Find<FlowInstanceLog>(1, 1, "CreateDate descending",
                o => o.FlowinstanceId == flowInstance.Id&&o.ActivityId== flowInstance.PreviousId).FirstOrDefault();

            FlowRuntime wfruntime = new FlowRuntime(flowInstance);

            string resnode = "";
            //获取驳回节点
            if (reqest.VerificationFinally == Convert.ToInt32(TagState.RejectTop).ToString()
                || reqest.VerificationFinally == Convert.ToInt32(TagState.RejectEdit).ToString())//驳回起始节点\驳回编辑
                resnode = wfruntime.RejectNode(reqest.VerificationFinally);
            else
                resnode = string.IsNullOrEmpty(reqest.NodeRejectStep) ? wfruntime.RejectNode() : reqest.NodeRejectStep;

            var tag = new Tag
            {
                Description = reqest.VerificationOpinion,
                Taged =Convert.ToInt32(reqest.VerificationFinally),//(int)TagState.Reject,
                UserId = user.employee_id,
                UserName = user.employee_name,
                UserJob = user.job_name,
            };

            wfruntime.MakeTagNode(wfruntime.currentNodeId, tag);
            flowInstance.IsFinish = Convert.ToInt32(reqest.VerificationFinally);//4表示驳回（需要申请者重新提交表单）、5退回编辑
            //添加审批记录
            FlowInstanceLog Flog = new FlowInstanceLog()
            {
                FlowinstanceId = flowInstance.Id,
                UninId = flowInstance.UninId,
                ActivityId = flowInstance.ActivityId,
                ActivityName = flowInstance.ActivityName,
                ActivityType = flowInstance.ActivityType,
                PreviousId = flowInstance.PreviousId,
                CreateDate = DateTime.Now,
                CreateUserId = tag.UserId,
                CreateUserName = tag.UserName,
                CreateCompanyId = user.organ_id,
                CreateCompanyName = user.organ_name,
                CreateDeptId = user.department_id,
                CreateDeptName = user.department_name,
                MakerList = flowInstance.MakerList,
                MakerNameList = ""
            };
            if (resnode != "")
            {
                flowInstance.PreviousId = wfruntime.GetPreviousNodeId(resnode);//flowInstance.ActivityId;//上一节应该是指，当前节点流程逻辑上的上一节点，而不是驳回时所在节点
                flowInstance.ActivityId = resnode;
                flowInstance.ActivityType = wfruntime.GetNodeType(resnode);
                flowInstance.ActivityName = wfruntime.Nodes[resnode].name;
                FlowNode u = wfruntime.Nodes[resnode];
                flowInstance.MakerList = log != null ? log.MakerList : GetNodeMakers2(u, flowInstance.CreateUserId, Convert.ToString(user.organ_id));//当前节点可执行的人信息

                AddTransHistory2(wfruntime);
            }

            UnitWork.Update(flowInstance);
            //获取当前节点审批记录
            FlowInstanceOperationHistory currNodeHistory = UnitWork.Find<FlowInstanceOperationHistory>(1, 1, "CreateDate descending", o =>
               o.NodeId == wfruntime.currentNodeId && o.InstanceId == flowInstance.Id).FirstOrDefault();
            //设置审批记录
            if (currNodeHistory == null)
            {
                currNodeHistory = new FlowInstanceOperationHistory()
                {
                    Id = string.Empty,
                    Result = tag.Taged.ToString(),
                    CreateUserId = tag.UserId,
                    CreateUserName = tag.UserName,
                    CreateDate = DateTime.Now,
                    Opinion = tag.Description,
                    CreateUserJob = tag.UserJob,
                    NodeId = wfruntime.currentNode.id,
                    NodeName = wfruntime.currentNode.name,
                    BeginTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    NextNodeId= resnode,
                    NextNodeName= wfruntime.Nodes[resnode].name
                };
            }
            else
            {
                currNodeHistory.Result = tag.Taged.ToString();
                currNodeHistory.CreateUserId = tag.UserId;
                currNodeHistory.CreateUserName = tag.UserName;
                currNodeHistory.CreateDate = DateTime.Now;
                currNodeHistory.Opinion = tag.Description;
                currNodeHistory.CreateUserJob = tag.UserJob;
                currNodeHistory.NodeId = wfruntime.currentNode.id;
                currNodeHistory.NodeName = wfruntime.currentNode.name;
                currNodeHistory.NextNodeId = resnode;
                currNodeHistory.NextNodeName = wfruntime.Nodes[resnode].name;
                currNodeHistory.EndTime = DateTime.Now;
                currNodeHistory.Content = "【" + (tag.Taged == 1 ? "批准" : "驳回") + "】【" +
                                            tag.UserName + "-" + tag.UserJob + "】" +
                                            ",审批意见：" + tag.Description;
            }
            FlowNode nnnode = wfruntime.GetNextNode(resnode);
            //设置审批记录
            //设置审批记录、下一节点
            FlowInstanceOperationHistory NextNodeHistoryEntity = new FlowInstanceOperationHistory
            {
                InstanceId = flowInstance.Id,
                CreateDate = DateTime.Now,
                Opinion = "",
                NodeId = resnode,
                NodeName = wfruntime.Nodes[resnode].name,
                BeginTime = currNodeHistory.EndTime,
                NextNodeId = nnnode == null ? "" : nnnode.id,
                NextNodeName = nnnode == null ? "" : nnnode.name,
                OperateList = JsonHelper.Instance.Serialize(wfruntime.Nodes[resnode].setInfo)
            };
            if (string.IsNullOrEmpty(currNodeHistory.Id))
            {
                currNodeHistory.Id = Guid.NewGuid().ToString();
                UnitWork.Add(currNodeHistory);
            }
            else
                UnitWork.Update(currNodeHistory);
            if(flowInstance.ActivityType!=3) UnitWork.Add(NextNodeHistoryEntity);
            

            UnitWork.Save();

            wfruntime.NotifyThirdParty(_httpClientFactory.CreateClient(), tag);

            return true;
        }

        #endregion 流程处理API

        /// <summary>
        /// 寻找下一步的执行人
        /// 一般用于本节点审核完成后，修改流程实例的当前执行人，可以做到通知等功能
        /// </summary>
        /// <returns></returns>
        private string GetNextMakers(FlowRuntime wfruntime)
        {
            string makerList = "";
            if (wfruntime.nextNodeId == "-1")
            {
                throw (new Exception("无法寻找到下一个节点"));
            }
            if (wfruntime.nextNodeType == 0)//如果是会签节点
            {
                List<string> _nodelist = wfruntime.FromNodeLines[wfruntime.nextNodeId].Select(u =>u.to).ToList();
                string makers = "";
                foreach (string item in _nodelist)
                {
                    makers = GetNodeMakers(wfruntime.Nodes[item]);
                    if (makers == "")
                    {
                        throw (new Exception("无法寻找到会签节点的审核者,请查看流程设计是否有问题!"));
                    }
                    if (makers == "1")
                    {
                        throw (new Exception("会签节点的审核者不能为所有人,请查看流程设计是否有问题!"));
                    }
                    if (makerList != "")
                    {
                        makerList += ",";
                    }
                    makerList += makers;
                }
            }
            else
            {
                makerList = GetNodeMakers(wfruntime.nextNode);
                if (string.IsNullOrEmpty(makerList))
                {
                    throw (new Exception("无法寻找到节点的审核者,请查看流程设计是否有问题!"));
                }
            }

            return makerList;
        }

        /// <summary>
        /// 寻找下一步的执行人
        /// 一般用于本节点审核完成后，修改流程实例的当前执行人，可以做到通知等功能
        /// </summary>
        /// <returns></returns>
        private string GetNextMakers2(FlowRuntime wfruntime,string createuser_id, string organ_id)
        {
            string makerList = "";
            if (wfruntime.nextNodeId == "-1")
            {
                throw (new Exception("无法寻找到下一个节点"));
            }
            if (wfruntime.nextNodeType == 0)//如果是会签节点
            {
                List<string> _nodelist = wfruntime.FromNodeLines[wfruntime.nextNodeId].Select(u => u.to).ToList();
                string makers = "";
                foreach (string item in _nodelist)
                {
                    makers = GetNodeMakers2(wfruntime.Nodes[item], createuser_id, organ_id);
                    if (makers == "")
                    {
                        throw (new Exception("无法寻找到会签节点的审核者,请查看流程设计是否有问题!"));
                    }
                    if (makers == "1")
                    {
                        throw (new Exception("会签节点的审核者不能为所有人,请查看流程设计是否有问题!"));
                    }
                    if (makerList != "")
                    {
                        makerList += ",";
                    }
                    makerList += makers;
                }
            }
            else
            {
                makerList = GetNodeMakers2(wfruntime.nextNode, createuser_id, organ_id);
                if (string.IsNullOrEmpty(makerList))
                {
                    throw (new Exception("无法寻找到节点的审核者,请查看流程设计是否有问题!"));
                }
            }

            return makerList;
        }

        /// <summary>
        /// 寻找该节点执行人
        /// </summary>
        /// <param name="node"></param>
        /// <param name="FlowCreaterId">流程创建人</param>
        /// <returns></returns>
        public string GetNodeMakers2(FlowNode node,string FlowCreaterId,string organ_id)
        {
            string makerList = "";

            if (node.setInfo != null)
            {
                if (node.setInfo.NodeDesignate == Setinfo.ALL_USER)//所有成员
                {
                    makerList = "1";
                }
                else if (node.setInfo.NodeDesignate == Setinfo.SPECIAL_USER)//指定成员
                {
                    makerList = GenericHelpers.ArrayToString(node.setInfo.NodeDesignateData.users, makerList);
                }
                else if (node.setInfo.NodeDesignate == Setinfo.SPECIAL_ROLE)  //指定角色
                {
                    //获取角色用户
                    //dynamic FlowCreater = JsonHelper.Instance.Deserialize<object>(_auth.GetUserById(FlowCreaterId));//流程创建用户
                    System.Collections.Generic.Dictionary<string, string> _head = new System.Collections.Generic.Dictionary<string, string>();
                    _head.Add("access_token", _auth.GetLoginToken());
                    var tag = _auth.GetLoginUser();//用户信息
                    foreach (string a in node.setInfo.NodeDesignateData.roles)
                    {
                        string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Employee/GetEmployeeByRoleId?roleId=" + a, header: _head, contentType: "application/json");
                        dynamic result = JsonHelper.Instance.Deserialize<object>(value);
                        if (result.code == 0)
                        {
                            foreach (var item in result.data)
                            {
                                if (item.organ_id == organ_id)
                                {
                                    makerList += makerList == "" ? item.id : "," + item.id;
                                }
                                else if (string.IsNullOrEmpty(Convert.ToString(item.organ_id)) && tag.IndexOf(Convert.ToString(item.department_id)) > -1)
                                {
                                    makerList += makerList == "" ? item.id : "," + item.id;
                                }
                            }
                        }
                        else
                            throw new Exception(result.msg);
                    }
                    //string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Role/GetRoles", header: _head, contentType: "application/json");
                    //dynamic result = JsonHelper.Instance.Deserialize<object>(value);
                    //string rArr = "";
                    //foreach (string a in node.setInfo.NodeDesignateData.roles) {
                    //    rArr+= rArr == "" ? a : "," + a;
                    //}
                    //string _names="";
                    //if (result.code == 0)
                    //{
                    //    foreach (var item in result.data)
                    //    {
                    //        if (rArr.IndexOf(Convert.ToString(item.id)) > -1)
                    //        {
                    //            _names += _names == "" ? item.name : "," + item.name;
                    //        }
                    //    }
                    //    string epls = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Employee/GetDepartmentEmployee?departmentId=" + organ_id + "&status=0&subcollection=true", header: _head, contentType: "application/json");
                    //    dynamic relt = JsonHelper.Instance.Deserialize<object>(epls);
                    //    foreach (var v in relt.data)
                    //    {
                    //        if (_names.IndexOf(Convert.ToString(v.job_name)) > -1)
                    //        {
                    //            makerList += makerList == "" ? v.id : "," + v.id;
                    //        }
                    //    }
                    //}
                    //else
                    //    throw new Exception(result.msg);
                }
                else if (node.setInfo.NodeDesignate == Setinfo.SPECIAL_CREATER) {
                    makerList = FlowCreaterId;
                }
            }
            else if (node.type == FlowNode.START || node.type == FlowNode.END) {
                makerList = FlowCreaterId;
            }
            else  //如果没有设置节点信息，默认所有人都可以审核
            {
                makerList = "1";
            }
            return makerList;
        }


        /// <summary>
        /// 寻找该节点执行人
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetNodeMakers(FlowNode node)
        {
            string makerList = "";

            if (node.setInfo != null)
            {
                if (node.setInfo.NodeDesignate == Setinfo.ALL_USER)//所有成员
                {
                    makerList = "1";
                }
                else if (node.setInfo.NodeDesignate == Setinfo.SPECIAL_USER)//指定成员
                {
                    makerList = GenericHelpers.ArrayToString(node.setInfo.NodeDesignateData.users, makerList);
                }
                else if (node.setInfo.NodeDesignate == Setinfo.SPECIAL_ROLE)  //指定角色
                {
                    var users = _revelanceApp.Get(Define.USERROLE, false, node.setInfo.NodeDesignateData.roles);
                    makerList = GenericHelpers.ArrayToString(users, makerList);
                }
            }
            else  //如果没有设置节点信息，默认所有人都可以审核
            {
                makerList = "1";
            }
            return makerList;
        }

        /// <summary>
        /// 审核流程(old)
        /// <para></para>
        /// </summary>
        public void Verification(VerificationReq request)
        {
            var user = _auth.GetCurrentUser().User;
            var tag = new Tag
            {
                UserName = user.Name,
                UserId = user.Id,
                Description = request.VerificationOpinion,
                Taged = Int32.Parse(request.VerificationFinally)
            };
            //驳回
            if (request.VerificationFinally ==Convert.ToInt32(TagState.Reject).ToString()|| request.VerificationFinally == Convert.ToInt32(TagState.RejectTop).ToString())
            {
                NodeReject(request);
            }else
            {
                //节点
                NodeVerification(request, tag);
            }
        }
        /// <summary>
        /// 审核流程(20191212)
        /// <para></para>
        /// </summary>
        public bool Verification2(VerificationReq request)
        {
            dynamic user = JsonHelper.Instance.Deserialize<object>(_auth.GetLoginUser());
            var tag = new Tag
            {
                UserName = user.employee_name,
                UserId = user.employee_id,
                UserJob=user.job_name,
                UserOrganId=user.organ_id,
                Description = request.VerificationOpinion,
                Taged = Int32.Parse(request.VerificationFinally)
            };

            lock (request.FlowInstanceId) {
                FlowInstance flowInstance = Get(request.FlowInstanceId);
                if (!flowInstance.MakerList.Contains(tag.UserId))
                {
                    return false;
                }

                //驳回
                if (request.VerificationFinally == Convert.ToInt32(TagState.Reject).ToString()|| request.VerificationFinally == Convert.ToInt32(TagState.RejectTop).ToString() 
                    || request.VerificationFinally == Convert.ToInt32(TagState.RejectEdit).ToString())
                {
                    return NodeReject2(request);
                }
                else
                {
                    //节点
                    return NodeVerification2(request, tag);
                }
            }
        }

        public void Update(FlowInstance flowScheme)
        {
            Repository.Update(flowScheme);
        }

        public TableData Load(QueryFlowInstanceListReq request)
        {
            var result = new TableData();
            var user = _auth.GetCurrentUser();

            if (request.type == "wait")   //待办事项
            {
                result.count = UnitWork.Find<FlowInstance>(u => u.MakerList == "1" || u.MakerList.Contains(user.User.Id)).Count();

                result.data = UnitWork.Find<FlowInstance>(request.page, request.limit, "CreateDate descending",
                    u => u.MakerList == "1" || u.MakerList.Contains(user.User.Id)).ToList();
            }
            else if (request.type == "disposed")  //已办事项（即我参与过的流程）
            {
                var instances = UnitWork.Find<FlowInstanceTransitionHistory>(u => u.CreateUserId == user.User.Id)
                    .Select(u => u.InstanceId).Distinct();
                var query = from ti in instances
                            join ct in UnitWork.Find<FlowInstance>(null) on ti equals ct.Id
                            select ct;

                result.data = query.OrderByDescending(u => u.CreateDate)
                    .Skip((request.page - 1) * request.limit)
                    .Take(request.limit).ToList();
                result.count = instances.Count();
            }
            else  //我的流程
            {
                result.count = UnitWork.Find<FlowInstance>(u => u.CreateUserId == user.User.Id).Count();
                result.data = UnitWork.Find<FlowInstance>(request.page, request.limit,
                    "CreateDate descending", u => u.CreateUserId == user.User.Id).ToList();
            }

            return result;
        }

        public TableData GetList(QueryFlowInstanceListReq request)
        {
            var result = new TableData();
            dynamic user = JsonHelper.Instance.Deserialize<object>(_auth.GetLoginUser());
            string u_id =!string.IsNullOrEmpty(Convert.ToString(user.employee_id)) ? Convert.ToString(user.employee_id)  : "";
            string userid = !string.IsNullOrEmpty(Convert.ToString(user.id)) ? Convert.ToString(user.id) : "";
            if (request.type == "wait")   //待办事项
            {
                result.count = UnitWork.Find<FlowInstance>(u => !string.IsNullOrEmpty(u.UninId) && (u.MakerList == "1" || u.MakerList.Contains(u_id))).Count();

                result.data = UnitWork.Find<FlowInstance>(request.page, request.limit, "CreateDate descending",
                    u => (u.MakerList == "1" || u.MakerList.Contains(u_id)) && !string.IsNullOrEmpty(u.UninId)
                    && (!string.IsNullOrEmpty(request.FlowInstanceType) ? u.SchemeType == request.FlowInstanceType : true)).ToList();
            }
            else if (request.type == "disposed")  //已办事项（即我参与过的流程）
            {
                var instances = UnitWork.Find<FlowInstanceTransitionHistory>(u =>  u.CreateUserId == userid|| u.CreateUserId == u_id)
                    .Select(u => u.InstanceId).Distinct();
                var query = from ti in instances
                            join ct in UnitWork.Find<FlowInstance>(u => !string.IsNullOrEmpty(request.FlowInstanceType) ? u.SchemeType == request.FlowInstanceType : true) on ti equals ct.Id
                            select ct;

                result.data = query.OrderByDescending(u => u.CreateDate)
                    .Skip((request.page - 1) * request.limit)
                    .Take(request.limit).ToList();
                result.count = instances.Count();
            }
            else  //我的流程
            {
                result.count = UnitWork.Find<FlowInstance>(u => u.CreateUserId == u_id && !string.IsNullOrEmpty(u.UninId)).Count();
                result.data = UnitWork.Find<FlowInstance>(request.page, request.limit,
                    "CreateDate descending", u => u.CreateUserId == u_id && !string.IsNullOrEmpty(u.UninId) &&
                    (!string.IsNullOrEmpty(request.FlowInstanceType) ? u.SchemeType == request.FlowInstanceType : true)).ToList();
            }

            return result;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public TableData GetList_Sim(QueryFlowInstanceListReq request)
        {
            var result = new TableData();
            dynamic user = JsonHelper.Instance.Deserialize<object>(_auth.GetLoginUser());
            string u_id = !string.IsNullOrEmpty(Convert.ToString(user.employee_id)) ? Convert.ToString(user.employee_id) : "";
            string userid = !string.IsNullOrEmpty(Convert.ToString(user.id)) ? Convert.ToString(user.id) : "";
            string organ_id = !string.IsNullOrEmpty(Convert.ToString(user.organ_id)) ? Convert.ToString(user.organ_id) : "";

            if (request.type == "wait")   //待办事项
            {
                JArray jArray = new JArray();
                result.count = UnitWork.Find<FlowInstance>(u => !string.IsNullOrEmpty(u.UninId) && (u.MakerList == "1" || u.MakerList.Contains(u_id))).Count();
                List<FlowInstance> FList = UnitWork.Find<FlowInstance>(request.page, request.limit, "CreateDate descending",
                    u => u.CreateCompanyId == organ_id && !string.IsNullOrEmpty(u.UninId) && (u.MakerList == "1" || u.MakerList.Contains(u_id))
                    && (!string.IsNullOrEmpty(request.FlowInstanceType) ? request.FlowInstanceType.Contains(u.SchemeType) : true)).ToList();
                foreach (FlowInstance flow in FList)
                {
                    JToken obj = new JObject();
                    obj["Id"] = flow.Id;
                    obj["MakerList"] = flow.MakerList;
                    obj["ActivityType"] = flow.ActivityType;
                    obj["IsFinish"] = flow.IsFinish;
                    obj["UninId"] = flow.UninId;
                    jArray.Add(obj);
                }

                result.data = jArray;
            }
            else if (request.type == "disposed")  //已办事项（即我参与过的流程）
            {
                var instances = UnitWork.Find<FlowInstanceTransitionHistory>(u => u.CreateUserId == userid || u.CreateUserId == u_id)
                    .Select(u => u.InstanceId).Distinct();
                var query = from ti in instances
                            join ct in UnitWork.Find<FlowInstance>(u=>!string.IsNullOrEmpty(request.FlowInstanceType) ? request.FlowInstanceType.Contains(u.SchemeType) : true) on ti equals ct.Id
                            select ct;

                result.data = query.OrderByDescending(u => u.CreateDate)
                    .Skip((request.page - 1) * request.limit)
                    .Take(request.limit).ToList();
                result.count = instances.Count();
            }
            else  //我的流程
            {
                result.count = UnitWork.Find<FlowInstance>(u => u.CreateUserId == u_id && !string.IsNullOrEmpty(u.UninId)).Count();
                result.data = UnitWork.Find<FlowInstance>(request.page, request.limit,
                    "CreateDate descending", u => u.CreateUserId == u_id && !string.IsNullOrEmpty(u.UninId) &&
                    (!string.IsNullOrEmpty(request.FlowInstanceType) ? request.FlowInstanceType.Contains(u.SchemeType) : true)).ToList();
            }

            return result;
        }

        private void AddTransHistory2(FlowRuntime wfruntime)
        {
            var tag = _auth.GetLoginUser();
            dynamic user = JsonHelper.Instance.Deserialize<object>(tag);
            UnitWork.Add(new FlowInstanceTransitionHistory
            {
                InstanceId = wfruntime.flowInstanceId,
                CreateUserId = user.employee_id,
                CreateUserName = user.employee_name,
                FromNodeId = wfruntime.currentNodeId,
                FromNodeName = wfruntime.currentNode.name,
                FromNodeType = wfruntime.currentNodeType,
                ToNodeId = wfruntime.nextNodeId,
                ToNodeName = wfruntime.nextNode.name,
                ToNodeType = wfruntime.nextNodeType,
                IsFinish = wfruntime.nextNodeType == 4 ? 1 : 0,
                TransitionSate = 0
            });
        }

        /// <summary>
        /// 添加扭转记录
        /// </summary>
        private void AddTransHistory(FlowRuntime wfruntime)
        {
            var tag = _auth.GetCurrentUser().User;
            UnitWork.Add(new FlowInstanceTransitionHistory
            {
                InstanceId = wfruntime.flowInstanceId,
                CreateUserId = tag.Id,
                CreateUserName = tag.Name,
                FromNodeId = wfruntime.currentNodeId,
                FromNodeName = wfruntime.currentNode.name,
                FromNodeType = wfruntime.currentNodeType,
                ToNodeId = wfruntime.nextNodeId,
                ToNodeName = wfruntime.nextNode.name,
                ToNodeType = wfruntime.nextNodeType,
                IsFinish = wfruntime.nextNodeType == 4 ? 1 : 0,
                TransitionSate = 0
            });
        }

        public FlowInstanceApp(IUnitWork unitWork, IRepository<FlowInstance> repository
        , IAuth auth, RevelanceManagerApp app, FlowSchemeApp flowSchemeApp, FormApp formApp, IHttpClientFactory httpClientFactory) : base(unitWork, repository)
        {
            _auth = auth;
            _revelanceApp = app;
            _flowSchemeApp = flowSchemeApp;
            _formApp = formApp;
            _httpClientFactory = httpClientFactory;
        }

        public List<FlowInstanceOperationHistory> QueryHistories(QueryFlowInstanceHistoryReq request)
        {
            List<FlowInstanceOperationHistory> list= UnitWork.Find<FlowInstanceOperationHistory>(u => u.InstanceId == request.FlowInstanceId)
                .OrderBy(u=>u.EndTime).ToList();
            //处理初始节点乱序
            FlowInstanceOperationHistory temp = list.FirstOrDefault(o => string.IsNullOrEmpty(o.CreateUserId));
            if (temp != null) {
                list.Remove(temp);
                list.Add(temp);
            }
            return list;
        }

    }
}