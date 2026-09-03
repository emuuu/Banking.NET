namespace Banking.NET.Commerzbank.CorporatePayments;

/// <summary>
/// An EBICS order type used by the Corporate Payments API to select a message or order kind (e.g. `C53` for a
/// camt.053 account statement, `CCT` for a SEPA credit transfer). The set of order types actually available to a
/// customer depends on the agreement with the bank; codes not listed among <see cref="Known"/> are not rejected by
/// this library and can still be used, since the bank may enable customer-specific codes that are not publicly
/// documented.
/// </summary>
public readonly struct OrderType : IEquatable<OrderType>
{
    private readonly string? _code;
    private readonly IReadOnlyList<string>? _schemaVersions;

    private OrderType(string code, OrderDirection direction, string? messageType, IReadOnlyList<string> schemaVersions, string? description)
    {
        _code = code;
        Direction = direction;
        MessageType = messageType;
        _schemaVersions = schemaVersions;
        Description = description;
    }

    /// <summary>The upper-case order type code, e.g. `C53` or `CCT`. Empty for <c>default(OrderType)</c>.</summary>
    public string Code => _code ?? "";

    /// <summary>The direction of the order type. <see cref="OrderDirection.Unknown"/> for a code not in <see cref="Known"/>.</summary>
    public OrderDirection Direction { get; }

    /// <summary>The ISO 20022 message family the order type carries (e.g. `camt.053`, `pain.001`), or null if unknown.</summary>
    public string? MessageType { get; }

    /// <summary>The ISO 20022 schema versions the order type may carry, most recent first, or empty if unknown.</summary>
    public IReadOnlyList<string> SchemaVersions => _schemaVersions ?? [];

    /// <summary>A human-readable description of the order type as documented by the bank, or null if unknown.</summary>
    public string? Description { get; }

    /// <summary>Whether this order type is one of the documented Corporate Payments order types.</summary>
    public bool IsKnown => Direction != OrderDirection.Unknown;

    /// <summary>Parses an order type code. Unknown codes are accepted and returned with <see cref="Direction"/> <see cref="OrderDirection.Unknown"/>.</summary>
    /// <param name="code">The order type code, matched case-insensitively and trimmed.</param>
    /// <returns>The known <see cref="OrderType"/> for the code, or an unknown one wrapping the normalized code.</returns>
    /// <exception cref="ArgumentException"><paramref name="code"/> is null, empty, or white space.</exception>
    public static OrderType Parse(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalized = code.Trim().ToUpperInvariant();
        return KnownByCode.TryGetValue(normalized, out var known) ? known : new OrderType(normalized, OrderDirection.Unknown, null, [], null);
    }

    /// <summary>Attempts to parse an order type code.</summary>
    /// <param name="code">The order type code, matched case-insensitively and trimmed.</param>
    /// <param name="orderType">The parsed order type, or <c>default</c> if <paramref name="code"/> is null, empty, or white space.</param>
    /// <returns><c>false</c> if <paramref name="code"/> is null, empty, or white space; otherwise <c>true</c>.</returns>
    public static bool TryParse(string? code, out OrderType orderType)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            orderType = default;
            return false;
        }

        orderType = Parse(code);
        return true;
    }

    /// <summary>Intraday account report.</summary>
    public static OrderType C52 { get; } = new("C52", OrderDirection.Download, "camt.052", ["camt.052.001.08", "camt.052.001.02"], "Intraday account report");

    /// <summary>Account statement.</summary>
    public static OrderType C53 { get; } = new("C53", OrderDirection.Download, "camt.053", ["camt.053.001.08", "camt.053.001.02"], "Account statement");

    /// <summary>Debit and credit notification.</summary>
    public static OrderType C54 { get; } = new("C54", OrderDirection.Download, "camt.054", ["camt.054.001.08"], "Debit and credit notification");

    /// <summary>Bank services billing statement.</summary>
    public static OrderType C86 { get; } = new("C86", OrderDirection.Download, "camt.086", ["camt.086.001.02"], "Bank services billing statement");

    /// <summary>Customer acknowledgement (payment status report).</summary>
    public static OrderType HAC { get; } = new("HAC", OrderDirection.Download, "pain.002", ["pain.002.001.10", "pain.002.001.03"], "Customer acknowledgement (payment status report)");

    /// <summary>Payment status report for cross-border credit transfers.</summary>
    public static OrderType AXS { get; } = new("AXS", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for cross-border credit transfers");

    /// <summary>Payment status report for urgent credit transfers.</summary>
    public static OrderType CUZ { get; } = new("CUZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for urgent credit transfers");

    /// <summary>Payment status report for XIC and XID orders.</summary>
    public static OrderType XIP { get; } = new("XIP", OrderDirection.Download, "pain.002", ["pain.002.001.03", "pain.002.001.10"], "Payment status report for XIC and XID orders");

    /// <summary>Payment status report for instant credit transfers.</summary>
    public static OrderType CIZ { get; } = new("CIZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for instant credit transfers");

    /// <summary>Payment status report for direct debits.</summary>
    public static OrderType CDZ { get; } = new("CDZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for direct debits");

    /// <summary>Payment status report for credit transfers.</summary>
    public static OrderType CRZ { get; } = new("CRZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for credit transfers");

    /// <summary>SEPA credit transfer.</summary>
    public static OrderType CCT { get; } = new("CCT", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA credit transfer");

    /// <summary>SEPA credit transfer (CTV).</summary>
    public static OrderType CTV { get; } = new("CTV", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA credit transfer (CTV)");

    /// <summary>SEPA instant credit transfer.</summary>
    public static OrderType CIP { get; } = new("CIP", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA instant credit transfer");

    /// <summary>SEPA instant credit transfer (CIV).</summary>
    public static OrderType CIV { get; } = new("CIV", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA instant credit transfer (CIV)");

    /// <summary>SEPA credit transfer (pain.001.001.03).</summary>
    public static OrderType XIC { get; } = new("XIC", OrderDirection.Upload, "pain.001", ["pain.001.001.03"], "SEPA credit transfer (pain.001.001.03)");

    /// <summary>SEPA direct debit (CORE).</summary>
    public static OrderType CDD { get; } = new("CDD", OrderDirection.Upload, "pain.008", ["pain.008.001.08"], "SEPA direct debit (CORE)");

    /// <summary>SEPA direct debit (B2B).</summary>
    public static OrderType CDB { get; } = new("CDB", OrderDirection.Upload, "pain.008", ["pain.008.001.08"], "SEPA direct debit (B2B)");

    /// <summary>SEPA direct debit (pain.008.001.02).</summary>
    public static OrderType XID { get; } = new("XID", OrderDirection.Upload, "pain.008", ["pain.008.001.02"], "SEPA direct debit (pain.008.001.02)");

    /// <summary>Cross-border credit transfer.</summary>
    public static OrderType AXZ { get; } = new("AXZ", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "Cross-border credit transfer");

    /// <summary>Urgent credit transfer.</summary>
    public static OrderType CCU { get; } = new("CCU", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "Urgent credit transfer");

    /// <summary>All documented Corporate Payments order types, download types first.</summary>
    public static IReadOnlyList<OrderType> Known { get; } = [C52, C53, C54, C86, HAC, AXS, CUZ, XIP, CIZ, CDZ, CRZ, CCT, CTV, CIP, CIV, XIC, CDD, CDB, XID, AXZ, CCU];

    /// <summary>The documented order types with <see cref="OrderDirection.Download"/>.</summary>
    public static IReadOnlyList<OrderType> DownloadTypes { get; } = [.. Known.Where(orderType => orderType.Direction == OrderDirection.Download)];

    /// <summary>The documented order types with <see cref="OrderDirection.Upload"/>.</summary>
    public static IReadOnlyList<OrderType> UploadTypes { get; } = [.. Known.Where(orderType => orderType.Direction == OrderDirection.Upload)];

    private static Dictionary<string, OrderType> KnownByCode { get; } = Known.ToDictionary(orderType => orderType.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns <see cref="Code"/>.</summary>
    public override string ToString() => Code;

    /// <inheritdoc />
    public bool Equals(OrderType other) => string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is OrderType other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Code.GetHashCode(StringComparison.OrdinalIgnoreCase);

    /// <summary>Compares two order types by <see cref="Code"/>, case-insensitively.</summary>
    public static bool operator ==(OrderType left, OrderType right) => left.Equals(right);

    /// <summary>Compares two order types by <see cref="Code"/>, case-insensitively.</summary>
    public static bool operator !=(OrderType left, OrderType right) => !left.Equals(right);
}
