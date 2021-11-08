using Infrastructure.Cache;
using Microsoft.AspNetCore.Http;
using OpenAuth.App.Interface;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace OpenAuth.App.SSO
{
    /// <summary>
    /// 使用本地登录。这个注入IAuth时，只需要OpenAuth.Mvc一个项目即可，无需webapi的支持
    /// </summary>
    public class LocalAuth : IAuth
    {
        private IHttpContextAccessor _httpContextAccessor;
        private IOptions<AppSetting> _appConfiguration;

        private AuthContextFactory _app;
        private LoginParse _loginParse;
        private ICacheContext _cacheContext;

        public LocalAuth(IHttpContextAccessor httpContextAccessor
            , AuthContextFactory app
            , LoginParse loginParse
            , ICacheContext cacheContext, IOptions<AppSetting> appConfiguration)
        {
            _httpContextAccessor = httpContextAccessor;
            _app = app;
            _loginParse = loginParse;
            _cacheContext = cacheContext;
            _appConfiguration = appConfiguration;
        }

        /// <summary>
        /// 如果是Identity，则返回信息为用户账号
        /// </summary>
        /// <returns></returns>
        private string GetToken()
        {
            if (_appConfiguration.Value.IsIdentityAuth)
            {
                return _httpContextAccessor.HttpContext.User.Identity.Name;
            }
            string token = _httpContextAccessor.HttpContext.Request.Query[Define.TOKEN_NAME];
            if (!String.IsNullOrEmpty(token)) return token;

            token = _httpContextAccessor.HttpContext.Request.Headers[Define.TOKEN_NAME];
            if (!String.IsNullOrEmpty(token)) return token;

            var cookie = _httpContextAccessor.HttpContext.Request.Cookies[Define.TOKEN_NAME];
            return cookie ?? String.Empty;
        }

        public  string Token() {
            return GetToken();
        }

        public bool CheckLogin(string token = "", string otherInfo = "")
        {
            if (_appConfiguration.Value.IsIdentityAuth)
            {
                return (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext.User.Identity.Name));
            }

            if (string.IsNullOrEmpty(token))
            {
                token = GetToken();
            }

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                var result = _cacheContext.Get<UserAuthSession>(token) != null;
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 获取当前登录的用户信息
        /// <para>通过URL中的Token参数或Cookie中的Token</para>
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns>LoginUserVM.</returns>
        public AuthStrategyContext GetCurrentUser()
        {
            if (_appConfiguration.Value.IsIdentityAuth)
            {
                return _app.GetAuthStrategyContext(GetToken());
            }
            AuthStrategyContext context = null;
            var user = _cacheContext.Get<UserAuthSession>(GetToken());
            if (user != null)
            {
                context = _app.GetAuthStrategyContext(user.Account);
            }
            return context;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string GetUserById(string userId)
        {
            System.Collections.Generic.Dictionary<string, string> _head = new System.Collections.Generic.Dictionary<string, string>();
            _head.Add("access_token", GetToken());
            string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Users/UserInfo?userId=" + userId, header: _head, contentType: "application/json");
            dynamic result = Infrastructure.JsonHelper.Instance.Deserialize<object>(value);
            if (result.code == 0)
            {
                return value;
            }
            throw new Exception(result.msg);
        }

        /// <summary>
        /// 获取当前登录的用户信息
        /// <para>通过URL中的Token参数或Cookie中的Token</para>
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns>LoginUserVM.</returns>
        public string GetLoginUser()
        {
            try
            {
                System.Collections.Generic.Dictionary<string, string> _head = new System.Collections.Generic.Dictionary<string, string>();
                var aa = GetToken();
                _head.Add("access_token", GetToken());
                string val = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Oauth/Verify", header: _head, contentType: "application/json");
                dynamic ret = Infrastructure.JsonHelper.Instance.Deserialize<object>(val);
                if (ret.code == 0)
                {
                    //string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Users/UserInfo?userId=" + ret.data.id, header: _head, contentType: "application/json");
                    //dynamic result = Infrastructure.JsonHelper.Instance.Deserialize<object>(value);
                    //if (result.code == 0)
                    //{
                    //    return Convert.ToString(result.data) ;
                    //}
                    return Convert.ToString(ret.data);
                }
                throw new Exception(ret.msg);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 获取当前登录的token
        /// <para>通过URL中的Token参数或Cookie中的Token</para>
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns>LoginUserVM.</returns>
        public string GetLoginToken()
        {
            return GetToken();
        }


        /// <summary>
        /// 获取当前登录的用户名
        /// <para>通过URL中的Token参数或Cookie中的Token</para>
        /// </summary>
        /// <param name="otherInfo">The account.</param>
        /// <returns>System.String.</returns>
        public string GetUserName(string otherInfo = "")
        {
            if (_appConfiguration.Value.IsIdentityAuth)
            {
                return _httpContextAccessor.HttpContext.User.Identity.Name;
            }

            var user = _cacheContext.Get<UserAuthSession>(GetToken());
            if (user != null)
            {
                return user.Account;
            }

            return "";
        }


        /// <summary>
        /// 登录接口20191212
        /// </summary>
        /// <param name="appKey">应用程序key.</param>
        /// <param name="username">用户名</param>
        /// <param name="pwd">密码</param>
        /// <returns>System.String.</returns>
        public LoginResult Login(string username, string pwd)
        {
            //if (_appConfiguration.Value.IsIdentityAuth)
            //{
            //    return new LoginResult
            //    {
            //        Code = 500,
            //        Message = "接口启动了OAuth认证,暂时不能使用该方式登录"
            //    };
            //}
            return _loginParse.DoNotKey(new PassportLoginRequest
            {
                Account = username,
                Password = pwd
            });
        }

        /// <summary>
        /// 登录接口20191212
        /// </summary>
        /// <param name="token">token.</param>
        /// <returns>System.String.</returns>
        public LoginResult SetLogin(string id,string token)
        {
            return _loginParse.DoByToken(id,token);
        }

        /// <summary>
        /// 登录接口
        /// </summary>
        /// <param name="appKey">应用程序key.</param>
        /// <param name="username">用户名</param>
        /// <param name="pwd">密码</param>
        /// <returns>System.String.</returns>
        public LoginResult Login(string appKey, string username, string pwd)
        {
            //if (_appConfiguration.Value.IsIdentityAuth)
            //{
            //    return new LoginResult
            //    {
            //        Code = 500,
            //        Message = "接口启动了OAuth认证,暂时不能使用该方式登录"
            //    };
            //}
            return _loginParse.Do(new PassportLoginRequest
            {
                AppKey = appKey,
                Account = username,
                Password = pwd
            });
        }

        /// <summary>
        /// 注销，如果是Identity登录，需要在controller处理注销逻辑
        /// </summary>
        public bool Logout()
        {
            var token = GetToken();
            if (String.IsNullOrEmpty(token)) return true;

            try
            {
                _cacheContext.Remove(token);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}