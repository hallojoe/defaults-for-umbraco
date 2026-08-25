# Distributed cache is shared

CM and every CD instance need to receive the same cache refresh instructions.

Redis is one option; this setup can also use SQL-backed distributed cache.

The important bit is one shared cache—not one per website process.
