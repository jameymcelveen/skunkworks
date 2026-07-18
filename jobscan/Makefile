DOTNET := dotnet
APP    := src/Jobscan/Jobscan.csproj
TESTS  := tests/Jobscan.Tests/Jobscan.Tests.csproj

.PHONY: build scan scan-all paste dry test kit clean publish

build:
	$(DOTNET) build $(APP) --nologo

scan:
	$(DOTNET) run --project $(APP) --nologo -v q

scan-all:
	$(DOTNET) run --project $(APP) --nologo -v q -- --all

paste:
	$(DOTNET) run --project $(APP) --nologo -v q -- --paste-only

dry:
	$(DOTNET) run --project $(APP) --nologo -v q -- --dry

test:
	$(DOTNET) run --project $(TESTS) --nologo -v q

publish:
	$(DOTNET) publish $(APP) -c Release -o dist

clean:
	$(DOTNET) clean $(APP) --nologo && rm -rf dist

kit:
	@test -n "$(ID)" || (echo "usage: make kit ID=<posting-id>   (from backlog.md)"; exit 1)
	@echo "Kit generation is a Claude Code task, not a cron task. Run:"
	@echo ""
	@echo "  claude \"read CLAUDE.md and brief.md, then build the apply kit for posting $(ID)\""
