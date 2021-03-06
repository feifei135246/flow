// ***********************************************************************
// Assembly         : OpenAuth.App
// Author           : 李玉宝
// Created          : 07-19-2018
//
// Last Modified By : 李玉宝
// Last Modified On : 07-19-2018
// ***********************************************************************
// <copyright file="FlowRuntime.cs" company="OpenAuth.App">
//     Copyright (c) http://www.openauth.me. All rights reserved.
// </copyright>
// <summary>
// 一个正在运行中的流程实例
//</summary>
// ***********************************************************************

using Infrastructure;
using Newtonsoft.Json.Linq;
using OpenAuth.Repository.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Castle.Core.Internal;

namespace OpenAuth.App.Flow
{
    public class FlowRuntime
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public FlowRuntime(FlowInstance instance)
        {
            dynamic schemeContentJson = instance.SchemeContent.ToJson();//获取工作流模板内容的json对象;

            InitLines(schemeContentJson);
            InitNodes(schemeContentJson);

            currentNodeId = (instance.ActivityId == "" ? startNodeId : instance.ActivityId);
            currentNodeType = GetNodeType(currentNodeId);
            FrmData = instance.FrmData;
            title = schemeContentJson.title;
            initNum = schemeContentJson.initNum;
            previousId = instance.PreviousId;
            flowInstanceId = instance.Id;

            //会签开始节点和流程结束节点没有下一步
            if (currentNodeType == 0 || currentNodeType == 4)
            {
                nextNodeId = "-1";
                nextNodeType = -1;
            }
            else
            {
                nextNodeId = GetNextNodeId();//下一个节点
                nextNodeType = GetNodeType(nextNodeId);
            }
        }

        #region 私有方法

        /// <summary>
        /// 获取工作流节点的字典列表:key节点id
        /// </summary>
        /// <param name="schemeContentJson"></param>
        /// <returns></returns>
        private void InitNodes(dynamic schemeContentJson)
        {
            Nodes = new Dictionary<string, FlowNode>();
            foreach (JObject item in schemeContentJson.nodes)
            {
                var node = item.ToObject<FlowNode>();
                if (!Nodes.ContainsKey(node.id))
                {
                    Nodes.Add(node.id, node);
                }
                if (node.type == FlowNode.START)
                {
                    this.startNodeId = node.id;
                }
            }
        }

        private void InitLines(dynamic schemeContentJson)
        {
            Lines = new List<FlowLine>();
            FromNodeLines = new Dictionary<string, List<FlowLine>>();
            ToNodeLines = new Dictionary<string, List<FlowLine>>();
            foreach (JObject item in schemeContentJson.lines)
            {
                var line = item.ToObject<FlowLine>();
                Lines.Add(line);

                if (!FromNodeLines.ContainsKey(line.from))
                {
                    List<FlowLine> d = new List<FlowLine> { line };
                    FromNodeLines.Add(line.from, d);
                }
                else
                {
                    FromNodeLines[line.from].Add(line);
                }

                if (!ToNodeLines.ContainsKey(line.to))
                {
                    List<FlowLine> d = new List<FlowLine> { line };
                    ToNodeLines.Add(line.to, d);
                }
                else
                {
                    ToNodeLines[line.to].Add(line);
                }
            }
        }

        /// <summary>
        /// 获取下一个节点
        /// </summary>
        private string GetNextNodeId(string nodeId = null)
        {
            var lines = nodeId == null ? FromNodeLines[currentNodeId] : FromNodeLines[nodeId];
            string NextNodeId = "";
            if (lines.Count == 0)
            {
                throw new Exception("无法寻找到下一个节点");
            }
            ///是否有条件
            if (string.IsNullOrEmpty(FrmData))
            {

                NextNodeId = lines[0].to;
            }
            else {
                FrmData = FrmData.ToLower();//统一转小写
                var frmDataJson = FrmData.ToJObject();//获取数据内容
                foreach (var l in lines)
                {
                    if (!(l.Compares.IsNullOrEmpty()) && l.Compare(frmDataJson))
                    {
                        NextNodeId = l.to;
                    }
                }
            }

            //节点是否允许跳过
            if (!string.IsNullOrEmpty(NextNodeId)&& Nodes[NextNodeId].setInfo!=null&& Nodes[NextNodeId].setInfo.NodeDesignate == Setinfo.SPECIAL_CREATER && Nodes[NextNodeId].setInfo.NodeSkip == 0)
            {
                var lines2 = FromNodeLines[lines[0].to];
                if (lines2.Count == 0)
                {
                    throw new Exception("无法寻找到下一个节点");
                }
                NextNodeId = lines2[0].to;
            }
            return NextNodeId;
        }

