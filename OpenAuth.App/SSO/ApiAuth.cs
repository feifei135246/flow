// ***********************************************************************
// Assembly         : OpenAuth.App
// Author           : ����
// Created          : 07-05-2018
//
// Last Modified By : ����
// Last Modified On : 07-05-2018
// ***********************************************************************
// <copyright file="ApiAuth.cs" company="OpenAuth.App">
//     Copyright (c) http://www.openauth.me. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using System;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OpenAuth.App.Interface;

namespace OpenAuth.App.SSO
{
    /// <summary>
    /// ��������վ��¼��֤��
    /// <para>��¼ʱ��</para>
    /// <code>
    ///  var result = IAuth.Login(AppKey, username, password);
    ///  if (result.Success)
    ///       return Redirect("/home/index?Token=" + result.Token);
    /// </code>
    /// </summary>
    public class ApiAuth :IAuth
    {
        private IOptions<AppSetting> _appConfiguration;
        private HttpHelper _helper;
        private IHttpContextAccessor _httpContextAccessor;
        private AuthContextFactory _authContextFactory;

        public ApiAuth(IOptions<AppSetting> appConfiguration
            , IHttpContextAccessor httpContextAccessor
            ,AuthContextFactory authContextFactory
            )
        {
            _appConfiguration = appConfiguration;
            _helper = new HttpHelper(_appConfiguration.Value.SSOPassport);
            _authContextFactory = authContextFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetToken()
        {
            string token = _httpContextAccessor.HttpContext.Request.Query[Define.TOKEN_NAME];
            if (!String.IsNullOrEmpty(token)) return token;

            var cookie = _httpContextAccessor.HttpContext.Request.Cookies[Define.TOKEN_NAME];
            return cookie == null ? String.Empty : cookie;
        }

        public string GetUserById(string userId)
        {
            System.Collections.Generic.Dictionary<string, string> _head = new System.Collections.Generic.Dictionary<string, string>();
            _head.Add("access_token", GetToken());
            string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Users/UserInfo?userId=" + userId, header: _head, contentType: "application/json");
            dynamic result = JsonHelper.Instance.Deserialize<object>(value);
            if (result.code == 0)
            {
                return result.data;
            }
            throw new Exception(result.msg);
        }

        public string GetLoginToken()
        {
            string token = _httpContextAccessor.HttpContext.Request.Query[Define.TOKEN_NAME];
            if (!String.IsNullOrEmpty(token)) return token;

            var cookie = _httpContextAccessor.HttpContext.Request.Cookies[Define.TOKEN_NAME];
            return cookie == null ? String.Empty : cookie;
        }
        /// <summary>
        /// ͨ��WebApi����token�Ƿ���Ч
        /// </summary>
        /// <remarks>http://www.openauth.me</remarks>
        public bool CheckLogin(string token="", string otherInfo = "")
        {
            if (string.IsNullOrEmpty(token))
            {
                token = GetToken();
            }

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
         
            var requestUri = String.Format("/api/Check/GetStatus?token={0}&requestid={1}", token, otherInfo);

            try
            {
                var value = _helper.Get(null, requestUri);
                var result = JsonHelper.Instance.Deserialize<Response<bool>>(value);
                if (result.Code == 200)
                {
                    return result.Result;
                }
                throw new Exception(result.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// ��ȡ��ǰ��¼���û���Ϣ
        /// <para>ͨ��URL�е�Token������Cookie�е�Token</para>
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns>LoginUserVM.</returns>
        public AuthStrategyContext GetCurrentUser()
        {
            string username = GetUserName();
            return _authContextFactory.GetAuthStrategyContext(username);
        }

        public string GetLoginUser() {
            try
            {
                System.Collections.Generic.Dictionary<string, string> _head = new System.Collections.Generic.Dictionary<string, string>();
                _head.Add("access_token", GetToken());
                string val = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Oauth/Verify", header: _head, contentType: "application/json");
                dynamic ret = JsonHelper.Instance.Deserialize<object>(val);
                if (ret.code == 0) {
                    string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Users/UserInfo?userId="+ ret.data.id, header: _head, contentType: "application/json");
                    dynamic result = JsonHelper.Instance.Deserialize<object>(value);
                    if (result.code == 0)
                    {
                        return result.data;
                    }
                    throw new Exception(ret.msg);
                }
                throw new Exception(ret.msg);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// ��ȡWebApi�е�ǰ��¼���û���
        /// <para>ͨ��URL�е�Token������Cookie�е�Token</para>
        /// </summary>
        /// <param name="otherInfo">The account.</param>
        /// <returns>System.String.</returns>
        public string GetUserName(string otherInfo = "")
        {
            var requestUri = String.Format("/api/Check/GetUserName?token={0}&requestid={1}", GetToken(), otherInfo);
            try
            {
                var value = _helper.Get(null, requestUri);
                var result = JsonHelper.Instance.Deserialize<Response<string>>(value);
                if (result.Code == 200)
                {
                    return result.Result;
                }
                throw new Exception(result.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// ͨ��WebApi��¼���û���Ϣ�����webapi��20191212
        /// </summary>
        /// <param name="username">�û���</param>
        /// <param name="pwd">����</param>
        /// <returns>System.String.</returns>
        public LoginResult SetLogin(string id, string token)
        {
            var requestUri = "/api/Oauth/Verify";

            try
            {
                
                return new LoginResult() { Token=token,ReturnUrl= requestUri };

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// ͨ��WebApi��¼���û���Ϣ�����webapi��20191212
        /// </summary>
        /// <param name="username">�û���</param>
        /// <param name="pwd">����</param>
        /// <returns>System.String.</returns>
        public LoginResult Login(string username, string pwd)
        {
            var requestUri = "/api/Check/Login";

            try
            {
                var value = _helper.Post(new { Account = username, Password = pwd }, requestUri);
                var result = JsonHelper.Instance.Deserialize<LoginResult>(value);
                return result;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// ͨ��WebApi��¼���û���Ϣ�����webapi��
        /// </summary>
        /// <param name="appKey">Ӧ�ó���key.</param>
        /// <param name="username">�û���</param>
        /// <param name="pwd">����</param>
        /// <returns>System.String.</returns>
        public LoginResult Login(string appKey, string username, string pwd)
        {
            var requestUri = "/api/Check/Login";

            try
            {
                var value = _helper.Post(new
                {
                    AppKey = appKey,
                    Account = username,
                    Password = pwd
                }, requestUri);

                var result = JsonHelper.Instance.Deserialize<LoginResult>(value);
                return result;
               
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// ע��
        /// </summary>
        public bool Logout()
        {
            var token = GetToken();
            if (String.IsNullOrEmpty(token)) return true;

            var requestUri = String.Format("/api/Check/Logout?token={0}&requestid={1}", token, "");

            try
            {
                var value = _helper.Post(null, requestUri);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}