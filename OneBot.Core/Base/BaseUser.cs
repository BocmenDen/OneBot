using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OneBot.Base
{
    [PrimaryKey(nameof(Id))]
    public class BaseUser
    {
        [Key]
        public int Id { get; set; }

        public BaseUser() { }
        public BaseUser(int id) => Id = id;

        public virtual BaseUser CreateEmpty() => new();

        public override string ToString() => $"UserId: {Id}";
    }
}
