.DEFAULT_GOAL := all

NAME := ddlog
VERSION := 0.1.0
TIMESTAMP := $(shell date +%s)

# File name for deployment artifact
ARTIFACT_NAME := $(NAME)_$(VERSION)_$(TIMESTAMP)

### Directories ###
# Root directory where the makefile is located
DIR_ROOT := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
# Target directory for build artifacts
DIR_BUILD := $(DIR_ROOT)/Out
# Target directory for test results
DIR_TEST_RESULTS := $(DIR_BUILD)/TestResults
# Target directory for make markers
DIR_MARKERS := $(DIR_BUILD)/Markers

export NAME
export VERSION

# Create target directories
$(DIR_BUILD) \
$(DIR_MARKERS) \
$(DIR_TEST_RESULTS):
	@mkdir -p $@

### Standard Rules ###

.PHONY: all
all: run

.PHONY: build
build: docker-build

.PHONY: run
run: build
	docker run --rm -it $(NAME) demo --lps 10 --delay 500 "/var/log/access.log"

.PHONY: test
test: build-test | $(DIR_TEST_RESULTS)
	docker run --rm -v $(DIR_TEST_RESULTS):/app/Tests/TestResults $(NAME):test

.PHONY: clean
clean:
	@rm -rf $(DIR_BUILD)
	@rm -rf DDLogReader/bin
	@rm -rf DDLogReader/obj
	@rm -rf Tests/bin
	@rm -rf Tests/obj
	docker rmi --force $(NAME)
	docker rmi --force $(NAME):test

### App Specific Rules ###

.PHONY: reader
reader: build
	docker run --rm -it $(NAME) read --lps 10

.PHONY: writer
writer: build
	docker run --rm -it $(NAME) write --delay 500

.PHONY: build-test
build-test: docker-build-test

### Rules for Docker builds ###

# Clean out all that old docker crud! Save harddrive space!
.PHONY: docker-clean
docker-clean:
	# delete all containers
	docker rm --force $(docker ps -aq)
	# delete all images
	docker rmi --force $(docker images -aq)

# Build the application in docker
.PHONY: docker-build
docker-build: $(DIR_MARKERS)/docker-build
# Build the application inside a pair of docker containers
$(DIR_MARKERS)/docker-build: Dockerfile ./DDLogReader/*.cs | $(DIR_BUILD) $(DIR_MARKERS)
	docker build --pull -t $(NAME) -f Dockerfile .
	touch $@

# Build the application for tests in docker
.PHONY: docker-build-test
docker-build-test: $(DIR_MARKERS)/docker-build-test
# Build the application inside a pair of docker containers
$(DIR_MARKERS)/docker-build-test: Dockerfile ./DDLogReader/*.cs ./Tests/*.cs | $(DIR_BUILD) $(DIR_MARKERS)
	docker build --pull --target testrunner -t $(NAME):test -f Dockerfile .
	touch $@