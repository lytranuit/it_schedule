namespace it.Areas.Admin.Models
{
    public enum AuditType
    {
        None = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
        Login = 4,
        Logout = 5,
        LoginFailed = 6,

    }
}
