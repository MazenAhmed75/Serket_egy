using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Constants;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Pages;

[EnableRateLimiting("shopping")]
public class CheckoutModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailNotifier _emailNotifier;
    private readonly IConfiguration _configuration;
    private readonly PromoCodeService _promoCodeService;
    private readonly OrderConfirmationLinks _confirmationLinks;

    public CheckoutModel(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IEmailNotifier emailNotifier,
        IConfiguration configuration,
        PromoCodeService promoCodeService,
        OrderConfirmationLinks confirmationLinks)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _emailNotifier = emailNotifier;
        _configuration = configuration;
        _promoCodeService = promoCodeService;
        _confirmationLinks = confirmationLinks;
    }

    [BindProperty]
    public CheckoutInputModel Input { get; set; } = new();

    public Product? Product { get; private set; }

    /// <summary>Items / discount / shipping / total shown in the order box.</summary>
    public PriceBreakdown Pricing { get; private set; } = new(0, 0, 0, 0, 0, false, null);

    /// <summary>True when a shopper is logged in (promo codes need an account; details are pre-filled).</summary>
    public bool IsLoggedIn => HttpContext.GetCustomerId() is not null;

    /// <summary>The colour chosen on the product page (null when the product has no colours).</summary>
    public Core.Entities.ProductColor? SelectedColor =>
        Input.ProductColorId.HasValue
            ? Product?.Colors.FirstOrDefault(c => c.Id == Input.ProductColorId.Value)
            : null;

    /// <summary>Query string that sends the customer back to the product page with their choices intact.</summary>
    public string ChangeSelectionUrl =>
        $"/product/{Product?.Slug}?gender={Input.Gender}&size={Input.Size}&quantity={Input.Quantity}" +
        (Input.ProductColorId.HasValue ? $"&colorId={Input.ProductColorId}" : string.Empty);

    /// <summary>This page's own address, used as the return address after logging in.</summary>
    public string CheckoutUrl =>
        $"/Checkout?productId={Input.ProductId}&quantity={Input.Quantity}&gender={Input.Gender}&size={Input.Size}" +
        (Input.ProductColorId.HasValue ? $"&colorId={Input.ProductColorId}" : string.Empty);

    /// <summary>The governorates offered in the delivery drop-down.</summary>
    public IReadOnlyList<string> Governorates => EgyptGovernorates.All;

    public string VodafoneCashNumber => _configuration["Payment:VodafoneCashNumber"] ?? string.Empty;

    public string InstaPayHandle => _configuration["Payment:InstaPayHandle"] ?? string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid productId, int quantity = 1, Guid? colorId = null, string? gender = null, string? size = null)
    {
        Product = await _unitOfWork.Products.GetWithColorsAsync(productId);

        if (Product is null || !Product.IsActive)
        {
            return RedirectToPage("/Index");
        }

        Input.ProductId = Product.Id;
        // Validate against allowlists — reject anything that isn't a known value.
        Input.Gender = ProductGenders.IsValid(gender) ? gender! : ProductGenders.Default;
        Input.Size = ProductSizes.IsValid(size) ? size! : ProductSizes.Default;

        var activeColors = Product.Colors.Where(c => c.IsActive).ToList();
        Core.Entities.ProductColor? selectedColor = null;

        if (activeColors.Count > 0)
        {
            selectedColor = colorId.HasValue
                ? activeColors.FirstOrDefault(c => c.Id == colorId.Value)
                : null;

            // Checkout only confirms a choice made on the product page. If the colour is
            // missing or unknown, send the customer back to choose one.
            if (selectedColor is null)
            {
                return RedirectToPage("/Product", new { slug = Product.Slug });
            }

            Input.ProductColorId = selectedColor.Id;
        }

        // The chosen size must be in stock for the chosen colour; otherwise go back and pick again.
        var available = Product.UnitsAvailable(selectedColor?.Id, Input.Size);
        if (available <= 0)
        {
            return RedirectToPage("/Product", new { slug = Product.Slug, gender = Input.Gender, size = Input.Size, colorId = selectedColor?.Id });
        }

        Input.Quantity = Math.Clamp(quantity, 1, available);

        await LoadPricingAsync(null);
        await PrefillFromAccountAsync();

        return Page();
    }

    /// <summary>Called by the "Apply" button on the promo box (fetch). Returns the price lines for the page to show.</summary>
    public async Task<IActionResult> OnPostApplyPromoAsync(Guid productId, int quantity, string? promoCode)
    {
        var product = await _unitOfWork.Products.GetWithColorsAsync(productId);
        if (product is null || !product.IsActive)
        {
            return BadRequest();
        }

        quantity = Math.Clamp(quantity, 1, 1000);

        PromoValidationResult? result = null;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            result = await _promoCodeService.ValidateAsync(promoCode, HttpContext.GetCustomerId());
        }

        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        var pricing = PricingCalculator.Calculate(product.CurrentPrice, quantity, settings.ShippingFee, result?.Promo);

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
        Product = await _unitOfWork.Products.GetWithColorsAsync(Input.ProductId);

        if (Product is null || !Product.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This product is no longer available.");
            return Page();
        }

        var activeColors = Product.Colors.Where(c => c.IsActive).ToList();
        Core.Entities.ProductColor? selectedColor = null;
        var colourOk = true;

        if (activeColors.Count > 0)
        {
            selectedColor = Input.ProductColorId.HasValue
                ? activeColors.FirstOrDefault(c => c.Id == Input.ProductColorId.Value)
                : null;

            if (selectedColor is null)
            {
                colourOk = false;
                ModelState.AddModelError(nameof(Input.ProductColorId), "Please choose a colour.");
            }
        }

        if (!ProductGenders.IsValid(Input.Gender))
        {
            ModelState.AddModelError(nameof(Input.Gender), "Invalid gender selection.");
        }

        var sizeOk = ProductSizes.IsValid(Input.Size);
        if (!sizeOk)
        {
            ModelState.AddModelError(nameof(Input.Size), "Invalid size selection.");
        }

        // Friendly pre-check of stock (tracked per colour and size). The atomic deduction further down is what
        // really prevents overselling when two customers order at the same moment.
        if (colourOk && sizeOk)
        {
            var available = Product.UnitsAvailable(selectedColor?.Id, Input.Size);
            var what = selectedColor is null ? $"size {Input.Size}" : $"size {Input.Size} in {selectedColor.Name}";

            if (available <= 0)
            {
                ModelState.AddModelError(nameof(Input.Size), $"Sorry, {what} is sold out. Please go back and choose another.");
            }
            else if (Input.Quantity > available)
            {
                ModelState.AddModelError(nameof(Input.Quantity), $"Only {available} left in {what}.");
            }
        }

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

        // Promo code: always re-checked here — the browser's "Apply" result is never trusted.
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

        // From here on, the stock change, the customer, the order and the promo use are saved together or not at all.
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        // Take the units out in one atomic step that only works if enough are left.
        if (!await _unitOfWork.Products.TryDeductStockAsync(Product.Id, selectedColor?.Id, Input.Size, Input.Quantity))
        {
            ModelState.AddModelError(string.Empty, "Sorry, the last units of this item were just bought. Please go back and choose another size or colour.");
            return Page();
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

        var unitPrice = Product.CurrentPrice;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customer.Id,
            Customer = customer,
            SubtotalAmount = Pricing.Subtotal,
            DiscountAmount = Pricing.Discount,
            ShippingAmount = Pricing.Shipping,
            PromoCodeText = promo?.Code,
            TotalAmount = Pricing.Total,
            PaymentMethod = Input.PaymentMethod,
            OrderStatus = requiresReceipt ? OrderStatus.PendingVerification : OrderStatus.PendingPayment,
            Latitude = Input.Latitude,
            Longitude = Input.Longitude,
            Governorate = Input.Governorate,
            Area = Input.Area.Trim(),
            Street = Input.Street.Trim(),
            BuildingNumber = Input.BuildingNumber.Trim(),
            AddressDetail = Input.AddressDetail?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = Product.Id,
                    Quantity = Input.Quantity,
                    UnitPrice = unitPrice,
                    ProductColorId = selectedColor?.Id,
                    ColorName = selectedColor?.Name,
                    Gender = Input.Gender,
                    Size = Input.Size
                }
            }
        };

        if (requiresReceipt && Input.ReceiptFile is not null)
        {
            await using var stream = Input.ReceiptFile.OpenReadStream();
            var savedPath = await _fileStorageService.SavePrivateAsync(stream, Input.ReceiptFile.FileName, "receipts");

            order.PaymentReceipts.Add(new PaymentReceipt
            {
                Id = Guid.NewGuid(),
                ImagePath = savedPath,
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
            // The database allows each account one use per code; this only happens if the same
            // account used the code in another tab a moment ago. PostgreSQL refuses further commands in a
            // failed transaction, so it is rolled back (which also puts the stock back) before querying again.
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "This promo code was just used and can't be applied again. Please review your total and try again.");
            Input.PromoCode = null;
            ModelState.Remove("Input.PromoCode");
            await LoadPricingAsync(null);
            return Page();
        }

        await transaction.CommitAsync();

        var variantParts = new List<string>();
        if (!string.IsNullOrEmpty(Input.Gender)) variantParts.Add(Input.Gender);
        if (!string.IsNullOrEmpty(Input.Size)) variantParts.Add(Input.Size);
        if (selectedColor is not null) variantParts.Add(selectedColor.Name);

        var variantDesc = variantParts.Count > 0 ? $" ({string.Join(", ", variantParts)})" : "";
        var itemDescription = $"{Input.Quantity} x {Product.Name}{variantDesc}";

        await _emailNotifier.SendOrderNotificationAsync(
            $"New order {order.OrderNumber}",
            $"Customer: {Input.CustomerFullName} ({Input.CustomerPhoneNumber})\n" +
            $"Item: {itemDescription}\n" +
            $"Items: {Pricing.Subtotal:N2} EGP\n" +
            (Pricing.Discount > 0 ? $"Discount: -{Pricing.Discount:N2} EGP\n" : "") +
            $"Shipping: {Pricing.Shipping:N2} EGP\n" +
            $"Total: {Pricing.Total:N2} EGP\n" +
            (promo is null ? "" : $"Promo code: {promo.Code} ({PromoSummary(Pricing)})\n") +
            $"Payment method: {Input.PaymentMethod}\n" +
            $"Delivery: {order.AddressLine}" + (string.IsNullOrEmpty(order.AddressDetail) ? "" : $" - {order.AddressDetail}") + "\n" +
            $"Map: https://www.openstreetmap.org/?mlat={Input.Latitude}&mlon={Input.Longitude}#map=17/{Input.Latitude}/{Input.Longitude}\n" +
            $"Order status: {order.OrderStatus}");

        // Receipt to the customer (their account e-mail, or the optional e-mail typed at checkout).
        var receiptEmail = customer.Email ?? Input.CustomerEmail;
        if (!string.IsNullOrWhiteSpace(receiptEmail))
        {
            await _emailNotifier.SendToCustomerAsync(
                receiptEmail,
                OrderReceiptFormatter.Subject(order),
                OrderReceiptFormatter.Body(order, Product.Name));
        }

        return RedirectToPage("/OrderConfirmation", new { t = _confirmationLinks.CreateToken(order.OrderNumber) });
    }

    private async Task LoadPricingAsync(PromoCode? promo)
    {
        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        Pricing = PricingCalculator.Calculate(Product!.CurrentPrice, Input.Quantity, settings.ShippingFee, promo);
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

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public class CheckoutInputModel
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Colour")]
        public Guid? ProductColorId { get; set; }

        [Required(ErrorMessage = "Please select gender.")]
        [StringLength(20)]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = "Male";

        [Required(ErrorMessage = "Please select a size.")]
        [StringLength(20)]
        [Display(Name = "Size")]
        public string Size { get; set; } = ProductSizes.Default;

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
