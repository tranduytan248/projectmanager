using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Ve dang nhap rieng cho API (mobile) — tach khoi Forms Authentication cookie ma web dung,
    /// vi Dio/mobile xu ly cookie phien phuc tap va de vo khi het han. Sinh moi luc dang nhap
    /// (<see cref="Controllers.Api.AuthApiController"/>), doc lai boi
    /// <see cref="Infrastructure.ApiAuthorizeAttribute"/> tren moi request API.
    /// </summary>
    public class ApiToken : IEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Chuoi ngau nhien (Guid), gui kem header "Authorization: Bearer &lt;Token&gt;".</summary>
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
