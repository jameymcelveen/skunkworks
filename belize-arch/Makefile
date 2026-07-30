# belize-arch — launcher. No dependencies, no build; python3's stdlib server only.
#
#   make run       hub click-through (index.html)
#   make arch      hub static reference (architecture.html)
#   make data      data-scope static (data.html)
#   make data-demo data ownership walkthrough (data-demo.html)
#   make stop      free the port if a server was left running
#
# Overrides:  make run PORT=9000  ·  make serve PAGE=architecture.html
#             make serve HOST=0.0.0.0   (present from a tablet on the same wifi)

PORT ?= 8080
HOST ?= 127.0.0.1
PAGE ?= index.html
PY   := $(shell command -v python3 2>/dev/null)

.DEFAULT_GOAL := help
.PHONY: help run arch data data-demo serve open stop

help:
	@echo "belize-arch"
	@echo "  make run        hub click-through demo (index.html)"
	@echo "  make arch       hub static diagram (architecture.html)"
	@echo "  make data       data-scope containment (data.html)"
	@echo "  make data-demo  data ownership walkthrough (data-demo.html)"
	@echo "  make serve      serve only, no browser  [PAGE= PORT= HOST=]"
	@echo "  make open       open PAGE straight off disk, no server"
	@echo "  make stop       kill whatever is listening on port $(PORT)"

run: PAGE := index.html
run: serve

arch: PAGE := architecture.html
arch: serve

data: PAGE := data.html
data: serve

data-demo: PAGE := data-demo.html
data-demo: serve

serve:
ifeq ($(PY),)
	@echo "python3 not found — falling back to opening the file directly."
	@$(MAKE) --no-print-directory open PAGE=$(PAGE)
else
	@echo "→ http://$(HOST):$(PORT)/$(PAGE)   (ctrl-c to stop)"
	@( sleep 1; open "http://$(HOST):$(PORT)/$(PAGE)" >/dev/null 2>&1 ) &
	@$(PY) -m http.server $(PORT) --bind $(HOST) --directory "$(CURDIR)"
endif

open:
	@open "$(CURDIR)/$(PAGE)"

stop:
	@pids=$$(lsof -ti tcp:$(PORT) 2>/dev/null); \
	if [ -n "$$pids" ]; then kill $$pids && echo "stopped $$pids on port $(PORT)"; \
	else echo "nothing listening on port $(PORT)"; fi
