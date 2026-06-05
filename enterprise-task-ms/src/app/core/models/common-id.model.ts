export type Uuid = string;
export type BigIntId = number;

// Backend UUID migration is in progress while local mock data still uses numeric ids.
export type EntityId = Uuid | BigIntId;
