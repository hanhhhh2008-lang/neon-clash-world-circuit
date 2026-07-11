import { integer, sqliteTable, text } from "drizzle-orm/sqlite-core";

export const rooms = sqliteTable("rooms", {
  id: text("id").primaryKey(),
  hostToken: text("host_token").notNull(),
  guestToken: text("guest_token"),
  config: text("config").notNull(),
  guestInput: text("guest_input").notNull().default("{}"),
  state: text("state").notNull().default("{}"),
  status: text("status").notNull().default("waiting"),
  createdAt: integer("created_at").notNull(),
  updatedAt: integer("updated_at").notNull(),
  hostSeen: integer("host_seen").notNull(),
  guestSeen: integer("guest_seen"),
});

export const spectators = sqliteTable("spectators", {
  token: text("token").primaryKey(),
  roomId: text("room_id").notNull(),
  lastSeen: integer("last_seen").notNull(),
});
