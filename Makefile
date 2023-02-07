SOLUTION_DIR=src
ARTIFACTS_DIR=artifacts
PACK_CMD=dotnet pack --configuration Release --no-build -o $(ARTIFACTS_DIR)

PACKAGES= \
	RabbitLink \
	RabbitLink.Serialization.Json \
	RabbitLink.System.Text.Json \
	RabbitLink.Microsoft.Extensions.Logging


.PHONY: all
all: artifacts

.PHONY: build
build: src
	dotnet build --configuration Release src --disable-build-servers

.PHONY: $(PACKAGES)
$(PACKAGES): %: src/% build
	$(PACK_CMD) $<

.PHONY: artifacts
artifacts: $(PACKAGES)