        /// <summary>
        /// 获取上一个节点
        /// </summary>
        public string GetPreviousNodeId(string nodeId = null)
        {
            if (!ToNodeLines.Keys.Contains(nodeId)) return nodeId == null ? currentNodeId : nodeId;
            var lines = nodeId == null ? ToNodeLines[currentNodeId] : ToNodeLines[nodeId];
            if (lines.Count == 0)
            {
                throw new Exception("无法寻找到下一个节点");
            }

            if (string.IsNullOrEmpty(FrmData)) return lines[0].from;

            FrmData = FrmData.ToLower();//统一转小写
            var frmDataJson = FrmData.ToJObject();//获取数据内容

            foreach (var l in lines)
            {
                if (!(l.Compares.IsNullOrEmpty()) && l.Compare(frmDataJson))
                {
                    return l.from;
                }
            }

            return lines[0].from;
        }


        #endregion 私有方法

        #region 共有方法

        /// <summary>
        /// 获取实例接下来运行的状态
        /// </summary>
        /// <returns>-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束</returns>
        public int GetNextNodeType()
        {
            if (nextNodeId != "-1")
            {
                return GetNodeType(nextNodeId);
            }
            return -1;
        }

        /// <summary>
        /// 获取实例接下来运行的状态
        /// </summary>
        /// <returns>-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束</returns>
        public FlowNode GetNextNode(string nodeId)
        {
            try {
                int _t = GetNodeType(nodeId);
                if (_t == 4)
                {
                    return null;
                }
                string nextNodeId = GetNextNodeId(nodeId);
                return Nodes[nextNodeId];
            }
            catch (Exception ex) {
                return null;
            }
        }


        /// <summary>
        /// 获取节点类型 0会签开始,1会签结束,2一般节点,开始节点,4流程运行结束
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public int GetNodeType(string nodeId)
        {
            switch (Nodes[nodeId].type)
            {
                //会签开始节点
                case FlowNode.FORK:
                    return 0;
                //会签结束节点
                case FlowNode.JOIN:
                    return 1;
                //结束节点
                case FlowNode.END:
                    return 4;
                //开始节点
                case FlowNode.START:
                    return 3;

                default:
                    return 2;
            }
        }

        /// <summary>
        /// 节点会签审核
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="tag"></param>
        /// <returns>-1不通过,1等待,其它通过</returns>
        public string NodeConfluence(string nodeId, Tag tag)
        {

            var forkToThisLine = ToNodeLines[nodeId].FirstOrDefault();
            if (forkToThisLine == null)
            {
                throw (new Exception("该会签节点没有来源，请检查流程结构"));
            }

            var forkNode = Nodes[forkToThisLine.from];  //会签开始节点
            string joinNodeId = GetNextNodeId(nodeId); //获取会签的合流节点

            int allnum = FromNodeLines[forkToThisLine.from].Count;   //总会签数量
            string res =string.Empty;  //记录会签的结果,默认正在会签
            if (forkNode.setInfo.NodeConfluenceType == "one") //有一个步骤通过即可
            {
                if (tag.Taged == (int) TagState.Ok)
                {
                    res = GetNextNodeId(joinNodeId);
                }
                else if(tag.Taged ==(int) TagState.No)
                {
                    if (forkNode.setInfo.ConfluenceNo == null)
                    {
                        forkNode.setInfo.ConfluenceNo = 1;
                    }
                    else if (forkNode.setInfo.ConfluenceNo == (allnum - 1))
                    {
                        res = TagState.No.ToString("D");
                    }
                    else
                    {
                        forkNode.setInfo.ConfluenceNo++;
                    }
                }
            }
            else //默认所有步骤通过
            {
                if (tag.Taged == (int) TagState.No)  //只要有一个不同意，那么流程就结束
                {
                    res = TagState.No.ToString("D");
                }
                else if(tag.Taged == (int)TagState.Ok)
                {
                    if (forkNode.setInfo.ConfluenceOk == null)
                    {
                        forkNode.setInfo.ConfluenceOk = 1;
                    }
                    else if (forkNode.setInfo.ConfluenceOk == (allnum - 1))  //会签成功
                    {
                        res = GetNextNodeId(joinNodeId);
                    }
                    else
                    {
                        forkNode.setInfo.ConfluenceOk++;
                    }
                }
            }

            if (res == TagState.No.ToString("D"))
            {
                tag.Taged = (int) TagState.No;
                MakeTagNode(joinNodeId, tag);
            }
            else if (!string.IsNullOrEmpty(res)) //会签结束，标记合流节点
            {
                tag.Taged = (int) TagState.Ok;
                MakeTagNode(joinNodeId, tag);
                nextNodeId = res;
                nextNodeType = GetNodeType(res);
            }
            else
            {
                nextNodeId = joinNodeId;
                nextNodeType = GetNodeType(joinNodeId);
            }
            return res;
        }

        /// <summary>
        /// 驳回节点0"前一步"1"第一步"2"某一步" 3"不处理"
        /// </summary>20191204
        /// <returns>rType 驳回起点</returns>
        public string RejectNode(string rType=null)
        {
            return RejectNode(currentNodeId, rType);
        }

