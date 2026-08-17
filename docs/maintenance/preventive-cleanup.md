# Preventive maintenance: duplication cleanup

## Program comprehension and reverse engineering

The category-type values are domain data but currently exist as unchecked string literals.
The inventory found `Income`/`Expense` comparisons or stored values in:

- `Category.cs` and `Transaction.cs`
- `Dashboard.cs` and `PredictionService.cs`
- Category Create/Edit form values and comparisons
- Category, Transaction, and Dashboard client-template comparisons

The Category and Transaction controllers independently call
`_userManager.GetUserId(User)` in every user-scoped query or assignment. Category uses it
in Index, Details, Create, Edit, Delete, and DeleteConfirmed; Transaction uses it in Index,
Details, Delete, DeleteConfirmed, and PopulateCategories. There is no shared controller
helper or centralized ownership-query abstraction, making omission easy when actions are
added.

## Change management

This is preventive, incremental maintenance: no reported defect or environment change
requires it. The small refactor is performed opportunistically to reduce the chance that a
misspelled category type or omitted user lookup creates a future defect.

## Impact analysis

The constants belong in `Spendit.Models`, already referenced by the web and utility
projects, avoiding a new or circular dependency. Compile-time references affect the two
models, `Dashboard.cs`, `PredictionService.cs`, and Razor forms/client templates. A shared
base controller affects only Category and Transaction controller inheritance and replaces
their repeated user-manager calls; routes and public action signatures remain unchanged.
There is no schema or migration impact because constant values remain exactly `Income` and
`Expense`. QA should exercise category forms, type styling, dashboard totals, prediction,
and every user-scoped Category/Transaction action.
