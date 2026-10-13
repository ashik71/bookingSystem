# Grafana Cloud runbook: alerts for the demo

The demo runs on Render (`slotbook-api`), exports traces, metrics and logs over OTLP to Grafana Cloud
(free tier), and uses MongoDB Atlas (free tier). This runbook sets up the checks and alerts that tell the
developer the demo is down or near a free-tier limit (AD-17, AD-18). Everything below is configured by hand
in the Grafana Cloud and Atlas UIs; there is no Terraform until production exists.

## App instruments added for the alerts

| Instrument | Meter | Type | Tags |
|---|---|---|---|
| `slotbook.mongodb.operations` | `SlotBook.MongoDB` | counter | `db.operation.name` only (the command name, never command text) |
| `slotbook.outbox.pending` | `SlotBook.Outbox` | observable gauge | none |

The gauge counts documents in the bus-outbox message collection, read once per metric export. If MongoDB is
unreachable it records nothing for that export (the series has a gap; it does not drop to 0).

## 1. Synthetics check

Testing & synthetics → Synthetics → Add check → HTTP.

| Setting | Value |
|---|---|
| Target | `https://slotbook-api-sc87.onrender.com/health` |
| Frequency | 5 minutes |
| Probes | **one**, nearest Render's region (Mumbai or Singapore) |
| Valid status codes | 200 |
| Body regex | `Healthy` (response body matches `"Healthy"`) |
| Timeout | the maximum the check allows (Render cold starts are slow) |

One probe at 5 minutes is about 8,900 executions a month, inside the free allowance. Adding probes multiplies this.

## 2. Contact point and policy

Alerting → Contact points → add an **email** contact point with the developer's address. Alerting →
Notification policies → set it as the **default** contact point.

## 3. Alert rules

Alerting → Alert rules → New alert rule (Grafana-managed), folder `SlotBook`, evaluation group `slotbook`
(interval 5m unless stated). Metric names below are the expected Prometheus names. OTLP→Prometheus conversion
adds `_total` to counters and unit suffixes, and replaces `.` with `_`. **Confirm each exact name in Explore
once data has arrived** (Explore → Metrics → search `slotbook` / `http_server`) and fix the queries if they differ.

| # | Rule | Query (expected names) | Condition | Pending |
|---|---|---|---|---|
| a | Synthetics failing twice in a row | `probe_success{job="<check job>", instance="https://slotbook-api-sc87.onrender.com/health"}` (take both labels from the check's page) | `< 1` | 10m |
| b | 5xx share above 1% | `sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])) / sum(rate(http_server_request_duration_seconds_count[5m]))` | `> 0.01` | 5m |
| c | MongoDB operations above 70/s | `sum(rate(slotbook_mongodb_operations_total[1m]))` | `> 70` | 1m |
| d | Outbox backlog above 1,000 | `max(slotbook_outbox_pending)` | `> 1000` | 5m |

Notes:

- (c) 70 operations per second is below the Atlas M0 cap of 100 per second, so the rule fires before throttling begins.
- (b) The request-duration histogram's `_count` is the request count. With no traffic the division yields no data; set "no data" handling to *OK*.
- (d) The gauge may be exposed as `slotbook_outbox_pending_messages` if the unit suffix is applied. Use whatever Explore shows.
- For (c), (d): set "no data" to *OK*. The service sleeps on Render's free plan, and the Synthetics rule (a) covers it being down.

## 4. Atlas storage above 70%

Configured in **Atlas**, not Grafana: Project → Alerts → Add alert → condition *Disk space used (% of data size)*
(or the cluster's storage metric on a shared tier) `>` 70, notify by email to the developer.

## 5. Local: Aspire dashboard

```bash
docker run --rm -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard \
  -e DASHBOARD__FRONTEND__AUTHMODE=Unsecured \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Set `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` for the API, then open <http://localhost:18888>. Without the
variable the app exports nothing.

## 6. Verify

1. Request the demo. In Explore → Tempo find the trace: `{ resource.service.name = "slotbook-api" }`.
2. In Explore → Prometheus, query `slotbook_mongodb_operations_total` and confirm it is non-zero and has only the
   `db.operation.name` label (besides resource labels).
3. In Render, suspend `slotbook-api`. Wait about 10–15 minutes: the Synthetics rule (a) fires and the email arrives.
4. Resume the service and confirm the resolved email.

## Not covered yet

Alerts for the `_error` queue and CloudAMQP queued messages are a follow-up. Check what CloudAMQP's free plan
exposes before writing them.
