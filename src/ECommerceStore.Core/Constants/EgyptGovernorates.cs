namespace ECommerceStore.Core.Constants;

/// <summary>The 27 governorates of Egypt, offered as a drop-down at checkout.</summary>
public static class EgyptGovernorates
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "Cairo", "Giza", "Alexandria", "Qalyubia", "Port Said", "Suez", "Dakahlia", "Sharqia", "Gharbia",
        "Monufia", "Beheira", "Kafr El Sheikh", "Damietta", "Ismailia", "Fayoum", "Beni Suef", "Minya",
        "Asyut", "Sohag", "Qena", "Luxor", "Aswan", "Red Sea", "New Valley", "Matrouh", "North Sinai", "South Sinai"
    };

    public static bool IsValid(string? governorate) => governorate is not null && All.Contains(governorate);
}
