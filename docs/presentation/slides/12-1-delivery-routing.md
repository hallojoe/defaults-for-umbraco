# YARP routes delivery traffic

YARP knows both delivery instances and round-robins public traffic across them:

```json
"Destinations": {
  "subscriber-1": { "Address": "https://localhost:44101/" },
  "subscriber-2": { "Address": "https://localhost:54101/" }
},
"LoadBalancingPolicy": "RoundRobin"
```

Aspire starts, connects, configures, and lets us inspect the complete environment.
