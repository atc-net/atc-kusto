# Atc.Kusto.Ingestion.Sample

A console app showing every `IKustoIngestor` capability against a real cluster. Each scenario logs what happened, and where possible reads its rows back with a normal Atc.Kusto query, so you can see the effect.

## Setup

Ingestion needs a **writable** database (`help.kusto.windows.net` is read-only). A [free Kusto cluster](https://aka.ms/kustofree) works.

1. Run [`setup.kql`](setup.kql) against the database once. It creates the `SampleDeviceReadings` table (camelCase columns), its JSON mapping `SampleDeviceReadings_mapping`, and enables the streaming ingestion policy.
2. Set the environment variables:

   | Variable                    | Required | Example                                                    |
   | --------------------------- | -------- | ---------------------------------------------------------- |
   | `ATC_KUSTO_INGEST_CLUSTER`  | yes      | `https://mycluster.westeurope.kusto.windows.net`           |
   | `ATC_KUSTO_INGEST_DATABASE` | yes      | `MyDatabase`                                               |
   | `ATC_KUSTO_INGEST_BLOB_URL` | no       | A cluster-readable CSV blob (SAS URL) for the blob scenario |

3. Sign in so `DefaultAzureCredential` can find you (for example `az login`). Your identity needs at least **Database Ingestor** (plus **Database Viewer** to read the rows back).
4. `dotnet run`

Without the required variables the sample prints these steps and exits.

## Scenarios

| # | Scenario | What to look for |
| - | -------- | ---------------- |
| 1 | In-memory rows, default mode | `ManagedStreaming`: `Succeeded` when streamed, `Queued` if it fell back (e.g. streaming not enabled). |
| 2 | `Streaming`, then read back | The rows are queryable immediately after the call returns. Fails if streaming isn't enabled on the cluster and table. |
| 3 | `Queued` with `EnableTracking` | `Queued`, an `OperationId` and an `OperationHandle`, then one `GetIngestionStatusAsync` check — normally still `InProgress`. The rows appear after the batching delay (typically minutes). |
| 4 | CSV from a stream | No mapping needed (columns by position); the caller's stream is left open. |
| 5 | Empty batch | `Skipped`, `IsSuccess = true`, the cluster isn't contacted. |
| 6 | Mapping mismatch | PascalCase JSON against camelCase mapping paths: the ingestion **succeeds**, but the read-back shows `(empty)` columns. This is why property names must match the mapping. |
| 7 | Failure handling | A missing table gives `Status = Failed` with an error message, not an exception; `EnsureSuccess()` then throws `KustoIngestionException`. |
| 8 | Blob | Runs only when `ATC_KUSTO_INGEST_BLOB_URL` is set; queued ingestion of a CSV blob read by the cluster. |

Every row is tagged with a per-run `runId` and the scenario name, so repeated runs don't interfere. Clean up with `.drop table SampleDeviceReadings`.