# Payment Service - JMeter Performance Tests

## Prerequisites

- Apache JMeter 5.6+
- Payment Service running at `http://localhost:5003`

## How to Run

```bash
jmeter -n \
  -t services/payment-service/performance-tests/jmeter/payment-performance-test.jmx \
  -JbaseUrl=http://localhost:5003 \
  -JauthToken="your-jwt-token" \
  -l results.jtl \
  -e -o report/
```

## Metrics Captured

- Throughput
- Average Response Time
- Median, 90th, 95th, 99th percentile
- Error %
- Active Threads

## Report

After execution, open `report/index.html` in a browser.
