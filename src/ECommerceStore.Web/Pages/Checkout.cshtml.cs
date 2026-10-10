using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Constants;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;
using ECommerceStore.Core.Exceptions;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Web.Pages;

/// <summary>
/// Checkout for everything in the cart. The cart only remembers what was chosen; prices and stock are read from the
/// database again here, and the order is saved together with the stock change in one transaction.
/// </summary>
[EnableRateLimiting("shopping")]
public class CheckoutModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailQueue _emailQueue;
    private readonly IConfiguration _configuration;
    private readonly PromoCodeService _promoCodeService;
    private readonly OrderConfirmationLinks _confirmationLinks;
    private readonly FileCleanup _fileCleanup;
    private readonly CartStore _cartStore;
    private readonly CartService _cartService;
    private readonly ILogger<CheckoutModel> _logger;

    public CheckoutModel(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IEmailQueue emailQueue,
        IConfiguration configuration,
        PromoCodeService promoCodeService,
        OrderConfirmationLinks confirmationLinks,
        FileCleanup fileCleanup,
        CartStore cartStore,
        CartService cartService,
        ILogger<CheckoutModel> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _emailQueue = emailQueue;
        _configuration = configuration;
        _promoCodeService = promoCodeService;
        _confirmationLinks = confirmationLinks;
        _fileCleanup = fileCleanup;
        _cartStore = cartStore;
        _cartService = cartService;
        _logger = logger;
    }

    [BindProperty]
    public CheckoutInputModel Input { get; set; } = new();

    /// <summary>The cart's lines, priced from the database.</summary>
    public CartSummary Summary { get; private set; } = new(Array.Empty<CartItemView>());

    /// <summary>Items / discount / shipping / total shown in the order box.</summary>
    public PriceBreakdown Pricing { get; private set; } = new(0, 0, 0, 0, 0, false, null);

    /// <summary>True when a shopper is logged in (promo codes need an account; details are pre-filled).</summary>
    public bool IsLoggedIn => HttpContext.GetCustomerId() is not null;

    /// <summary>Where to come back to after logging in or signing up.</summary>
    public string CheckoutUrl => "/Checkout";

    /// <summary>The governorates offered in the delivery drop-down.</summary>
    public IReadOnlyList<string> Governorates => EgyptGovernorates.All;

    public string VodafoneCashNumber => _configuration["Payment:VodafoneCashNumber"] ?? string.Empty;

    public string InstaPayHandle => _configuration["Payment:InstaPayHandle"] ?? string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var summary = await _cartService.BuildAsync(_cartStore.Read(HttpContext));
        if (summary.IsEmpty || summary.HasProblems)
        {
            // Nothing to buy, or something in the cart changed: the cart page explains what.
            return RedirectToPage("/Cart");
        }

        Summary = summary;
        await LoadPricingAsync(null);
        await PrefillFromAccountAsync();

        return Page();
    }

    /// <summary>Called by the "Apply" button on the promo box (fetch). Returns the price lines for the page to show.</summary>
    public async Task<IActionResult> OnPostApplyPromoAsync(string? promoCode)
    {
        var summary = await _cartService.BuildAsync(_cartStore.Read(HttpContext));
        if (summary.IsEmpty)
        {
            return BadRequest();
        }

        PromoValidationResult? result = null;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            result = await _promoCodeService.ValidateAsync(promoCode, HttpContext.GetCustomerId());
        }

        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        var pricing = PricingCalculator.Calculate(summary.Subtotal, settings.ShippingFee, result?.Promo);

        var message = result switch
        {
            null => string.Empty,
            { IsValid: false } => result.Error ?? "This promo code is not valid.",
            _ => DescribePromo(pricing)
        };

        return new JsonResult(new
        {
            valid = result?.IsValid == true,
            requiresLogin = result?.RequiresLogin == true,
            message,
            code = result?.Promo?.Code,
            percentOff = pricing.PercentOff,
            freeShipping = pricing.FreeShipping,
            subtotalText = Money(pricing.Subtotal),
            discount = pricing.Discount,
            discountText = "-" + Money(pricing.Discount),
            shippingText = ShippingText(pricing.Shipping),
            totalText = Money(pricing.Total)
        });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var summary = await _cartService.BuildAsync(_cartStore.Read(HttpContext));
        if (summary.IsEmpty || summary.HasProblems)
        {
            return RedirectToPage("/Cart");
        }

        Summary = summary;

        if (!string.IsNullOrEmpty(Input.Governorate) && !EgyptGovernorates.IsValid(Input.Governorate))
        {
            ModelState.AddModelError(nameof(Input.Governorate), "Please choose a governorate from the list.");
        }

        if (Input.Latitude == 0 && Input.Longitude == 0)
        {
            ModelState.AddModelError(string.Empty, "Please drop a pin on the map for the delivery location.");
        }

        var requiresReceipt = Input.PaymentMethod != PaymentMethod.CashOnDelivery;
        if (requiresReceipt)
        {
            if (Input.ReceiptFile is null || Input.ReceiptFile.Length == 0)
            {
                ModelState.AddModelError(nameof(Input.ReceiptFile), "Please upload a screenshot of your payment receipt.");
            }
            else if (ImageUploadRules.Validate(Input.ReceiptFile) is { } receiptError)
            {
                ModelState.AddModelError(nameof(Input.ReceiptFile), receiptError);
            }
        }

        // Promo code: always re-checked here; the browser's "Apply" result is never trusted.
        var customerId = HttpContext.GetCustomerId();
        PromoCode? promo = null;
        if (!string.IsNullOrWhiteSpace(Input.PromoCode))
        {
            var promoResult = await _promoCodeService.ValidateAsync(Input.PromoCode, customerId);
            if (promoResult.IsValid)
            {
                promo = promoResult.Promo;
            }
            else
            {
                ModelState.AddModelError(string.Empty, promoResult.Error ?? "This promo code is not valid.");
                Input.PromoCode = null;
                ModelState.Remove("Input.PromoCode");
            }
        }

        await LoadPricingAsync(promo);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // From here on, the stock changes, the customer, the order and the promo use are saved together or not at all.
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        // Take the units out one line at a time, each in one atomic step that only works if enough are left.
        foreach (var item in summary.Items)
        {
            if (!await _unitOfWork.Products.TryDeductStockAsync(item.Line.ProductId, item.Line.ColorId, item.Line.Size, item.Line.Quantity))
            {
                // Someone else just bought it. Leaving here rolls back the lines already taken.
                TempData["CartNotice"] = $"Sorry, the last units of {item.ProductName} (size {item.Line.Size}) were just bought. Please review your cart.";
                return RedirectToPage("/Cart");
            }
        }

        // Who is ordering: the logged-in account, or a brand-new guest record. Guests are never matched to an
        // earlier customer by phone number, so one person's details can't overwrite or reveal another's.
        Customer? customer = null;
        if (customerId is not null)
        {
            customer = await _unitOfWork.Customers.GetByIdAsync(customerId.Value);
        }

        if (customer is not null)
        {
            // Accounts keep their login e-mail; name and phone can be updated here.
            customer.FullName = Input.CustomerFullName;
            customer.PhoneNumber = Input.CustomerPhoneNumber;
        }
        else
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                FullName = Input.CustomerFullName,
                PhoneNumber = Input.CustomerPhoneNumber,
                Email = Input.CustomerEmail
            };
            await _unitOfWork.Customers.AddAsync(customer);
        }

        var order = OrderBuilder.Build(
            summary,
            customer,
            Pricing,
            Input.PaymentMethod,
            new OrderDelivery(
                Input.Governorate,
                Input.Area,
                Input.Street,
                Input.BuildingNumber,
                Input.AddressDetail ?? string.Empty,
                Input.Latitude,
                Input.Longitude));

        string? savedReceiptPath = null;
        if (requiresReceipt && Input.ReceiptFile is not null)
        {
            try
            {
                await using var stream = Input.ReceiptFile.OpenReadStream();
                savedReceiptPath = await _fileStorageService.SavePrivateAsync(stream, Input.ReceiptFile.FileName, "receipts");
            }
            catch (FileStorageException)
            {
                // Nothing has been saved yet: leaving here rolls the transaction back, so no order and no stock change.
                ModelState.AddModelError(nameof(Input.ReceiptFile), "We couldn't upload your receipt right now. Nothing was ordered. Please choose the screenshot again and retry in a moment.");
                return Page();
            }

            order.PaymentReceipts.Add(new PaymentReceipt
            {
                Id = Guid.NewGuid(),
                ImagePath = savedReceiptPath,
                TransactionReference = Input.TransactionReference,
                UploadedAt = DateTime.UtcNow,
                IsVerified = false
            });
        }

        await _unitOfWork.Orders.AddAsync(order);

        if (promo is not null)
        {
            await _unitOfWork.PromoRedemptions.AddAsync(new PromoRedemption
            {
                Id = Guid.NewGuid(),
                PromoCodeId = promo.Id,
                CustomerId = customer.Id,
                OrderId = order.Id,
                RedeemedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException) when (promo is not null)
        {
            // The database allows each account one use per code; this only happens if the same account used the
            // code in another tab a moment ago. PostgreSQL refuses further commands in a failed transaction, so it is
            // rolled back (which also puts the stock back) before querying again.
            await transaction.RollbackAsync();
            await _fileCleanup.DeleteQuietlyAsync(savedReceiptPath);
            ModelState.AddModelError(string.Empty, "This promo code was just used and can't be applied again. Please review your total and try again.");
            Input.PromoCode = null;
            ModelState.Remove("Input.PromoCode");
            await LoadPricingAsync(null);
            return Page();
        }

        await transaction.CommitAsync();

        // The order is saved. Nothing below may stop the customer from reaching the confirmation page.
        _cartStore.Clear(HttpContext);
        QueueNotifications(order, summary, customer);

        return RedirectToPage("/OrderConfirmation", new { t = _confirmationLinks.CreateToken(order.OrderNumber) });
    }

    /// <summary>Hands the owner alert and the customer's receipt to the background sender (never throws).</summary>
    private void QueueNotifications(Order order, CartSummary summary, Customer customer)
    {
        try
        {
            var names = summary.Items
                .GroupBy(i => i.Line.ProductId)
                .ToDictionary(g => g.Key, g => g.First().ProductName);

            _emailQueue.Enqueue(new EmailMessage(
                null,
                OrderReceiptFormatter.OwnerSubject(order),
                OrderReceiptFormatter.OwnerBody(order, names, Input.CustomerPhoneNumber)));

            var receiptEmail = customer.Email ?? Input.CustomerEmail;
            if (!string.IsNullOrWhiteSpace(receiptEmail))
            {
                _emailQueue.Enqueue(new EmailMessage(
                    receiptEmail,
                    OrderReceiptFormatter.Subject(order),
                    OrderReceiptFormatter.Body(order, names)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order {OrderNumber} was saved but its e-mails could not be queued.", order.OrderNumber);
        }
    }

    private async Task LoadPricingAsync(PromoCode? promo)
    {
        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        Pricing = PricingCalculator.Calculate(Summary.Subtotal, settings.ShippingFee, promo);
    }

    /// <summary>Logged-in shoppers get their name, phone and e-mail filled in. The delivery address is always typed fresh.</summary>
    private async Task PrefillFromAccountAsync()
    {
        var customerId = HttpContext.GetCustomerId();
        if (customerId is null)
        {
            return;
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId.Value);
        if (customer is null)
        {
            return;
        }

        Input.CustomerFullName = customer.FullName;
        Input.CustomerPhoneNumber = customer.PhoneNumber;
        Input.CustomerEmail = customer.Email;
    }

    public static string Money(decimal amount) => $"{amount:N2} EGP";

    public static string ShippingText(decimal shipping) => shipping <= 0 ? "Free shipping" : Money(shipping);

    private static string PromoSummary(PriceBreakdown pricing)
    {
        var parts = new List<string>();
        if (pricing.PercentOff > 0) parts.Add($"{pricing.PercentOff}% off");
        if (pricing.FreeShipping) parts.Add("free shipping");
        return string.Join(" + ", parts);
    }

    private static string DescribePromo(PriceBreakdown pricing)
    {
        var summary = PromoSummary(pricing);
        return summary.Length == 0 ? "Promo code applied." : $"Promo code applied: {summary}.";
    }

    public class CheckoutInputModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full name")]
        public string CustomerFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(254, ErrorMessage = "Email address is too long.")]
        [Display(Name = "Email (optional, we send your receipt here)")]
        public string? CustomerEmail { get; set; }

        [Required]
        [Display(Name = "Payment method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [Required(ErrorMessage = "Please choose your governorate.")]
        [StringLength(50)]
        [Display(Name = "Governorate")]
        public string Governorate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your area.")]
        [StringLength(100)]
        [Display(Name = "Area")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your street name.")]
        [StringLength(150)]
        [Display(Name = "Street name")]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your building number.")]
        [StringLength(30)]
        [Display(Name = "Building number")]
        public string BuildingNumber { get; set; } = string.Empty;

        // Optional: nullable so the framework does not treat it as required.
        [StringLength(500)]
        [Display(Name = "Address details (optional)")]
        public string? AddressDetail { get; set; }

        [Display(Name = "Payment receipt screenshot")]
        public IFormFile? ReceiptFile { get; set; }

        [StringLength(100)]
        [Display(Name = "Transaction reference (optional)")]
        public string? TransactionReference { get; set; }

        [StringLength(50)]
        public string? PromoCode { get; set; }
    }
}
