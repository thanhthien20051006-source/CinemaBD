namespace CinemaBD.Web.Models;

public class CheckoutPageViewModel
{
    public string ShowtimeId { get; set; } = string.Empty;
    public List<string> Seats { get; set; } = new();
    public List<string> SeatIds { get; set; } = new();
    public string Combos { get; set; } = string.Empty;
    public decimal TicketTotal { get; set; }
    public decimal ComboTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LoyaltyDiscountAmount { get; set; }
    public int LoyaltyPoints { get; set; }
    public int AvailableLoyaltyPoints { get; set; }
    public int LoyaltyPointValue { get; set; } = 1000;
    public decimal TotalBeforeDiscount => TicketTotal + ComboTotal;
    public string VoucherCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

