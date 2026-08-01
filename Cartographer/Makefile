.PHONY: test build up down health

test:
	dotnet test Cartographer.sln --nologo

build:
	dotnet build Cartographer.sln -c Release --nologo

up:
	docker compose up -d postgres pmtiles

down:
	docker compose down

health:
	curl -s http://localhost:5080/health | python3 -m json.tool
