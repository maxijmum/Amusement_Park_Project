using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmusementParkAPI.Models
{
    [Table("shopping_cart")]
    public class ShoppingCart
    {
        [Key]
        [Column("Cart_ID")]
        public int CartId { get; set; }

        [Required]
        [Column("Visitor_ID")]
        public int VisitorId { get; set; }

        [Required]
        [Column("Commodity_TypeID")]
        public int CommodityTypeId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Column("Quantity")]
        public int Quantity { get; set; }

        [MaxLength(10)]
        [Column("Size")]
        public string? Size { get; set; }

        [Column("Added_At")]
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("VisitorId")]
        public virtual Customer? Visitor { get; set; }

        [ForeignKey("CommodityTypeId")]
        public virtual CommodityType? CommodityType { get; set; }
    }
}
