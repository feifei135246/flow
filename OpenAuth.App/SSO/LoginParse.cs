/*
 * 登录解析
 * 处理登录逻辑，验证客户段提交的账号密码，保存登录信息
 */
using System;
using Infrastructure.Cache;
using OpenAuth.Repository.Domain;
using OpenAuth.Repository.Interface;
using Microsoft.AspNetCore.Http;
using Infrastructure;

namespace OpenAuth.App.SSO
{
    public class LoginParse
    {

        //这个地方使用IRepository<User> 而不使用UserManagerApp是防止循环依赖
        public IRepository<User> _app;
        private ICacheContext _cacheContext;
        private AppInfoService _appInfoService;
        private Infrastructure.HttpHelper _helper;

        public LoginParse( AppInfoService infoService, ICacheContext cacheContext, IRepository<User> userApp)
        {
            _appInfoService = infoService;
            _cacheContext = cacheContext;
            _app = userApp;
        }

        public  LoginResult Do(PassportLoginRequest model)
        {
            var result = new LoginResult();
            try
            {
                model.Trim();
                //获取应用信息
                var appInfo = _appInfoService.Get(model.AppKey);
                if (appInfo == null)
                {
                    throw  new Exception("应用不存在");
                }
                //获取用户信息
                User userInfo = null;
                if (model.Account == Define.SYSTEM_USERNAME)
                {
                    userInfo = new User
                    {
                        Id = Guid.Empty.ToString(), 
                        Account = Define.SYSTEM_USERNAME,
                        Name ="超级管理员",
                        Password = Define.SYSTEM_USERPWD
                    };
                }
                else
                {
                    userInfo = _app.FindSingle(u =>u.Account == model.Account);
                }
               
                if (userInfo == null)
                {
                    throw new Exception("用户不存在");
                }
                if (userInfo.Password != model.Password)
                {
                    throw new Exception("密码错误");
                }

                var currentSession = new UserAuthSession
                {
                    Account = model.Account,
                    Name = userInfo.Name,
                    Token = Guid.NewGuid().ToString().GetHashCode().ToString("x"),
                    AppKey = model.AppKey,
                    CreateTime = DateTime.Now
               //    , IpAddress = HttpContext.Current.Request.UserHostAddress
                };

                //创建Session
                _cacheContext.Set(currentSession.Token, currentSession, DateTime.Now.AddDays(10));

                result.Code = 200;
                result.ReturnUrl = appInfo.ReturnUrl;
                result.Token = currentSession.Token;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }

            return result;
        }

        //无需appkey
        public LoginResult DoNotKey(PassportLoginRequest model)
        {
            var result = new LoginResult();
            try
            {
                model.Trim();
                //获取用户信息
                User userInfo = null;
                if (model.Account == Define.SYSTEM_USERNAME)
                {
                    userInfo = new User
                    {
                        Id = Guid.Empty.ToString(),
                        Account = Define.SYSTEM_USERNAME,
                        Name = "超级管理员",
                        Password = Define.SYSTEM_USERPWD
                    };
                }
                else
                {//获取用户信息/////20191212
                    //var value = _helper.Post(new { Account = model.Account, Password = model.Password },AppSetting.PALTURL+ "/api/Employee/Detail");
                    //var _rrr = JsonHelper.Instance.Deserialize<LoginResult>(value);
                    userInfo = _app.FindSingle(u => u.Account == model.Account);
                }

                if (userInfo == null)
                {
                    throw new Exception("用户不存在");
                }
                if (userInfo.Password != model.Password)
                {
                    throw new Exception("密码错误");
                }

                var currentSession = new UserAuthSession
                {
                    Account = model.Account,
                    Name = userInfo.Name,
                    Token = Guid.NewGuid().ToString().GetHashCode().ToString("x"),
                    //AppKey = model.AppKey,
                    CreateTime = DateTime.Now
                    //, IpAddress = HttpContext.Current.Request.UserHostAddress
                };

                //创建Session
                _cacheContext.Set(currentSession.Token, currentSession, DateTime.Now.AddDays(10));
                result.Code = 200;
                //result.ReturnUrl = appInfo.ReturnUrl;
                result.Token = currentSession.Token;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }

            return result;
        }


        //无需appkey
        public LoginResult DoByToken(string id, string token)
        {
            var result = new LoginResult();
            try
            {
                //获取用户信息
                //userInfo = _app.FindSingle(u => u.Account == model.Account);
                System.Collections.Generic.Dictionary<string, string> _head = new System.Collections.Generic.Dictionary<string, string>();
                _head.Add("access_token", token);
                string value = Infrastructure.HttpHelper.HttpGet(AppSetting.PALTURL + "/api/Users/UserInfo?userId="+id, header: _head, contentType: "application/json");
                dynamic _rest = JsonHelper.Instance.Deserialize<object>(value);
                dynamic userInfo = _rest.data;

                var currentSession = new UserAuthSession
                {
                    Account = userInfo.login_id,
                    Name = userInfo.name,
                    Token = token,
                    //AppKey = model.AppKey,
                    CreateTime = DateTime.Now
                    //, IpAddress = HttpContext.Current.Request.UserHostAddress
                };

                //创建Session
                _cacheContext.Set(currentSession.Token, currentSession, DateTime.Now.AddDays(10));
                result.Code = 200;
                //result.ReturnUrl = appInfo.ReturnUrl;
                result.Token = currentSession.Token;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }

            return result;
        }

    }
}