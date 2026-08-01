# iFlame - local static serve + iOS Simulator
#
# Usage:
#   make ios              Boot Simulator, serve the site, open Safari
#   make serve            Serve only (host browser)
#   make stop             Stop the background server
#   make open             Open the site in an already-booted Simulator
#
# Override device or port:
#   make ios DEVICE="iPhone 16 Pro" PORT=9000

ROOT_DIR  := $(abspath $(dir $(lastword $(MAKEFILE_LIST))))
PORT      ?= 8765
HOST      ?= 127.0.0.1
URL       := http://$(HOST):$(PORT)/
PIDFILE   := $(ROOT_DIR)/.server.pid
LOGFILE   := $(ROOT_DIR)/.server.log
UDIDFILE  := $(ROOT_DIR)/.sim.udid
DEVICE    ?= iPhone 17
BOOT_WAIT ?= 90

.PHONY: help ios serve serve-bg stop open boot-sim status

help:
	@echo "iFlame local targets"
	@echo "  make ios      Serve site and open it in the iOS Simulator"
	@echo "  make serve    Foreground static server at $(URL)"
	@echo "  make stop     Stop background server started by make ios"
	@echo "  make open     Re-open $(URL) in the booted Simulator"
	@echo "  make status   Show server + simulator state"
	@echo ""
	@echo "Vars: DEVICE=\"$(DEVICE)\" PORT=$(PORT)"

ios: serve-bg boot-sim open
	@echo "iFlame at $(URL) on $(DEVICE)"

serve:
	@cd "$(ROOT_DIR)" && python3 -m http.server $(PORT) --bind $(HOST)

serve-bg:
	@chmod +x "$(ROOT_DIR)/scripts/serve-bg.sh"
	@"$(ROOT_DIR)/scripts/serve-bg.sh" "$(ROOT_DIR)" "$(PORT)" "$(HOST)" "$(PIDFILE)" "$(LOGFILE)"

stop:
	@if [ -f "$(PIDFILE)" ]; then \
		pid=$$(cat "$(PIDFILE)"); \
		if kill -0 $$pid 2>/dev/null; then \
			kill $$pid && echo "Stopped server pid $$pid"; \
		else \
			echo "Stale pidfile (pid $$pid not running)"; \
		fi; \
		rm -f "$(PIDFILE)"; \
	else \
		echo "No server pidfile at $(PIDFILE)"; \
	fi

boot-sim:
	@udid=$$(xcrun simctl list devices available | grep -F "$(DEVICE) (" | grep -E '\((Booted|Shutdown)\)' | head -1 | sed -E 's/.*\(([A-F0-9-]{36})\) \((Booted|Shutdown)\).*/\1/'); \
	if [ -z "$$udid" ]; then \
		echo "No available simulator named '$(DEVICE)'."; \
		echo "List with: xcrun simctl list devices available"; \
		exit 1; \
	fi; \
	echo "$$udid" >"$(UDIDFILE)"; \
	state=$$(xcrun simctl list devices | grep "$$udid" | sed -E 's/.*\((Booted|Shutdown|Booting)\).*/\1/'); \
	if [ "$$state" != "Booted" ]; then \
		echo "Booting $(DEVICE) ($$udid)..."; \
		xcrun simctl boot "$$udid" 2>/dev/null || true; \
	else \
		echo "$(DEVICE) already booted ($$udid)"; \
	fi; \
	open -a Simulator --args -CurrentDeviceUDID "$$udid"; \
	echo "Waiting for Simulator (up to $(BOOT_WAIT)s)..."; \
	i=0; \
	while [ $$i -lt $(BOOT_WAIT) ]; do \
		if xcrun simctl list devices | grep "$$udid" | grep -q Booted; then \
			if xcrun simctl launch "$$udid" com.apple.mobilesafari >/dev/null 2>&1; then \
				echo "Simulator ready."; \
				exit 0; \
			fi; \
		fi; \
		i=$$((i + 1)); \
		sleep 1; \
	done; \
	echo "Simulator did not become ready in $(BOOT_WAIT)s; try: make open"; \
	exit 1

open:
	@udid=$$(cat "$(UDIDFILE)" 2>/dev/null); \
	if [ -z "$$udid" ]; then udid=booted; fi; \
	echo "Opening $(URL) in Simulator Safari"; \
	xcrun simctl launch "$$udid" com.apple.mobilesafari >/dev/null 2>&1 || true; \
	sleep 1; \
	xcrun simctl openurl "$$udid" "$(URL)"

status:
	@if [ -f "$(PIDFILE)" ] && kill -0 $$(cat "$(PIDFILE)") 2>/dev/null; then \
		pid=$$(cat "$(PIDFILE)"); \
		echo "Server: running pid $$pid at $(URL)"; \
	else \
		echo "Server: stopped"; \
	fi
	@booted=$$(xcrun simctl list devices | grep Booted | head -5); \
	if [ -n "$$booted" ]; then \
		echo "Simulator:"; \
		printf '%s\n' "$$booted"; \
	else \
		echo "Simulator: none booted"; \
	fi
