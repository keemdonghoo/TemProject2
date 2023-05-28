﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Domain
{
    [Table("movie_user")]
    public class User
    {
        [Key]
        public long UserUID { get; set; }
        public string Name { get; set; }
        public string PassWord { get; set; }
        public string UserName { get; set; }
        public string PhoneNum { get; set; }
        public string NickName { get; set; }
        [DefaultValue(false)]
        public bool IsAdmin { get; set; }

        public string? Email { get; set; }
    }
}
