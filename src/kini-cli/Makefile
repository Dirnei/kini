BINARY := kini
VERSION := $(shell git describe --tags --always --dirty 2>/dev/null || echo dev)

.PHONY: build install test clean

build:
	go build -ldflags "-X kini/cmd.version=$(VERSION)" -o $(BINARY) .

install:
	go install -ldflags "-X kini/cmd.version=$(VERSION)" .

test:
	go test ./...

clean:
	rm -f $(BINARY)
