namespace ECommerceStore.Core.Enums;

/// <summary>
/// Supported direct payment methods for an <see cref="Entities.Order"/>.
/// </summary>
public enum PaymentMethod
{
    VodafoneCash = 0,
    InstaPay = 1,
    CashOnDelivery = 2
}
