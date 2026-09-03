namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Identifies an ISO 20022 document by its message type, schema identifier and XML namespace.</summary>
/// <param name="Type">The identified message type, or <see cref="Iso20022MessageType.Unknown"/> when the namespace does not match a known ISO 20022 schema.</param>
/// <param name="Identifier">The part of the root element's namespace after the ISO 20022 tech XSD prefix, e.g. <c>"camt.053.001.08"</c>. Empty only when the namespace does not carry that prefix at all; a recognized prefix followed by a malformed or unknown schema identifier still yields a non-empty value here even though <paramref name="Type"/> is <see cref="Iso20022MessageType.Unknown"/>.</param>
/// <param name="Namespace">The full XML namespace of the document's root element.</param>
public sealed record Iso20022MessageIdentifier(Iso20022MessageType Type, string Identifier, string Namespace);
