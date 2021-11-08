namespace OpenAuth.App
{
    public static class Define
    {
        public static string USERROLE = "UserRole";       //用户角色关联KEY
        public const string ROLERESOURCE= "RoleResource";  //角色资源关联KEY
        public const string USERORG = "UserOrg";  //用户机构关联KEY
        public const string ROLEELEMENT = "RoleElement"; //角色菜单关联KEY
        public const string ROLEMODULE = "RoleModule";   //角色模块关联KEY
        public const string ROLEDATAPROPERTY = "RoleDataProperty";   //角色数据字段权限

        /// <summary>
        /// 数据库
        /// </summary>
        public const string DBTYPE_SQLSERVER = "SqlServer";    //sql server
        public const string DBTYPE_MYSQL = "MySql";
        public const string DBTYPE_NPGSQL = "NpgSql";


        public const int INVALID_TOKEN = 50014;     //token无效

        public const string TOKEN_NAME = "X-Token";


        public const string SYSTEM_USERNAME = "System";
        public const string SYSTEM_USERPWD = "123456";

        public const string ORDER = "0f38d107-e7c3-464b-a4ac-f3c827a0821c";

        public const string PALTURL = "https://crm.fzzhs.cn:8107"; // https://crm.willjay.net:9100https://crm.willjay.net:9100http://platformapi.willjay.nethttps://crm.willjay.net:9100

    }
}
