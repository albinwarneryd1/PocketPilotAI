# Architecture

PocketPilotAI uses layered architecture:

- Core: domain model + application contracts
- Infrastructure: persistence and integrations
- API: external HTTP surface + auth
- Clients (Web/App): UI + API client wrappers
