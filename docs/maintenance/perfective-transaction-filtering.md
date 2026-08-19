# Perfective maintenance: transaction filtering

## Program comprehension

`TransactionController.Index()` currently builds one user-scoped EF Core query, includes
`Category`, materializes every matching transaction, and passes the full list to the view.
It accepts no query parameters and applies no server-side ordering or paging.

`Views/Transaction/Index.cshtml` renders that list with a Syncfusion grid containing
Category, Date, Amount, and Actions columns. The grid supplies client-side sorting, search,
and ten-row paging, but only after the controller has loaded and transferred the user's
entire history. There is no date/category filter form or server-side pager.

## Change management

This is perfective, incremental maintenance: it adds an optional way to find and navigate
transactions rather than correcting a defect. With no filters supplied, the same user-owned
history remains available, now ordered newest-first and divided into pages.

## Impact analysis

The change is confined to `TransactionController.Index` (optional date/category parameters,
query composition, ordering, and paging) and `Views/Transaction/Index.cshtml` (filter form
and pager). The existing category population helper can supply the dropdown while retaining
user scoping. Create, Edit, Details, and Delete actions are unaffected. Existing `Date` and
`CategoryId` columns support the query, so no model, schema, or migration change is needed.
QA should test individual and combined filters, inclusive end dates, empty results, invalid
page numbers, preserved filters during paging, and isolation between users.
