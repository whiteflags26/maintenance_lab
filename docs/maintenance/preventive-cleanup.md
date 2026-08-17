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
