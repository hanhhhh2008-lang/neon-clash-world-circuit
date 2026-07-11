CREATE TABLE `rooms` (
	`id` text PRIMARY KEY NOT NULL,
	`host_token` text NOT NULL,
	`guest_token` text,
	`config` text NOT NULL,
	`guest_input` text DEFAULT '{}' NOT NULL,
	`state` text DEFAULT '{}' NOT NULL,
	`status` text DEFAULT 'waiting' NOT NULL,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL,
	`host_seen` integer NOT NULL,
	`guest_seen` integer
);
--> statement-breakpoint
CREATE TABLE `spectators` (
	`token` text PRIMARY KEY NOT NULL,
	`room_id` text NOT NULL,
	`last_seen` integer NOT NULL
);
