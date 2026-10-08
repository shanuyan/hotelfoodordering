using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBillingSystem.Models
{
    public enum TableStatus
    {
        Available = 1,
        Occupied = 2,
        Reserved = 3,
        Cleaning = 4,
        OutOfService = 5
    }

    public enum StaffStatus
    {
        Available = 1,
        Busy = 2,
        Offline = 3,
        OnBreak = 4
    }

    public enum OrderStatus
    {
        New = 1,
        Assigned = 2,
        Accepted = 3,
        Preparing = 4,
        Ready = 5,
        Served = 6,
        Completed = 7,
        Cancelled = 8,
        Rejected = 9
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Card = 2,
        Online = 3
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3
    }

    public class RestaurantTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string TableNumber { get; set; } = string.Empty;

        public int Capacity { get; set; } = 4;

        [StringLength(50)]
        public string Location { get; set; } = "Main Dining";

        public TableStatus Status { get; set; } = TableStatus.Available;

        [Required]
        [StringLength(100)]
        public string QrToken { get; set; } = Guid.NewGuid().ToString("N");

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TableSession> Sessions { get; set; } = new List<TableSession>();
    }

    public class TableSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SessionCode { get; set; } = string.Empty;

        public int TableId { get; set; }
        [ForeignKey("TableId")]
        public virtual RestaurantTable? Table { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsBillRequested { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public class FoodCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
    }

    public class FoodItem
    {
        [Key]
        public int Id { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual FoodCategory? Category { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; } = "/images/food-placeholder.png";

        public int PrepTimeMinutes { get; set; } = 15;
        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public int TableSessionId { get; set; }
        [ForeignKey("TableSessionId")]
        public virtual TableSession? TableSession { get; set; }

        public int TableId { get; set; }
        [ForeignKey("TableId")]
        public virtual RestaurantTable? Table { get; set; }

        public string? AssignedKitchenStaffId { get; set; }
        [ForeignKey("AssignedKitchenStaffId")]
        public virtual ApplicationUser? AssignedKitchenStaff { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.New;

        [StringLength(500)]
        public string CustomerNotes { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? PreparingAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? ServedAt { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public int FoodItemId { get; set; }
        [ForeignKey("FoodItemId")]
        public virtual FoodItem? FoodItem { get; set; }

        [Required]
        [StringLength(100)]
        public string FoodName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [StringLength(200)]
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    public class OrderStatusHistory
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public OrderStatus PreviousStatus { get; set; }
        public OrderStatus NewStatus { get; set; }

        public string? ChangedByUserId { get; set; }
        [ForeignKey("ChangedByUserId")]
        public virtual ApplicationUser? ChangedByUser { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(250)]
        public string Remarks { get; set; } = string.Empty;
    }

    public class KitchenStaffStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public StaffStatus CurrentStatus { get; set; } = StaffStatus.Available;
        public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
    }

    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int TableSessionId { get; set; }
        [ForeignKey("TableSessionId")]
        public virtual TableSession? TableSession { get; set; }

        public int TableId { get; set; }
        [ForeignKey("TableId")]
        public virtual RestaurantTable? Table { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(100)]
        public string DiscountReason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharge { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public string? CashierUserId { get; set; }
        [ForeignKey("CashierUserId")]
        public virtual ApplicationUser? CashierUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int BillId { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill? Bill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;

        [StringLength(100)]
        public string TransactionReference { get; set; } = string.Empty;

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public string? ProcessedByUserId { get; set; }
        [ForeignKey("ProcessedByUserId")]
        public virtual ApplicationUser? ProcessedByUser { get; set; }
    }

    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [StringLength(50)]
        public string EntityId { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string TargetRole { get; set; } = string.Empty; // e.g. "KitchenStaff", "CashierStaff", "Admin"

        public string? TargetUserId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(250)]
        public string LinkUrl { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