        public string RejectNode(string nodeId,string rType)
        {
            dynamic node = Nodes[nodeId];
            if (!string.IsNullOrEmpty(rType)) {//设置指定驳回起点
                if (rType == "4")//驳回发起人
                {
                    return startNodeId;
                }
                else if (rType == "5")//驳回编辑
                { 
                    foreach (dynamic _n in Nodes) {
                        if (_n.setInfo.NodeDesignate == Setinfo.SPECIAL_CREATER) {
                            return _n.id;
                        }
                    }
                }
                else
                    return GetNextNodeId(startNodeId);
            }
            
            if (node.setInfo != null)
            {
                if (node.setInfo.NodeRejectType == "0")//驳回上一节点
                {
                    return previousId;
                }
                else if (node.setInfo.NodeRejectType == "1")//驳回第一步
                {
                    return GetNextNodeId(startNodeId);
                }
                else if (node.setInfo.NodeRejectType == "2")//驳回指定步骤
                {
                    return node.setInfo.NodeRejectStep;
                }
                else if (node.setInfo.NodeRejectType == "3")//不处理
                {
                    return "";
                }
                else { //默认打回发起人起点
                    return startNodeId;
                }
            }
            return previousId;
        }

        ///<summary>
        /// 标记节点1通过，-1不通过，0驳回
        /// </summary>
        /// <param name="nodeId"></param>
        public void MakeTagNode(string nodeId, Tag tag)
        {
            foreach (var item in Nodes)
            {
                if (item.Key == nodeId)
                {
                    if (item.Value.setInfo == null)
                    {
                        item.Value.setInfo  = new Setinfo();
                    }
                    item.Value.setInfo.Taged = tag.Taged;
                    item.Value.setInfo.UserId = tag.UserId;
                    item.Value.setInfo.UserName = tag.UserName;
                    item.Value.setInfo.Description = tag.Description;
                    item.Value.setInfo.TagedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    break;
                }
            }
        }

        public object ToSchemeObj()
        {
            return new
            {
                title = this.title,
                initNum = this.initNum,
                lines = Lines,
                nodes = Nodes.Select(u => u.Value),
                areas = new string[0]
            };
        }

        /// <summary>
        /// 通知三方系统，节点执行情况
        /// </summary>
        public void NotifyThirdParty(HttpClient client, Tag tag)
        {
            if (currentNode.setInfo == null || string.IsNullOrEmpty(currentNode.setInfo.ThirdPartyUrl))
            {
                return;
            }

            var postData = new
            {
                flowInstanceId,
                nodeName=currentNode.name,
                nodeId = currentNodeId,
                userId = tag.UserId,
                userName = tag.UserName,
                result=tag.Taged, //1：通过;2：不通过；3驳回
                description = tag.Description,
                execTime = tag.TagedTime,
                isFinish = currentNodeType == 4
            };

            using (HttpContent httpContent = new StringContent(JsonHelper.Instance.Serialize(postData), Encoding.UTF8))
            {
                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                   client.PostAsync(currentNode.setInfo.ThirdPartyUrl, httpContent);
            }
        }

        #endregion 共有方法

        #region 属性

        public string title { get; set; }

        public int initNum { get; set; }

        /// <summary>
        /// 运行实例的Id
        /// </summary>
        public string flowInstanceId { get; set; }

        /// <summary>
        /// 开始节点的ID
        /// </summary>
        public string startNodeId { get; set; }

        /// <summary>
        /// 当前节点的ID
        /// </summary>
        public string currentNodeId { get; set; }

        /// <summary>
        /// 当前节点类型 0会签开始,1会签结束,2一般节点,开始节点,4流程运行结束
        /// </summary>
        public int currentNodeType { get; set; }

        /// <summary>
        /// 当前节点的对象
        /// </summary>
        public FlowNode currentNode => Nodes[currentNodeId];

        /// <summary>
        /// 下一个节点
        /// </summary>
        public string nextNodeId { get; set; }

        /// <summary>
        /// 下一个节点类型 -1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束
        /// </summary>
        /// <value>The type of the next node.</value>
        public int nextNodeType { get; set; }

        /// <summary>
        /// 下一个节点对象
        /// </summary>
        public FlowNode nextNode => Nodes[nextNodeId];

        /// <summary>
        /// 上一个节点
        /// </summary>
        public string previousId { get; set; }
        /// <summary>
        /// 实例节点集合
        /// </summary>
        public Dictionary<string, FlowNode> Nodes { get; set; }

        /// <summary>
        /// 流程实例中所有的线段
        /// </summary>
        public List<FlowLine> Lines { get; set; }

        /// <summary>
        /// 从节点发出的线段集合
        /// </summary>
        public Dictionary<string, List<FlowLine>> FromNodeLines { get; set; }

        /// <summary>
        /// 到达节点的线段集合
        /// </summary>
        public Dictionary<string, List<FlowLine>> ToNodeLines { get; set; }

        /// <summary>
        /// 表单数据
        /// </summary>
        public string FrmData { get; set; }

        #endregion 属性
    }
}