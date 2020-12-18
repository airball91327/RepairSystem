using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Models
{
    public class TransDefaultEng
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        [Display(Name = "使用者Id")]
        public int UserId { get; set; }
        [Display(Name = "使用者全名")]
        public string FullName { get; set; }
        [Display(Name = "使用者名稱")]
        public string UserName { get; set; }
        [Display(Name = "部門代號")]
        public string DptId { get; set; }
        [Display(Name = "中文名稱")]
        public string DptName { get; set; }
    }
}
