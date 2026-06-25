# Database Migrations

This folder contains forward-only SQL migrations embedded in `EnterpriseTask.Infrastructure`.

Rules:

- Use `NNNN_description.sql` naming.
- Never add destructive reset statements such as `DROP TABLE ... CASCADE`.
- Make data migrations idempotent where possible.
- Keep feature schema changes in their own migration and commit boundary.
- Apply migrations through the development endpoint or a future deployment runner, not by re-running `supabase_schema_v2_clean.sql`.

`0001_initial_schema.sql` is generated from the current clean schema with the destructive drop block removed. The runtime migrator records it as a baseline when it detects an existing EnterpriseTask schema, so populated local databases are not reset.
