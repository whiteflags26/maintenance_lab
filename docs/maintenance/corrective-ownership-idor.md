# Corrective maintenance: ownership checks

## Program comprehension

`Category` stores the owning ASP.NET Identity user in `UserId`. `Transaction` does not
store a user ID; its owner is reached through `Transaction.Category.UserId`. The
controllers enforce this rule independently in each action rather than through a shared
authorization policy or query helper.

## Reverse engineering

No design document defines the ownership rule, so the current behavior was reconstructed
from `CategoryController` and `TransactionController`.

| Controller action | Filters by current `UserId`? |
| --- | --- |
| `Category.Index` | Yes |
| `Category.Details` | No |
| `Category.Delete` (GET) | No |
| `Category.DeleteConfirmed` (POST) | No |
| `Transaction.Index` | Yes, through `Category.UserId` |
| `Transaction.Details` | No |
| `Transaction.Delete` (GET) | No |
| `Transaction.DeleteConfirmed` (POST) | No |

## Change management

This is corrective maintenance: an incomplete ownership check is a logic defect, not a
new capability. It is an emergency change because authenticated users can expose or delete
another user's data and the fix cannot wait for the normal release cycle. Approval should
weigh the high cost of leaving cross-user disclosure and data loss possible against the
small, localized implementation cost. Legitimate user behavior does not change.

## Impact analysis

Dependency tracing limits the fix to `CategoryController.Details`, `Delete`, and
`DeleteConfirmed`, plus the equivalent three `TransactionController` actions. Their Details
and Delete views only render the supplied model, so they need no changes when a non-owned
resource resolves to `NotFound()`. No model, database schema, or routing change is required.
QA should verify that a bookmarked `/Category/Details/{id}` belonging to another user now
returns 404 rather than displaying the record, and repeat that ownership check for the
transaction details and both delete flows.
