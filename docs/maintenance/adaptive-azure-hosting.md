# Adaptive maintenance: Azure hosting

## Program comprehension

`Dashboard.cs` formats `TotalIncome`, `Expense`, and `Balance` with `c0`, and formats
the two doughnut-chart amount projections with `C0`. `Transaction.FormattedAmount` also
uses `C0`, while Razor views consume these already-formatted values rather than selecting
a culture themselves.

## Reverse engineering

Reading `Program.cs` confirms there is no request-localization middleware and neither
`CultureInfo.DefaultThreadCurrentCulture` nor `DefaultThreadCurrentUICulture` is set.
Currency output therefore inherits the process or operating-system culture supplied by
the host. Identity is registered with default application-cookie security and SameSite
settings rather than an explicit policy for the Azure deployment environment.

## Change management

This is adaptive maintenance because it responds to moving the application from a
developer-controlled machine to Azure App Service. It is an anticipatory change: the Azure
migration was planned and underway, so dependence on host locale and proxy-sensitive cookie
defaults was foreseeable. The change stabilizes existing behavior rather than adding a new
user-facing capability.

## Impact analysis

The dependency sweep found currency-format calls in `Dashboard.cs` and the
`Transaction.FormattedAmount` model property; the Razor views render those results and do
not apply independent currency formats. Setting the default culture once in `Program.cs`
is the lowest-ripple change point and also covers future formatting calls. The cookie change
is confined to Identity's application-cookie configuration. Controllers, views, database
schema, and migrations need no modification. QA should verify consistent currency symbols
and formatting locally and on Azure, plus sign-in persistence over HTTPS.
