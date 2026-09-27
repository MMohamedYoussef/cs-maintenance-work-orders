# Maintenance work orders

Rank repair orders by asset criticality × order severity, then assign each order to the first qualified technician with enough remaining hours. This gives a clear, repeatable daily schedule and makes capacity gaps visible.

## Run

Requires .NET 10. From this directory:

```sh
dotnet run --project WorkOrders.csproj -- fixtures/assets.csv fixtures/orders.csv fixtures/technicians.csv schedule.csv
dotnet run --project tests/Tests.csproj
```

`schedule.csv` must be a new path. Compare it with `examples/schedule.csv`. All fixtures use synthetic names.

Headers are `asset,criticality`, `order,asset,severity,hours,skill`, and `technician,skill,capacity_hours`. Criticality and severity are integers 1..5; hours are 1..1,000,000 and capacity hours 0..1,000,000. IDs and skills contain ASCII letters, digits, `_`, or `-`. Each asset, order, and technician ID must be unique, and an order must name a known asset. Bad input exits 2 without writing a schedule.

The output is `order,priority,technician,status`, sorted by descending priority, then order ID. It uses greedy assignment, a single skill per technician, and integer-hour tasks; it does not optimize travel, deadlines, or split work across technicians.
